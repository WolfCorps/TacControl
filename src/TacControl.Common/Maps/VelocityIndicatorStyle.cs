using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Mapsui.Styles;

namespace TacControl.Common.Maps
{
    public class VelocityIndicatorStyle : IStyle
    {
        public Vector3 velocity;

        public double MinVisible { get; set; } = 0.0f;
        public double MaxVisible { get; set; } = double.MaxValue;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1f;
    }
}
