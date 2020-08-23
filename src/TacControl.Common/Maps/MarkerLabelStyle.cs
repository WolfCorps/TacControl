using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mapsui.Styles;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public class MarkerLabelStyle : LabelStyle
    {
        static Font markerFont = new Mapsui.Styles.Font { FontFamily = "Verdana", Size = 24, Bold = true };
        public MarkerLabelStyle(string text, ModuleMarker.MarkerType type, ModuleMarker.MarkerColor color)
        {
            Text = text;
            
            BackColor = null;
            Halo = null;
            Offset = new Offset(type.size, 0, false);
            Font = markerFont;
            ForeColor = color.ToMapsuiColor();
            HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left;
        }
    }
}
