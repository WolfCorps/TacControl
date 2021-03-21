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
        public ActiveMarker marker { get; private set; }
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

            if (string.IsNullOrEmpty(marker.size))
                marker.size = "64,64";

            if (marker.shape == "ICON")
            {
                if (string.IsNullOrEmpty(marker.type)) return; //Can happen, somehow


                if (!GameState.Instance.marker.markerTypes.ContainsKey(marker.type)) throw new InvalidOperationException();
                var markerType = GameState.Instance.marker.markerTypes[marker.type];

                if (marker.color == "Default")
                    markerColor = new MarkerColor
                    { color = markerType.color, name = markerType.name };

                try
                {
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
                    marker.PropertyChanged += OnMarkerOnPropertyChangedIcon;
                }
                catch (System.FormatException ex)
                {

                }

               
            }
            else if (marker.shape == "RECTANGLE" || marker.shape == "ELLIPSE")
            {

                if (!GameState.Instance.marker.markerBrushes.ContainsKey(marker.brush)) throw new InvalidOperationException();
                var markerBrush = GameState.Instance.marker.markerBrushes[marker.brush];

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
                    color = markerColor.ToSKColor(),
                    Opacity = marker.alpha
                };
                Styles.Add(tiledBitmap);


                MarkerCache.Instance.GetImage(markerBrush, null).ContinueWith(
                    (image) =>
                    {
                        tiledBitmap.image = image.Result;
                    });

                marker.PropertyChanged += OnMarkerOnPropertyChangedTiled;
            }
            else if (marker.shape == "POLYLINE")
            {
                if (marker.polyline.Count == 0) return;
                Geometry = new BoundBox(marker.polyline);

                var polyMarker = new PolylineMarkerStyle(marker.polyline)
                {
                    color = markerColor.ToSKColor()
                };
                Styles.Add(polyMarker);

                //Polylines have no propertychanged as (in ACE) they cannot be edited
            }


            

        }

        protected override void Dispose(bool disposing)
        {
            marker.PropertyChanged -= OnMarkerOnPropertyChangedIcon;
            marker.PropertyChanged -= OnMarkerOnPropertyChangedTiled;
            base.Dispose(disposing);
        }

        void OnMarkerOnPropertyChangedIcon(object a, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveMarker.text))
            {
                this["Label"] = marker.text;
                //#TODO update Label style
                foreach (var label in Styles.Where(x => x is MarkerIconStyle))
                    (label as MarkerIconStyle).text = marker.text;

                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.pos))
            {
                Geometry = new Point(marker.pos[0], marker.pos[1]);
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.dir))
            {
                foreach (var sym in Styles.Where(x => x is MarkerIconStyle)) //#TODO just store the style in a variable
                    (sym as MarkerIconStyle).SymbolRotation = marker.dir;
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
                iconStyle.shadow = markerType.shadow;

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
            else if (e.PropertyName == nameof(ActiveMarker.alpha))
            {
                MarkerIconStyle iconStyle = (MarkerIconStyle)Styles.First(x => x is MarkerIconStyle);
                if (iconStyle == null) return;

                iconStyle.Opacity = marker.alpha;
                DataHasChanged();
            }
        }


        private void OnMarkerOnPropertyChangedTiled(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveMarker.text))
            {
                this["Label"] = marker.text;
            }
            else if (e.PropertyName == nameof(ActiveMarker.pos))
            {

                TiledBitmapStyle iconStyle = (TiledBitmapStyle)Styles.First(x => x is TiledBitmapStyle); //#TODO make interface for both styles with setters for pos/dir/color/stuff
                if (iconStyle == null) return;


                CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.NumberDecimalSeparator = ".";

                var markerSize = marker.size.Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToArray();
                var center = new Point(marker.pos[0], marker.pos[1]);
                Geometry = new BoundBox(center.Offset(-markerSize[0], -markerSize[1]), center.Offset(markerSize[0], markerSize[1]));
                iconStyle.rect = new SkiaSharp.SKRect(-markerSize[0], -markerSize[1], markerSize[0], markerSize[1]);
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.dir))
            {
                foreach (var sym in Styles.Where(x => x is TiledBitmapStyle)) //#TODO just store the style in a variable
                    (sym as TiledBitmapStyle).rotation = marker.dir;
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.brush))
            {
                TiledBitmapStyle iconStyle = (TiledBitmapStyle)Styles.First(x => x is TiledBitmapStyle);
                if (iconStyle == null) return;

                if (!GameState.Instance.marker.markerBrushes.ContainsKey(marker.brush)) return;
                var markerBrush = GameState.Instance.marker.markerBrushes[marker.brush];

                if (marker.color == "Default") markerColor = new MarkerColor { color = marker.color, name = markerBrush.name };

                iconStyle.color = markerColor.ToSKColor();

                //set rect
                MarkerCache.Instance.GetImage(markerBrush, null).ContinueWith(
                    (image) =>
                    {
                        iconStyle.image = image.Result;
                    });
                DataHasChanged();
            }
            else if (e.PropertyName == nameof(ActiveMarker.color))
            {
                TiledBitmapStyle iconStyle = (TiledBitmapStyle)Styles.First(x => x is TiledBitmapStyle);
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
            else if (e.PropertyName == nameof(ActiveMarker.alpha))
            {
                TiledBitmapStyle iconStyle = (TiledBitmapStyle)Styles.First(x => x is TiledBitmapStyle);
                if (iconStyle == null) return;

                iconStyle.Opacity = marker.alpha;
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
        public ILayer MapMarkerLayer { get; private set; }
        private BoundingBox _boundingBox;
        private readonly MarkerVisibilityManager _visibilityManager;

        public string CRS { get; set; } = "";

        private Dictionary<string, IFeature> features = new Dictionary<string, IFeature>();


        public MapMarkerProvider(ILayer mapMarkerLayer, BoundingBox boundingBox, MarkerVisibilityManager visibilityManager)
        {
            MapMarkerLayer = mapMarkerLayer;
            _boundingBox = boundingBox;
            _visibilityManager = visibilityManager;
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

            _visibilityManager.OnUpdated += () =>
            {
                MapMarkerLayer.DataHasChanged();
            };


            MapMarkerLayer.DataHasChanged();

            OnMarkersUpdated();
        }

        private void OnMarkersUpdated()
        {
            //if (!GameState.Instance.marker.markerColors.Any() || !GameState.Instance.marker.markerTypes.Any()) return; //Don't create markers if we aren't ready
            foreach (var keyValuePair in GameState.Instance.marker.markers)
            {
                if (!features.ContainsKey(keyValuePair.Key))
                    AddMarker(keyValuePair.Value, false);
            }

            features.Where(x => !GameState.Instance.marker.markers.ContainsKey(x.Key)).Select(x => x.Key)
                .ToList()
                .ForEach(x => RemoveMarker(x, false));

            MapMarkerLayer.DataHasChanged();
        }


        public void AddMarker(ActiveMarker marker, bool fireDataChanged = true)
        {
            if (features.ContainsKey(marker.id)) return;

            try
            {
                var feature = new MarkerFeature(marker);
                feature.DataChanged += (x,y) => MapMarkerLayer.DataHasChanged();
                features[marker.id] = feature;
                if (fireDataChanged) MapMarkerLayer.DataHasChanged();
            }
            catch (InvalidOperationException ex)
            {

            }
        }

        public void RemoveMarker(string id, bool fireDataChanged = true)
        {
            features.Remove(id);
            if (fireDataChanged) MapMarkerLayer.DataHasChanged();
        }

        public virtual IEnumerable<IFeature> GetFeaturesInView(
          BoundingBox box,
          double resolution)
        {
            if (box == null)
                throw new ArgumentNullException(nameof(box));

            BoundingBox grownBox = box.Grow(resolution);

            return features.Values.Where(f => f.Geometry != null && f.Geometry.BoundingBox.Intersects(grownBox) && _visibilityManager.IsVisible(((MarkerFeature)f).marker)).ToList();
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
