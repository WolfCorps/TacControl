using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mapsui.Styles;
using SkiaSharp;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public class PolylineMarkerStyle : IStyle
    {
        public SKColor color;
        public SKPath path;
        public SKPoint start;

        public PolylineMarkerStyle(ObservableCollection<float[]> markerPolyline)
        {
            path = new SKPath();
            start = new SKPoint(markerPolyline.First()[0], markerPolyline.First()[1]);

            var startOffs = new SKPoint(start.X, start.Y);
            startOffs.Offset((float)-start.X, (float)-start.Y);
            startOffs.Offset(0, (float)-startOffs.Y * 2);
            path.MoveTo(startOffs);

            for (int i = 1; i < markerPolyline.Count; i++)
            {
                var p = markerPolyline[i];
                var point = new SKPoint(p[0], p[1]);
                point.Offset((float)-start.X, (float)-start.Y);
                point.Offset(0, (float)-point.Y*2);

                path.LineTo(point);
            }

            markerPolyline.CollectionChanged += (x, y) =>
            {
                foreach (var it in y.NewItems)
                {
                    var p = (float[])it;
                    var point = new SKPoint(p[0], p[1]);
                    point.Offset((float)-start.X, (float)-start.Y);
                    point.Offset(0, (float)-point.Y * 2);

                    path.LineTo(point);
                }
            };



        }

        public double MinVisible { get; set; } = 0.0f;
        public double MaxVisible { get; set; } = double.MaxValue;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1f;
        /*

          ParamEntryVal cfg = cls >> "LineMarker";
  _thinLineWidth   = cfg >> "lineWidthThin";
  _thickLineWidth  = cfg >> "lineWidthThick";
  _minLineDistance = cfg >> "lineDistanceMin";
  _minLineLength   = cfg >> "lineLengthMin";


        		class LineMarker
		{
			textureComboBoxColor = "#(argb,8,8,3)color(1,1,1,1)";
			lineWidthThin = 0.008;
			lineWidthThick = 0.014;
			lineDistanceMin = 3e-005;
			lineLengthMin = 5;
		};



          //set color and update alpha according to marker alpha property    
  PackedColor fadedMarkerColor = ModAlpha(mInfo.color, mInfo.alpha * alpha);



               float width = mInfo.selected ? _markerCfg._thickLineWidth : _markerCfg._thinLineWidth;
      width *= _wScreen;
      for (int i = 0; i < mInfo.linePointCoodinates.Size() - 3; i += 2)
      {
        DrawLine(
          Vector3(mInfo.linePointCoodinates.Get(i + 0), 0, mInfo.linePointCoodinates.Get(i + 1)),
          Vector3(mInfo.linePointCoodinates.Get(i + 2), 0, mInfo.linePointCoodinates.Get(i + 3)),
          fadedMarkerColor, width);
      }
         
         
         */

    }
}
