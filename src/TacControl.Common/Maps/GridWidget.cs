using System;
using System.Collections.Generic;
using System.Text;
using Mapsui;
using Mapsui.Widgets;

namespace TacControl.Common.Maps
{
    public class GridWidget : IWidget
    {
        public event EventHandler<WidgetEventArgs> Tapped;
        public event EventHandler<WidgetEventArgs> PointerPressed;
        public event EventHandler<WidgetEventArgs> PointerMoved;
        public event EventHandler<WidgetEventArgs> PointerReleased;

        public bool HandleWidgetTouched(Navigator navigator, MPoint position)
        {
            return false;
        }

        public void OnTapped(WidgetEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnPointerPressed(WidgetEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnPointerMoved(WidgetEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnPointerReleased(WidgetEventArgs e)
        {
            throw new NotImplementedException();
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public float MarginX { get; set; }
        public float MarginY { get; set; }
        public MRect Envelope { get; set; }
        public bool Enabled { get; set; } = true;
        public MRect Margin { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public MPoint Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Width { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Height { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public InputAreaType InputAreaType => throw new NotImplementedException();

        public bool InputTransparent { get => true; init => throw new NotImplementedException(); } // Cannot click on grid
    }
}
