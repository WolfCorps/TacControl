using System;
using Mapsui;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    // https://github.com/Mapsui/Mapsui/blob/a9c28e1f111605775881fa57382f7142b5c2ade9/Mapsui.Rendering.Skia/PointRenderer.cs
    internal class PointRenderer
    {
        public static void Draw(SKCanvas canvas, IReadOnlyViewport viewport, IStyle style, IFeature feature,
            double x, double y, SymbolCache symbolCache, float opacity)
        {
            var (destX, destY) = viewport.WorldToScreenXY(x, y);

            if (style is CalloutStyle calloutStyle)
            {
                CalloutStyleRenderer.Draw(canvas, viewport, opacity, destX, destY, calloutStyle);
            }
            else if (style is LabelStyle labelStyle)
            {
                LabelRenderer.Draw(canvas, labelStyle, feature, destX, destY, opacity);
            }
            else if (style is SymbolStyle symbolStyle)
            {
                if (symbolStyle.BitmapId >= 0)
                {
                    // todo: Remove this call. ImageStyle should be used instead of SymbolStyle with BitmapId
                    ImageStyleRenderer.Draw(canvas, symbolStyle, destX, destY, symbolCache, opacity, viewport.Rotation);
                }
                else
                {
                    SymbolStyleRenderer.Draw(canvas, symbolStyle, destX, destY, opacity, symbolStyle.SymbolType, viewport.Rotation);
                }
            }
            else if (style is ImageStyle imageStyle)
            {
                ImageStyleRenderer.Draw(canvas, imageStyle, destX, destY, symbolCache, opacity, viewport.Rotation);
            }
            else if (style is VectorStyle vectorStyle)
            {
                // Use the SymbolStyleRenderer and specify Ellipse
                SymbolStyleRenderer.Draw(canvas, vectorStyle, destX, destY, opacity, SymbolType.Ellipse);
            }
            else
            {
                throw new Exception($"Style of type '{style.GetType()}' is not supported for points");
            }
        }
    }
}
