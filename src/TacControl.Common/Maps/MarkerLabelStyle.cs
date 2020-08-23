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


            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";

            var colorArr = color.color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToList();

            var textColor = new Color((byte)(colorArr[0] * 255), (byte)(colorArr[1] * 255),
                (byte)(colorArr[2] * 255));

            Font = markerFont;
            ForeColor = textColor;
            HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left;
        }
    }
}
