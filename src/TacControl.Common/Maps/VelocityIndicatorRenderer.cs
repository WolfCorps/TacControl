using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Styles;
using Mapsui.Extensions;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class VelocityIndicatorRenderer : Mapsui.Experimental.Rendering.Skia.SkiaStyles.ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle istyle,
            RenderService renderService, long iteration)
        {
            var style = ((VelocityIndicatorStyle)istyle);

            var position = feature.Extent.GetBottomLeft();
            var dest = viewport.WorldToScreen(position);

            var zoom = 1 / (float)viewport.Resolution;

            canvas.Translate((float)dest.X, (float)dest.Y);
            //canvas.Scale(zoom, zoom);

            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);

            //#TODO store paint with shader as static
            using (SKPaint paint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                Color = SKColors.Red,
                StrokeWidth = 4
            })
            {
                //if (style.rotation == 0) //Weird artifacting on 0 rotation, no idea why. Seems Skia bug.
                //    style.rotation = 180;
                //
                //SKMatrix shaderTransform =
                //    SKMatrix.CreateScale((float)viewport.Resolution, (float)viewport.Resolution);
                //if (style.rotation != 0)
                //    shaderTransform = SKMatrix.Concat(shaderTransform, SKMatrix.CreateRotationDegrees(-style.rotation));


                canvas.DrawLine(new SKPoint(0, 0), new SKPoint(-style.velocity.X, style.velocity.Y), paint);
            }




            return true;
        }
    }
}
