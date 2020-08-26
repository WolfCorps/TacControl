using System;
using System.Collections.Generic;
using System.Text;
using Mapsui.Styles;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class MarkerIconStyle : IStyle
    {
        public SKImage markerIcon;
        public float SymbolRotation;
        public float typeSize;
        public float[] size;

        public SKPoint ShadowOffsetRotated
        {
            get
            {
                var size = finalSize;
                return SKMatrix.CreateRotation(-SymbolRotation).MapPoint(ShadowOffset);
            }
        }

        public SKPoint ShadowOffset
        {
            get
            {
                return new SKPoint(typeSize * 0.12f, typeSize * 0.12f);
            }
        }

        public SKSize finalSize
        {
            get
            {
                return new SKSize(size[0] * typeSize, size[1] * typeSize); //#TODO cache this, don't recalc on rendering
            }
        }

        public SKRect finalRect
        {
            get
            {
                return new SKRect(-finalSize.Width, -finalSize.Height, finalSize.Width, finalSize.Height);
            }
        }



        public SKColorFilter colorFilter;
        public bool shadow;
        public string text;

        private SKColor colorInt;

        public SKColor color
        {
            set
            {
                colorFilter = SkiaSharp.SKColorFilter.CreateLighting(value, new SKColor(0, 0, 0));
                colorInt = value;
            }
            get
            {
                return colorInt;
            }
        }


        public double MinVisible { get; set; } = 0.0f;
        public double MaxVisible { get; set; } = double.MaxValue;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1f;
    }
}
