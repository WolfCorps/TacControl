using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class VelocityIndicatorRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, IReadOnlyViewport viewport, ILayer layer, IFeature feature, IStyle istyle,
            ISymbolCache symbolCache)
        {
            var style = ((VelocityIndicatorStyle)istyle);

            var position = feature.Geometry as Point;
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
