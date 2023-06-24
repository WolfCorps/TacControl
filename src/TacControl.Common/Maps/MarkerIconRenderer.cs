using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;
using SkiaTextRenderer;
using TacControl.Common.Maps.Util;
using FontStyle = SkiaTextRenderer.FontStyle;

namespace TacControl.Common.Maps
{
    public class MarkerIconRenderer : ISkiaStyleRenderer
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
            var style = ((MarkerIconStyle)istyle);

            if (!viewport.ToExtent().Intersects(feature.Extent))
                return true; // We didn't render anything, but we still did so successfully

            if (style.markerIcon == null)
                return true;

            var position = feature.Extent as MRect;
            var dest = viewport.WorldToScreen(position.BottomLeft);


            var zoom = 1 / (float)viewport.Resolution;

            canvas.Translate((float)dest.X, (float)dest.Y);
            canvas.Scale(0.7f, 0.7f);

            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);

            SKPoint textOffset = new SKPoint(style.finalSize.Width, style.finalSize.Height / 2);
            if (!string.IsNullOrEmpty(style.text))
            {
                if (zoom > 1.5f)
                    zoom = 1.5f;
                if (zoom < 0.5f)
                    zoom = 0.5f;


                renderPaint.TextSize = 32 * zoom;
                SKRect bounds = new SKRect();
                //renderPaint.GetFontMetrics(out var fontMet);
                //renderPaint.MeasureText(style.text, ref bounds);
                var textHeight = (style.finalSize.Height / 2);
                //if (zoom < 1.5f)
                //{
                //    textHeight += bounds.Height/2;
                //}

                textOffset = new SKPoint(style.finalSize.Width, textHeight );
            }


            if (style.shadow)
            {
                var targetRect = style.finalRect;
                var shadowOffset = style.ShadowOffset;

                targetRect.Offset(shadowOffset);
                renderPaint.Color = new SKColor(0, 0, 0, (byte) (style.Opacity * 0.8f * 255));
                renderPaint.ColorFilter = SkiaSharp.SKColorFilter.CreateLighting(renderPaint.Color, new SKColor(0, 0, 0));

                if (style.SymbolRotation != 0) canvas.RotateDegrees(style.SymbolRotation, 0.0f, 0.0f);
                canvas.DrawImage(style.markerIcon, targetRect, renderPaint);
                if (style.SymbolRotation != 0) canvas.RotateDegrees(-style.SymbolRotation, 0.0f, 0.0f); //No rotation for text
                //canvas.DrawRect(targetRect, renderPaint);

                if (!string.IsNullOrEmpty(style.text) && zoom > 1)
                {
                    canvas.DrawText(style.text, textOffset + style.ShadowOffset, renderPaint);
                }
            }



            renderPaint.Color = SKColor.Empty.WithAlpha((byte)(style.Opacity * 255));
            renderPaint.ColorFilter = style.colorFilter;
            if (style.SymbolRotation != 0) canvas.RotateDegrees(style.SymbolRotation, 0.0f, 0.0f);
            canvas.DrawImage(style.markerIcon, style.finalRect, renderPaint);
            if (!string.IsNullOrEmpty(style.text))
            {
                if (style.SymbolRotation != 0) canvas.RotateDegrees(-style.SymbolRotation, 0.0f, 0.0f); //No rotation for text
                renderPaint.Color = style.color.WithAlpha((byte)(style.Opacity * 255));
                renderPaint.ColorFilter = null;
                //canvas.DrawText(style.text, textOffset, renderPaint);

                // This is broken, it somehow adds a empty rectangle on first line and pushes everything to second
                //canvas.Translate(textOffset);
                //TextRendererSk.DrawText(canvas, style.text, new SkiaTextRenderer.Font(renderPaint.Typeface, renderPaint.TextSize), SKRect.Empty, renderPaint.Color, TextFormatFlags.Left | TextFormatFlags.NoClipping | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                SkiaTextBox.Draw(style.text, textOffset, canvas, renderPaint);
            }


            return true;
        }
    }
}
