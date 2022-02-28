using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Widgets;

namespace TacControl.Common.Maps
{
    public class GridWidget : IWidget
    {
        public bool HandleWidgetTouched(INavigator navigator, MPoint position)
        {
            return false;
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public float MarginX { get; set; }
        public float MarginY { get; set; }
        public MRect Envelope { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
