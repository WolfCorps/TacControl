using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class TiledBitmapRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, IReadOnlyViewport viewport, ILayer layer, IFeature feature, IStyle istyle,
            ISymbolCache symbolCache)
        {
            var style = ((TiledBitmapStyle) istyle);

            if (style.image == null || style.Opacity == 0)
                return true;

            var position = feature.Extent.Centroid;
            var dest = viewport.WorldToScreen(position);


            var zoom = 1 / (float)viewport.Resolution;

            canvas.Translate((float)dest.X, (float)dest.Y);
            canvas.Scale(zoom, zoom);

            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);
            if (style.rotation != 0) canvas.RotateDegrees(style.rotation, 0.0f, 0.0f);

            //#TODO store paint with shader in the style
            using (SKPaint paint = new SKPaint())
            {
                if (style.rotation == 0) //Weird artifacting on 0 rotation, no idea why. Seems Skia bug.
                    style.rotation = 180;

                SKMatrix shaderTransform =
                    SKMatrix.CreateScale((float) viewport.Resolution, (float) viewport.Resolution);
                if (style.rotation != 0)
                    shaderTransform = SKMatrix.Concat(shaderTransform, SKMatrix.CreateRotationDegrees(-style.rotation));


                paint.Color = SKColors.White.WithAlpha((byte)(style.Opacity * 255));
                paint.Shader = SKShader.CreateImage(style.image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, shaderTransform);
                paint.ColorFilter = style.colorFilter;

                //style.image.Encode().SaveTo(File.Create($"P:/{style.image.UniqueId}.png"));

                if (style.ellipse)
                {
                    canvas.DrawOval(0, 0, style.rect.Right, style.rect.Bottom, paint);
                }
                else
                {
                    canvas.DrawRect(style.rect, paint);
                }

                if (style.border)
                {

                    var borderPaint = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        Color = style.color
                    };


                    if (style.ellipse)
                    {
                        canvas.DrawOval(0, 0, style.rect.Right, style.rect.Bottom, borderPaint);
                    }
                    else
                    {
                        canvas.DrawRect(style.rect, borderPaint);
                    }


                }



            }




            return true;
        }
    }
}
