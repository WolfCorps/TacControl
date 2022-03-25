using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Providers;
using Mapsui.Styles;
using SkiaSharp;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    // Mapsui.Layers.PointFeature, but with editable point
    public class GPSTrackerFeature : BaseFeature, IFeature
    {
        public GPSTracker Tracker { get; private set; }
        private MarkerIconStyle imgStyle;
        private LabelStyle lblStyle;
        private LabelStyle heightStyle;
        private VelocityIndicatorStyle velStyle;
        public GPSTrackerFeature(GPSTracker tracker)
        {
            Tracker = tracker;
            this["Label"] = tracker.displayName;
            imgStyle = new MarkerIconStyle
            {
                SymbolRotation = 0,
                Opacity = 1,
                size = new float[]{32,32},
                typeSize = 1,
                color = SKColors.Black,
                shadow = false,
                text = tracker.displayName

            };


            Styles.Add(imgStyle);

            var markerType = GameState.Instance.marker.markerTypes["hd_join"];
            var markerColor = GameState.Instance.marker.markerColors["ColorBlack"];

            MarkerCache.Instance.GetImage(markerType, markerColor)
                .ContinueWith(
                    (image) =>
                    {
                        imgStyle.markerIcon = image.Result;
                    });



            lblStyle = new MarkerLabelStyle(tracker.displayName, markerType, markerColor);
            Styles.Add(lblStyle);

            heightStyle = new MarkerLabelStyle(tracker.displayName, markerType, markerColor);
            heightStyle.Offset = new Offset(0, markerType.size, false);
            Styles.Add(heightStyle);


            velStyle = new VelocityIndicatorStyle {velocity = new Vector3(tracker.vel[0], tracker.vel[1], 0f) };
            Styles.Add(velStyle);

            Point = new MPoint(tracker.pos[0], tracker.pos[1]);
        }


        public void SetDisplayName(string newName)
        {
            this["Label"] = newName;
            lblStyle.Text = newName;
            imgStyle.text = newName;
        }

        public void SetPosition(MPoint newPos)
        {
            Point = newPos;
            heightStyle.Text = $"({Tracker.pos[2]:F0}m)";
        }

        public void UpdateVelocity()
        {
            velStyle.velocity.X = Tracker.vel[0];
            velStyle.velocity.Y = Tracker.vel[1];
        }


        public MPoint Point { get; private set; }
        public MRect Extent => Point.MRect;

        public void CoordinateVisitor(Action<double, double, CoordinateSetter> visit)
        {
            visit(Point.X, Point.Y, (x, y) => {
                Point.X = x;
                Point.Y = y;
            });
        }
    }
}
