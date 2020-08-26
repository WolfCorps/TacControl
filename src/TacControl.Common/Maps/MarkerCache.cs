using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapsui.Styles;
using SkiaSharp;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public class MarkerCache
    {
        public static MarkerCache Instance = new MarkerCache();

        private class MarkerRequest
        {
            public string path;
            public TaskCompletionSource<SKImage> completionSource;
        }

        private Dictionary<string, MarkerRequest> requests = new Dictionary<string, MarkerRequest>(StringComparer.InvariantCultureIgnoreCase);

        public async Task<int> GetBitmapId(MarkerType type, MarkerColor color)
        {
            var image = await GetImage(type, color);

            var content = new MemoryStream(image.Encode().ToArray());
            int bitmapId;
            lock (BitmapRegistry.Instance)
            {
                bitmapId = BitmapRegistry.Instance.Register(content);
            }

            return bitmapId;
        }

        public Task<SKImage> GetImage(MarkerType type, MarkerColor color)
        {
            lock (requests)
            {
                var path = $"{type.name}_{color?.name ?? ""}";

                if (requests.ContainsKey(path))
                    return requests[path].completionSource.Task;

                var request = new MarkerRequest { path = path, completionSource = new TaskCompletionSource<SKImage>() };
                requests[path] = request;


                void ContinuationFunction(SKImage image)
                {
                    if (color != null)
                        image = image.ApplyImageFilter(
                            SkiaSharp.SKImageFilter.CreateColorFilter(SkiaSharp.SKColorFilter.CreateLighting(color.ToSKColor(), new SKColor(0, 0, 0))),
                            new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                            new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                            out var outSUbs,
                            out SKPoint outoffs
                        );

                    if (type.shadow)
                    {
                        image = image.ApplyImageFilter(
                            SkiaSharp.SKImageFilter.CreateDropShadow(2, 2, 2, 2, SKColors.Black),
                            new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                            new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                            out var outSUbs2,
                            out SKPoint outoffs2
                        );
                    }

                    request.completionSource.SetResult(image);
                }


                if (type.iconImage != null)
                    ContinuationFunction(type.iconImage);
                else if (type.icon.StartsWith("#"))
                {
                    var image = GenerateProcedural(type.icon);

                    ContinuationFunction(image);
                }
                else
                {
                    ImageDirectory.Instance.GetImage(type.icon)
                        .ContinueWith(
                            x => ContinuationFunction(x.Result));
                }



                return request.completionSource.Task;
            }
        }


        private class BrushRequest
        {
            public string path;
            public TaskCompletionSource<SKImage> completionSource;
        }

        private Dictionary<string, BrushRequest> brushRequests = new Dictionary<string, BrushRequest>(StringComparer.InvariantCultureIgnoreCase);




        public Task<SKImage> GetImage(MarkerBrush brush, MarkerColor color)
        {
            lock (brushRequests)
            {
                var path = $"{brush.name}_{color?.name ?? ""}";

                if (brushRequests.ContainsKey(path))
                    return brushRequests[path].completionSource.Task;

                var request = new BrushRequest { path = path, completionSource = new TaskCompletionSource<SKImage>() };
                brushRequests[path] = request;


                if (brush.texture == "")
                    brush.texture = "#(argb,8,8,3)color(0.5,0.5,0.5,0.5)";

                if (brush.texture.StartsWith("#"))
                {
                    var bmp = GenerateProcedural(brush.texture);

                    if (color != null)
                        bmp = bmp.ApplyImageFilter(
                            SkiaSharp.SKImageFilter.CreateColorFilter(
                                SkiaSharp.SKColorFilter.CreateLighting(color.ToSKColor(), new SKColor(0, 0, 0))
                            ),
                            new SkiaSharp.SKRectI(0, 0, bmp.Width, bmp.Height),
                            new SkiaSharp.SKRectI(0, 0, bmp.Width, bmp.Height),
                            out var outSUbs,
                            out SKPoint outoffs);
                    request.completionSource.SetResult(bmp);
                }
                else
                    ImageDirectory.Instance.GetImage(brush.texture)
                            .ContinueWith(
                                (x) =>
                                {
                                    var image = x.Result;
                                    
                                    if (color != null)
                                        image = image.ApplyImageFilter(
                                            SkiaSharp.SKImageFilter.CreateColorFilter(
                                                SkiaSharp.SKColorFilter.CreateLighting(color.ToSKColor(), new SKColor(0, 0, 0))
                                            ),
                                            new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                                            new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                                            out var outSUbs,
                                            out SKPoint outoffs);

                                    request.completionSource.SetResult(image);
                                });

                return request.completionSource.Task;
            }
        }

        private static SKImage GenerateProcedural(string texture)
        {
            if (!texture.StartsWith("#(argb"))
                Debugger.Break();

            var split = texture.Trim('#', '(', ')').Replace(")color(", ",").Split(',');

            var format = split[0];
            var width = int.Parse(split[1]);
            var height = int.Parse(split[2]);
            //Mipmaps 3

            CultureInfo ci = (CultureInfo) CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";
            var colorArr = split.Skip(4).Select(xy => (byte) (float.Parse(xy, NumberStyles.Any, ci) * 255)).ToList();


            byte[] data = new byte[width * height * 4];
            for (int i = 0; i < width * height * 4; i += 4)
            {
                data[i] = colorArr[0];
                data[i + 1] = colorArr[1];
                data[i + 2] = colorArr[2];
                data[i + 3] = colorArr[3];
            }

            var bmp = SKImage.FromPixelCopy(new SKImageInfo(width, height, SKColorType.Rgba8888), data);
            return bmp;
        }
    }
}
