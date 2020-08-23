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
    public class TiledBitmapRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, IReadOnlyViewport viewport, ILayer layer, IFeature feature, IStyle style,
            ISymbolCache symbolCache)
        {

            var image = ((TiledBitmapStyle)style).image;
            if (image == null) return false;
            var rect = ((TiledBitmapStyle)style).rect;
            var rotation = ((TiledBitmapStyle)style).rotation;

            var position = feature.Geometry as Point;
            var dest = viewport.WorldToScreen(position);


            var zoom = 1 / (float)viewport.Resolution;

            canvas.Translate((float)dest.X, (float)dest.Y);
            canvas.Scale(zoom, zoom);

            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);
            canvas.RotateDegrees(rotation, 0.0f, 0.0f);

            //#TODO store paint with shader in the style
            using (SKPaint paint = new SKPaint())
            {
                paint.Shader = SKShader.CreateImage(image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
                canvas.DrawRect(rect, paint);
            }

            return true;
        }
    }
}
