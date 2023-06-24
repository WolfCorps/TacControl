using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using Mapsui.Extensions;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class PolylineMarkerRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle istyle,
            IRenderCache symbolCache, long iteration)
        {
            var style = ((PolylineMarkerStyle)istyle);

            var zoom = 1 / (float)viewport.Resolution;
            //
            var dest = viewport.WorldToScreen(style.start.X, style.start.Y);
            canvas.Translate((float)dest.X, (float)dest.Y);
            canvas.Scale(zoom, zoom);

            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);

            //#TODO store paint with shader in the style
            using (SKPaint paint = new SKPaint())
            {
                paint.Color = style.color.WithAlpha((byte)(style.Opacity*255));
                paint.StrokeWidth = 3f * (float)viewport.Resolution;
                paint.Style = SKPaintStyle.Stroke;
                paint.IsAntialias = true;

                canvas.DrawPath(style.path, paint);
            }

            return true;
        }
    }
}
