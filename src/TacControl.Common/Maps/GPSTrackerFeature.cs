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
    public class GPSTrackerFeature : PointFeature, IFeature
    {
        public GPSTracker Tracker { get; private set; }
        private MarkerIconStyle imgStyle;
        private VelocityIndicatorStyle velStyle;
        public GPSTrackerFeature(GPSTracker tracker) : base(new MPoint(tracker.pos[0], tracker.pos[1]))
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
                shadow = false
            };


            Styles.Add(imgStyle);
            UpdateTextLabel();

            var markerType = GameState.Instance.marker.markerTypes["hd_join"];
            var markerColor = GameState.Instance.marker.markerColors["ColorBlack"];

            MarkerCache.Instance.GetImage(markerType, markerColor)
                .ContinueWith(
                    (image) =>
                    {
                        imgStyle.markerIcon = image.Result;
                    });

            velStyle = new VelocityIndicatorStyle {velocity = new Vector3(tracker.vel[0], tracker.vel[1], 0f) };
            Styles.Add(velStyle);

            Point = new MPoint(tracker.pos[0], tracker.pos[1]);
        }


        public void SetDisplayName(string newName)
        {
            UpdateTextLabel();
        }

        public void SetPosition(MPoint newPos)
        {
            Point = newPos;
            UpdateTextLabel();
        }

        public void UpdateVelocity()
        {
            velStyle.velocity.X = Tracker.vel[0];
            velStyle.velocity.Y = Tracker.vel[1];
            UpdateTextLabel();
        }

        public void UpdateTextLabel()
        {
            imgStyle.text = $"{Tracker.displayName}\n" +
                            $"({Tracker.pos[2]:F0}m)\n" +
                            $"{Tracker.GetSpeedInKMH():F0}km/h";
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
