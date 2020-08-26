using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Mapsui.Fetcher;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using SkiaSharp;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{

    public class MarkerFeature : Feature, IDisposable
    {
        private ActiveMarker marker;
        private MarkerColor markerColor;

        public event DataChangedEventHandler DataChanged;

        public MarkerFeature(ActiveMarker marker)
        {
            this.marker = marker;

            this["Label"] = marker.text;
            Geometry = new Point(marker.pos[0], marker.pos[1]);

            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";

            if (!GameState.Instance.marker.markerColors.ContainsKey(marker.color)) throw new InvalidOperationException();
            markerColor = GameState.Instance.marker.markerColors[marker.color];

            if (marker.shape == "ICON")
            {

                if (!GameState.Instance.marker.markerTypes.ContainsKey(marker.type)) throw new InvalidOperationException();
                var markerType = GameState.Instance.marker.markerTypes[marker.type];

                if (marker.color == "Default")
                    markerColor = new MarkerColor
                    { color = markerType.color, name = markerType.name };


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

                Styles.Add(symStyle);
            }
            else if (marker.shape == "RECTANGLE" || marker.shape == "ELLIPSE")
            {

                if (!GameState.Instance.marker.markerBrushes.ContainsKey(marker.brush)) throw new InvalidOperationException();
                var markerBrush = GameState.Instance.marker.markerBrushes[marker.brush];

                if (marker.size == null)
                    marker.size = "64,44";

                var markerSize = marker.size.Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToArray();


                var center = new Point(marker.pos[0], marker.pos[1]);

                //set rect
                Geometry = new BoundBox(center.Offset(-markerSize[0], -markerSize[1]), center.Offset(markerSize[0], markerSize[1]));

                var tiledBitmap = new TiledBitmapStyle
                {
                    image = null,
                    rect = new SkiaSharp.SKRect(-markerSize[0], -markerSize[1], markerSize[0], markerSize[1]),
                    rotation = marker.dir,
                    ellipse = marker.shape == "ELLIPSE",
                    border = markerBrush.drawBorder,
                    color = markerColor.ToSKColor()
                };
                Styles.Add(tiledBitmap);


                MarkerCache.Instance.GetImage(markerBrush, null).ContinueWith(
                    (image) =>
                    {
                        tiledBitmap.image = image.Result;
                    });


            }
            else if (marker.shape == "POLYLINE")
            {
                Geometry = new BoundBox(marker.polyline);

                var polyMarker = new PolylineMarkerStyle(marker.polyline)
                {
                    color = markerColor.ToSKColor()
                };
                Styles.Add(polyMarker);
            }


            

            marker.PropertyChanged += OnMarkerOnPropertyChanged;

        }

        protected override void Dispose(bool disposing)
        {
            marker.PropertyChanged -= OnMarkerOnPropertyChanged;
            base.Dispose(disposing);
        }

        void OnMarkerOnPropertyChanged(object a, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveMarker.text))
            {
                this["Label"] = marker.text;
                //#TODO update Label style
                foreach (var label in Styles.Where(x => x is MarkerLabelStyle)) (label as MarkerLabelStyle).Text = marker.text;

                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.pos))
            {
                Geometry = new Point(marker.pos[0], marker.pos[1]);
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.dir))
            {
                foreach (var sym in Styles.Where(x => x is MarkerIconStyle)) (sym as SymbolStyle).SymbolRotation = marker.dir;
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.type))
            {
                MarkerIconStyle iconStyle = (MarkerIconStyle)Styles.First(x => x is MarkerIconStyle);
                if (iconStyle == null) return;

                if (!GameState.Instance.marker.markerTypes.ContainsKey(marker.type)) return;
                var markerType = GameState.Instance.marker.markerTypes[marker.type];

                if (marker.color == "Default") markerColor = new MarkerColor { color = markerType.color, name = markerType.name };

                iconStyle.color = markerColor.ToSKColor();
                iconStyle.typeSize = markerType.size;

                MarkerCache.Instance.GetImage(markerType, null)
                    .ContinueWith((image) => { iconStyle.markerIcon = image.Result; });
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.color))
            {
                MarkerIconStyle iconStyle = (MarkerIconStyle)Styles.First(x => x is MarkerIconStyle);
                if (iconStyle == null) return;


                if (!GameState.Instance.marker.markerColors.ContainsKey(marker.color)) return;
                markerColor = GameState.Instance.marker.markerColors[marker.color];



                if (marker.color == "Default")
                {
                    if (!GameState.Instance.marker.markerTypes.ContainsKey(marker.type)) return;
                    var markerType = GameState.Instance.marker.markerTypes[marker.type];

                    markerColor = new MarkerColor { color = markerType.color, name = markerType.name };
                }

                iconStyle.color = markerColor.ToSKColor();
                DataHasChanged();
            }
        }

        private void DataHasChanged()
        {
            DataChanged?.Invoke(this, null);
        }
    }
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
            MapMarkerLayer.DataHasChanged();

            OnMarkersUpdated();
        }

        private void OnMarkersUpdated()
        {
            foreach (var keyValuePair in GameState.Instance.marker.markers)
            {
                if (!features.ContainsKey(keyValuePair.Key))
                    AddMarker(keyValuePair.Value);
            }

            features.Where(x => !GameState.Instance.marker.markers.ContainsKey(x.Key)).Select(x => x.Key)
                .ToList()
                .ForEach(RemoveMarker);

            MapMarkerLayer.DataHasChanged();
        }


        public void AddMarker(ActiveMarker marker)
        {
            if (features.ContainsKey(marker.id)) return;

            try
            {
                var feature = new MarkerFeature(marker);
                feature.DataChanged += (x,y) => MapMarkerLayer.DataHasChanged();
                features[marker.id] = feature;
            }
            catch (InvalidOperationException ex)
            {

            }
        }

        public void RemoveMarker(string id)
        {
            features.Remove(id);
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
