using System;
using System.Collections.Generic;
using System.Text;
using Mapsui.Styles;

namespace TacControl.Common.Maps
{
    public class NullStlye : IStyle
    {
        public double MinVisible { get; set; } = 0.0f;
        public double MaxVisible { get; set; } = double.MaxValue;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1f;
    }
}
