using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using SkiaSharp;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public class MapMarkerProvider : IProvider, IDisposable
    {
        public Layer MapMarkerLayer { get; private set; }
        private BoundingBox _boundingBox;

        public string CRS { get; set; } = "";

        private Dictionary<string, IFeature> features = new Dictionary<string, IFeature>();


        public MapMarkerProvider(Layer mapMarkerLayer, BoundingBox boundingBox)
        {
            MapMarkerLayer = mapMarkerLayer;
            _boundingBox = boundingBox;
            //this._boundingBox = MemoryProvider.GetExtents(this.Features);
            GameState.Instance.marker.markers.CollectionChanged += (a, e) => OnMarkersUpdated();

            GameState.Instance.marker.PropertyChanged += (a, e) => //#TODO probably don't need this anymore, same on GPSTracker
            {
                if (e.PropertyName == nameof(ModuleMarker.markers) && GameState.Instance.marker.markers != null)
                {
                    GameState.Instance.marker.markers.CollectionChanged += (b, c) => OnMarkersUpdated();
                    OnMarkersUpdated();
                }

            };


            OnMarkersUpdated();
        }

        private void OnMarkersUpdated()
        {
            foreach (var keyValuePair in GameState.Instance.marker.markers)
            {

                IFeature feature;

                if (!features.ContainsKey(keyValuePair.Key))
                {


                    CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                    ci.NumberFormat.NumberDecimalSeparator = ".";

                    var marker = keyValuePair.Value;

                    if (!GameState.Instance.marker.markerColors.ContainsKey(marker.color)) continue;
                    var markerColor = GameState.Instance.marker.markerColors[marker.color];

                    feature = new Feature { ["Label"] = marker.text, Geometry = new Point(marker.pos[0], marker.pos[1]) };
                    
                    if (marker.shape == "ICON")
                    {

                        if (!GameState.Instance.marker.markerTypes.ContainsKey(marker.type)) continue;
                        var markerType = GameState.Instance.marker.markerTypes[marker.type];

                        if (marker.color == "Default")
                            markerColor = new ModuleMarker.MarkerColor
                                {color = markerType.color, name = markerType.name};


                        var symStyle = new MarkerIconStyle
                        {
                            SymbolRotation = marker.dir,
                            Opacity = marker.alpha,
                            size = marker.size.Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToArray(),
                            typeSize = markerType.size,
                            color = markerColor.ToSKColor(),
                            shadow = markerType.shadow,
                            text = marker.text
                        };

                        MarkerCache.Instance.GetImage(markerType, null)
                            .ContinueWith(
                                (image) =>
                                {
                                    symStyle.markerIcon = image.Result;
                                });

                        feature.Styles.Add(symStyle);
                    }
                    else if (marker.shape == "RECTANGLE" || marker.shape == "ELLIPSE")
                    {

                        if (!GameState.Instance.marker.markerBrushes.ContainsKey(marker.brush)) continue;
                        var markerBrush = GameState.Instance.marker.markerBrushes[marker.brush];

                        if (marker.size == null)
                            marker.size = "64,44";

                        var markerSize = marker.size.Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToArray();


                        var center = new Point(marker.pos[0], marker.pos[1]);
                        
                        //set rect
                        feature.Geometry = new BoundBox(center.Offset(-markerSize[0], -markerSize[1]), center.Offset(markerSize[0], markerSize[1]));

                        var tiledBitmap = new TiledBitmapStyle {
                            image = null,
                            rect = new SkiaSharp.SKRect(-markerSize[0], -markerSize[1], markerSize[0], markerSize[1]),
                            rotation = marker.dir,
                            ellipse = marker.shape == "ELLIPSE",
                            border = markerBrush.drawBorder,
                            color = markerColor.ToSKColor()
                        };
                        feature.Styles.Add(tiledBitmap);


                        MarkerCache.Instance.GetImage(markerBrush, null).ContinueWith(
                            (image) =>
                            {
                                tiledBitmap.image = image.Result;
                            });


                    } else if (marker.shape == "POLYLINE")
                    {
                        feature.Geometry = new BoundBox(marker.polyline);

                        var polyMarker = new PolylineMarkerStyle(marker.polyline)
                        {
                            color = markerColor.ToSKColor()
                        };
                        feature.Styles.Add(polyMarker);
                    }

                    features[keyValuePair.Key] = feature;

                    keyValuePair.Value.PropertyChanged += (a, e) =>
                    {
                        if (e.PropertyName == nameof(ModuleMarker.ActiveMarker.text))
                        {
                            feature["Label"] = keyValuePair.Value.text;
                            //#TODO update Label style
                            foreach (var label in feature.Styles.Where(x => x is MarkerLabelStyle))
                                (label as MarkerLabelStyle).Text = keyValuePair.Value.text;

                            MapMarkerLayer.DataHasChanged();
                        }

                        else if (e.PropertyName == nameof(ModuleMarker.ActiveMarker.pos))
                        {
                            feature.Geometry = new Point(keyValuePair.Value.pos[0], keyValuePair.Value.pos[1]);
                            MapMarkerLayer.DataHasChanged();
                        }
                        else if (e.PropertyName == nameof(ModuleMarker.ActiveMarker.dir))
                        {
                            foreach (var sym in feature.Styles.Where(x => x is SymbolStyle))
                                (sym as SymbolStyle).SymbolRotation = keyValuePair.Value.dir;
                            MapMarkerLayer.DataHasChanged();
                        }
                    };
                }
            }

            var toRemove = features.Where(x => !GameState.Instance.marker.markers.ContainsKey(x.Key)).Select(x => x.Key)
                .ToList();

            toRemove.ForEach(x => features.Remove(x));

            MapMarkerLayer.DataHasChanged();
        }

        public virtual IEnumerable<IFeature> GetFeaturesInView(
          BoundingBox box,
          double resolution)
        {
            if (box == null)
                throw new ArgumentNullException(nameof(box));

            BoundingBox grownBox = box.Grow(resolution);

            return features.Values.Where(f => f.Geometry != null && f.Geometry.BoundingBox.Intersects(grownBox)).ToList();
        }

        public BoundingBox GetExtents()
        {
            return this._boundingBox;
        }

        private static BoundingBox GetExtents(IReadOnlyList<IFeature> features)
        {
            BoundingBox boundingBox = (BoundingBox)null;
            foreach (IFeature feature in (IEnumerable<IFeature>)features)
            {
                if (!feature.Geometry.IsEmpty())
                    boundingBox = boundingBox == null ? feature.Geometry.BoundingBox : boundingBox.Join(feature.Geometry.BoundingBox);
            }
            return boundingBox;
        }

        public void Dispose()
        {

        }
    }

}
