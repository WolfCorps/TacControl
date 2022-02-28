using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using TacControl.Common;

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
                            ""path"": {JsonConvert.ToString(path)}
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
                int width = args["width"].Value<int>();
                int height = args["height"].Value<int>();

                ImageRequest request;

                lock (imageCache)
                {
                    if (!pendingRequests.ContainsKey(path)) return; //#TODO log/handle

                    request = pendingRequests[path];
                }

                var dataBytes = Convert.FromBase64String(data);
                
                //ABGR -> BGRA
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

                var bmp = SKImage.FromPixelCopy(new SKImageInfo(width, height, SKColorType.Bgra8888), dataBytes);
                //if (bmp == null) Debugger.Break();
                request.completionSource.SetResult(bmp);


                //if (bmp != null)
                //{
                //    using (var data2 = bmp.Encode(SKEncodedImageFormat.Png, 80))
                //    using (var stream = File.OpenWrite(Path.Combine("P:/", Path.ChangeExtension(Path.GetFileName(path), ".png") )))
                //    {
                //        // save the data to a stream
                //        data2.SaveTo(stream);
                //    }
                //}
                




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
                if (path.EndsWith("z") || Path.GetExtension(path) == ".zip")
                    using (var writer = File.Create(Path.Combine(request.targetDirectory, path)))
                    {
                        writer.Write(dataBytes, 0, dataBytes.Length);
                    }
                else
                {
                    path = path + "z"; //svgz
                    using (var writer = File.Create(Path.Combine(request.targetDirectory, path)))
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

        public unsafe void ExportImagesToZip(string zipFilePath)
        {
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);
            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var image in imageCache)
                {
                    if (image.Value == null)
                        continue;
                    var entry = zip.CreateEntry(new System.IO.FileInfo(image.Key).Name, CompressionLevel.Optimal);

                    using (Stream destination1 = entry.Open())
                    using (var binWriter = new BinaryWriter(destination1))
                    using (var pixmap = image.Value.PeekPixels())
                    {
                        binWriter.Write(image.Value.Width);
                        binWriter.Write(image.Value.Height);
                        binWriter.Write(image.Key);
              

                        var buffer = new byte[pixmap.BytesSize];

                        if (pixmap.BytesSize != pixmap.Width * pixmap.Height * 4)
                        {
                            Debugger.Break();
                            
                        }

                        fixed (byte* p = buffer)
                        {
                            IntPtr ptr = (IntPtr)p;
                            pixmap.ReadPixels(new SKImageInfo(pixmap.Width, pixmap.Height, SKColorType.Bgra8888), ptr, pixmap.Width * pixmap.BytesPerPixel);
                        }


                        //BGRA -> ABGR
                        for (int i = 0; i < buffer.Length; i += 4)
                        {
                            var B = buffer[i];
                            var G = buffer[i + 1];
                            var R = buffer[i + 2];
                            var A = buffer[i + 3];

                            buffer[i] = A;
                            buffer[i + 1] = B;
                            buffer[i + 2] = G;
                            buffer[i + 3] = R;
                        }



                        binWriter.Write(buffer, 0, pixmap.BytesSize);
                    }
                }
            }
        }


        public bool HasPendingRequests()
        {
            return pendingRequests.Count == 0;
        }
    }
}
