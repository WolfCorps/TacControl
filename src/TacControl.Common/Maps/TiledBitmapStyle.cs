using System;
using System.Collections.Generic;
using System.Text;
using Mapsui.Styles;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class TiledBitmapStyle: IStyle
    {
        public SkiaSharp.SKImage image;
        public SkiaSharp.SKRect rect;
        public float rotation;
        public bool ellipse = false;
        public bool border;
        public SKColor color;

        public double MinVisible { get; set; } = 0.0f;
        public double MaxVisible { get; set; } = double.MaxValue;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1f;
    }
}
