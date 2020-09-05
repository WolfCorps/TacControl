using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Geometries;
using Mapsui.Widgets;

namespace TacControl.Common.Maps
{
    public class GridWidget : IWidget
    {
        public bool HandleWidgetTouched(INavigator navigator, Point position)
        {
            return false;
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public float MarginX { get; set; }
        public float MarginY { get; set; }
        public BoundingBox Envelope { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
