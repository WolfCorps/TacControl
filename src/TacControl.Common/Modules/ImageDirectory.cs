// /*
// * Copyright (C) Dedmen Miller @ R4P3 - All Rights Reserved
// * Unauthorized copying of this file, via any medium is strictly prohibited
// * Proprietary and confidential
// * Written by Dedmen Miller <dedmen@dedmen.de>, 08 2016
// */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TacControl.Common.Modules
{
    public class ImageDirectory
    {
        public static ImageDirectory Instance = new ImageDirectory();

        public interface IImage
        {

        }

        public class Bitmap : IImage
        {
            public System.Drawing.Bitmap bmp;
        }



        public Dictionary<string, IImage> imageCache = new Dictionary<string, IImage>(StringComparer.InvariantCultureIgnoreCase);


        private class ImageRequest
        {
            public string path;
            public TaskCompletionSource<IImage> completionSource;
        }


        private Dictionary<string, ImageRequest> pendingRequests = new Dictionary<string, ImageRequest>(StringComparer.InvariantCultureIgnoreCase);

        public Task<IImage> GetImage(string path)
        {
            lock (imageCache)
            {
                if (imageCache.ContainsKey(path))
                    return Task.FromResult(imageCache[path]);

                if (pendingRequests.ContainsKey(path))
                    return pendingRequests[path].completionSource.Task;

                var request = new ImageRequest { path = path, completionSource = new TaskCompletionSource<IImage>() };
                pendingRequests[path] = request;


                Networking.Instance.SendMessage(
                    $@"{{
                        ""cmd"": [""ImgDir"", ""RequestTexture""],
                        ""args"": {{
                            ""path"": ""{path.Replace("\\","\\\\")}""
                        }}
                    }}"
                );


                return request.completionSource.Task;
            }
        }

        public void OnNetworkMessage(IEnumerable<string> cmd, JObject args)
        {
            if (cmd.First() == "TextureFile")
            {
                var path = args["path"].Value<string>();
                var data = args["data"].Value<string>();

                ImageRequest request;

                lock (imageCache)
                {
                    if (!pendingRequests.ContainsKey(path)) return; //#TODO log/handle

                    request = pendingRequests[path];
                }

                var dataBytes = Convert.FromBase64String(data);
             
                int width = (int) Math.Sqrt(dataBytes.Length/4);
                var bmp = new Bitmap {bmp = new System.Drawing.Bitmap(width, width, PixelFormat.Format32bppArgb)};

                //ARGB -> BGRA
                for (int i = 0; i < dataBytes.Length; i+=4)
                {
                    var A = dataBytes[i];
                    var B = dataBytes[i + 1];
                    var G = dataBytes[i + 2];
                    var R = dataBytes[i + 3];

                    dataBytes[i] = B;
                    dataBytes[i+1] = G;
                    dataBytes[i+2] = R;
                    dataBytes[i+3] = A;
                }


                BitmapData bmpData = bmp.bmp.LockBits(new Rectangle(0, 0,
                        bmp.bmp.Width,
                        bmp.bmp.Height),
                    ImageLockMode.WriteOnly,
                    bmp.bmp.PixelFormat);

                IntPtr pNative = bmpData.Scan0;
                Marshal.Copy(dataBytes, 0, pNative, dataBytes.Length);

                //var output = new FileStream("P:/test2", FileMode.CreateNew);
                //output.Write(dataBytes, 0, dataBytes.Length);
                //output.Close();


                bmp.bmp.UnlockBits(bmpData);

                request.completionSource.SetResult(bmp);

                lock (imageCache)
                {
                    imageCache[path] = bmp;
                    pendingRequests.Remove(path);
                }
            }
        }

    }
}
