using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    internal class MarkerLabelRenderer : ISkiaStyleRenderer
    {
        private static SKPaint renderPaint = new SKPaint
        {
            IsAntialias = true,
            IsDither = true,
            FilterQuality = SKFilterQuality.High,
            IsStroke = false,
            //ColorFilter = SKColorFilter. //#TODO render time color filter? instead of prefiltering images in memory?
            StrokeWidth = 0,
            Typeface = SKTypeface.FromFamilyName("Roboto", SKFontStyleWeight.Bold, SKFontStyleWidth.Condensed, SKFontStyleSlant.Upright),
            TextSize = 24,
            TextAlign = SKTextAlign.Left
        };
        public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle istyle,
            IRenderCache symbolCache, long iteration)
        {
            var style = ((MarkerLabelStyle)istyle);

            if (!viewport.ToExtent().Intersects(feature.Extent))
                return true; // We didn't render anything, but we still did so successfully

            var text = style.GetLabelText(feature);

            if (string.IsNullOrEmpty(text))
                return true;

            var position = feature.Extent as MRect;
            var dest = viewport.WorldToScreen(position.BottomLeft);


            var zoom = 1 / (float)viewport.Resolution;

            canvas.Translate((float)dest.X, (float)dest.Y);
            canvas.Scale(0.7f, 0.7f);

            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);

            SKPoint textOffset = new SKPoint((float)style.Offset.X, (float)style.Offset.Y);
            //if (!string.IsNullOrEmpty(style.text))
            {
                if (zoom > 1.5f)
                    zoom = 1.5f;
                if (zoom < 0.5f)
                    zoom = 0.5f;


                renderPaint.TextSize = 32 * zoom;
                SKRect bounds = new SKRect();
                //renderPaint.GetFontMetrics(out var fontMet);
                //renderPaint.MeasureText(style.text, ref bounds);
                //var textHeight = (style.finalSize.Height / 2);
                //if (zoom < 1.5f)
                //{
                //    textHeight += bounds.Height/2;
                //}

                //textOffset = new SKPoint(style.finalSize.Width, textHeight);
            }


            //if (style.shadow)
            //{
            //    var targetRect = style.finalRect;
            //    var shadowOffset = style.ShadowOffset;
            //
            //    targetRect.Offset(shadowOffset);
            //    renderPaint.Color = new SKColor(0, 0, 0, (byte)(style.Opacity * 0.8f * 255));
            //    renderPaint.ColorFilter = SkiaSharp.SKColorFilter.CreateLighting(renderPaint.Color, new SKColor(0, 0, 0));
            //
            //    if (style.SymbolRotation != 0) canvas.RotateDegrees(style.SymbolRotation, 0.0f, 0.0f);
            //    canvas.DrawImage(style.markerIcon, targetRect, renderPaint);
            //    if (style.SymbolRotation != 0) canvas.RotateDegrees(-style.SymbolRotation, 0.0f, 0.0f); //No rotation for text
            //    //canvas.DrawRect(targetRect, renderPaint);
            //
            //    if (!string.IsNullOrEmpty(style.text) && zoom > 1)
            //    {
            //        canvas.DrawText(style.text, textOffset + style.ShadowOffset, renderPaint);
            //    }
            //}

            //renderPaint.ColorFilter = style.colorFilter;
            //if (style.SymbolRotation != 0) canvas.RotateDegrees(style.SymbolRotation, 0.0f, 0.0f);
            //canvas.DrawImage(style.markerIcon, style.finalRect, renderPaint);
            {
                //if (style.SymbolRotation != 0) canvas.RotateDegrees(-style.SymbolRotation, 0.0f, 0.0f); //No rotation for text
                renderPaint.Color = style.ForeColor.ToSkia().WithAlpha((byte)(style.Opacity * 255));
                renderPaint.ColorFilter = null;
                canvas.DrawText(text, textOffset, renderPaint);
            }


            return true;
        }
    }
}
