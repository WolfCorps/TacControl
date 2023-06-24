using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using Mapsui.Extensions;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class SvgStyleRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style,
            IRenderCache symbolCache, long iteration)
        {

            var image = ((SvgStyle)style).GetImage();
            if (image == null) return true;
            //SvgRenderer.Draw(canvas, image, -(float)viewport.Center.X, (float)viewport.Center.Y, (float)viewport.Rotation, 0,0,default, default, default, (float)viewport.Resolution);

            canvas.Save();


            var zoom = 1 / (float)viewport.Resolution;

            var canvasSize = canvas.LocalClipBounds;

            var canvasCenterX = canvasSize.Width / 2;
            var canvasCenterY = canvasSize.Height / 2;


            //float width = (float)MapPage.currentBounds.Width;
            //float height = (float)MapPage.currentBounds.Height;


            float width = image.Picture.CullRect.Width;
            float height = image.Picture.CullRect.Height;

            float num1 = (width / 2) * zoom;
            float num2 = (-height) * zoom;
            canvas.Translate(canvasCenterX, num2 + canvasCenterY);

            canvas.Translate(-(float)viewport.CenterX * zoom, (float)viewport.CenterY * zoom);

            canvas.Scale(zoom, zoom);


            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);


            canvas.DrawPicture(image.Picture, new SKPaint()
            {
                IsAntialias = true
            });
            canvas.Restore();






            return true;
        }
    }


}
