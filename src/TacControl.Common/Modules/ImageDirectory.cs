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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using Xamarin.Forms.PlatformConfiguration;

namespace TacControl.Common.Modules
{
    public class ImageDirectory
    {
        public static ImageDirectory Instance = new ImageDirectory();


        public Dictionary<string, SKImage> imageCache = new Dictionary<string, SKImage>(StringComparer.InvariantCultureIgnoreCase);


        private class ImageRequest
        {
            public string path;
            public TaskCompletionSource<SKImage> completionSource;
        }


        private Dictionary<string, ImageRequest> pendingRequests = new Dictionary<string, ImageRequest>(StringComparer.InvariantCultureIgnoreCase);

        public Task<SKImage> GetImage(string path)
        {
            lock (imageCache)
            {
                if (imageCache.ContainsKey(path))
                    return Task.FromResult(imageCache[path]);

                if (pendingRequests.ContainsKey(path))
                    return pendingRequests[path].completionSource.Task;

                var request = new ImageRequest { path = path, completionSource = new TaskCompletionSource<SKImage>() };
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

        private class MapfileRequest
        {
            public string name;
            public string targetDirectory;
            public TaskCompletionSource<string> completionSource;
        }


        private List<MapfileRequest> pendingMapRequests = new List<MapfileRequest>();

        public Task<string> RequestMapfile(string worldName, string targetDirectory)
        {
            lock (imageCache)
            {
                if (pendingMapRequests.Any( x => x.name == worldName))
                    return pendingMapRequests.First(x => x.name == worldName).completionSource.Task;

                var request = new MapfileRequest { name = worldName, targetDirectory = targetDirectory, completionSource = new TaskCompletionSource<string>() };
                pendingMapRequests.Add(request);


                Networking.Instance.SendMessage(
                    $@"{{
                        ""cmd"": [""ImgDir"", ""RequestMapfile""],
                        ""args"": {{
                            ""name"": ""{worldName}""
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

                //ARGB -> BGRA
                for (int i = 0; i < dataBytes.Length; i += 4)
                {
                    var A = dataBytes[i];
                    var B = dataBytes[i + 1];
                    var G = dataBytes[i + 2];
                    var R = dataBytes[i + 3];

                    dataBytes[i] = B;
                    dataBytes[i + 1] = G;
                    dataBytes[i + 2] = R;
                    dataBytes[i + 3] = A;
                }

                var bmp = SKImage.FromPixelCopy(new SKImageInfo(width, width, SKColorType.Bgra8888), dataBytes);
                request.completionSource.SetResult(bmp);

                lock (imageCache)
                {
                    imageCache[path] = bmp;
                    pendingRequests.Remove(path);
                }
            } else if (cmd.First() == "MapFile") {
                var path = args["name"].Value<string>();
                var data = args["data"].Value<string>();

                MapfileRequest request = pendingMapRequests.FirstOrDefault(x => path.StartsWith(x.name));

                lock (imageCache)
                {
                    request = pendingMapRequests.FirstOrDefault(x => path.StartsWith(x.name)); //#TODO log/handle
                    if (request == null) return;
                }

                var dataBytes = Convert.FromBase64String(data);

                //If source was not compressed, still store it compressed, save some disk space especially on mobile
                if (path.EndsWith("z"))
                    using (var writer = File.Create(Path.Combine(request.targetDirectory, path)))
                    {
                        writer.Write(dataBytes, 0, dataBytes.Length);
                    }
                else
                {
                    using (var writer = File.Create(Path.Combine(request.targetDirectory, path+"z")))
                    {
                        using (GZipStream compressionStream = new GZipStream(writer,
                            CompressionMode.Compress))
                        {
                            compressionStream.Write(dataBytes, 0, dataBytes.Length);
                        }
                    }
                }

                request.completionSource.SetResult(Path.Combine(request.targetDirectory, path));

                lock (imageCache)
                {
                    pendingMapRequests.Remove(request);
                }
            }
        }

    }
}
