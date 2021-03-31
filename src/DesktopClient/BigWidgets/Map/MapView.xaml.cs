using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using Mapsui;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Rendering.Skia.SkiaWidgets;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Widgets.Zoom;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using TacControl.Annotations;
using TacControl.Common;
using TacControl.Common.Maps;
using TacControl.Common.Modules;
using static TacControl.Common.Modules.ModuleMarker;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Geometry = Mapsui.Geometries.Geometry;
using LineStringRenderer = Mapsui.Rendering.Skia.LineStringRenderer;
using Math = System.Math;
using MultiLineStringRenderer = Mapsui.Rendering.Skia.MultiLineStringRenderer;
using MultiPolygonRenderer = Mapsui.Rendering.Skia.MultiPolygonRenderer;
using Path = System.IO.Path;
using Point = Mapsui.Geometries.Point;
using Polygon = Mapsui.Geometries.Polygon;
using RasterizingLayer = TacControl.Common.Maps.RasterizingLayer;
using SymbolCache = Mapsui.Rendering.Skia.SymbolCache;

namespace TacControl
{
    public class MyMap : Mapsui.Map
    {
        private List<double> res = GetResolutions();

        static List<double> GetResolutions()
        {
            List<double> ret = new List<double>();

            for (float i = 0.01f; i < 7f; i+=0.1f)
            {
                ret.Add(Math.Exp(i));
            }

            return ret;
        }


        public new IReadOnlyList<double> Resolutions
        {
            get
            {
                return res;
            }
        }
    }

    public partial class MapView : UserControl, IDisposable
    {

        private MemoryLayer GPSTrackerLayer = new Mapsui.Layers.MemoryLayer("GPS Trackers");
        private MemoryLayer MapMarkersLayer = new Mapsui.Layers.MemoryLayer("Map Markers");
        public static BoundingBox currentBounds = new Mapsui.Geometries.BoundingBox(0, 0, 0, 0);
        public readonly MarkerVisibilityManager MarkerVisibilityManager = new MarkerVisibilityManager();

        public MapView()
        {
            InitializeComponent();
            //MouseWheel += MapControlMouseWheel;
            MapControl.MouseLeftButtonDown += MapControlOnMouseLeftButtonDown;
            MapControl.MouseLeftButtonUp += MapControlOnMouseLeftButtonUp;
            MapControl.MouseMove += MapControlOnMouseMove;
            MapControl.MouseEnter += MapControlOnMouseEnter;
            MapControl.MouseLeave += MapControlOnMouseLeave;

            //MapControl.Map = new MyMap();

            MapControl.MouseWheel += MapControlMouseWheel;
            MapControl.MouseWheelAnimation.Duration = 0;

            MapControl.Renderer = new Common.Maps.MapRenderer();

            Helper.WaitingForTerrain += OnWaitingForTerrainData;

            



            EventSystem.CenterMap += (position) => MapControl.Navigator.NavigateTo(position, 1, 500);
            //MapControl.Map.Resolutions;

            GameState.Instance.gameInfo.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(ModuleGameInfo.worldName))
                    OnNewTerrainLoaded(GameState.Instance.gameInfo.worldName);
            };


            var gridWidget = new GridWidget();
            MapControl.Map.Widgets.Add(gridWidget);


            MapControl.Map.Limiter = new ViewportLimiter();
            MapControl.Map.Limiter.ZoomLimits = new MinMax(0.01, 40);

            MapMarkersLayer.IsMapInfoLayer = true;
            MapMarkersLayer.DataSource = new MapMarkerProvider(MapMarkersLayer, currentBounds, MarkerVisibilityManager);
            MapMarkersLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(MapMarkersLayer);
            //MapMarkersLayer.DataChanged += (a, b) => MapControl.RefreshData();
            // ^ without this create/delete only updates when screen is moved

            GPSTrackerLayer.IsMapInfoLayer = true;
            GPSTrackerLayer.DataSource = new GPSTrackerProvider(GPSTrackerLayer, currentBounds);
            GPSTrackerLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(GPSTrackerLayer);
            //GPSTrackerLayer.DataChanged += (a, b) => MapControl.RefreshData();
            // ^ without this create/delete only updates when screen is moved

            LayerList.AddWidget("Grid", gridWidget);
            MarkerVisibilityList.Initialize(MarkerVisibilityManager);
            //MapControl.ZoomToBox(new Point(0, 0), new Point(8192, 8192));
            //MapControl.Navigator.ZoomTo(6);

            var markerProvider = MapMarkersLayer.DataSource as MapMarkerProvider;
            MarkerCreate.OnChannelChanged += (oldID) =>
            {
                markerProvider?.RemoveMarker(oldID);
                markerProvider?.AddMarker(MarkerCreate.MarkerRef);
            };

            MarkerCreatePopup.Closed += (x, y) =>
            {
                if (MarkerCreate.MarkerRef != null && !MarkerCreate.IsEdit)
                    markerProvider?.RemoveMarker(MarkerCreate.MarkerRef.id);
                MarkerCreate.MarkerRef = null;
            };

            if (GameState.Instance.gameInfo.worldName != null)
                OnNewTerrainLoaded(GameState.Instance.gameInfo.worldName);
        }


        private void MapView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += MapControlOnKeyDown;
        }

        private void MapControl_OnInitialized(object sender, EventArgs e)
        {
            
        }



        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Helper.WaitingForTerrain -= OnWaitingForTerrainData;
                var window = Window.GetWindow(this);
                window.KeyDown -= MapControlOnKeyDown;
            }
            _disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// New terrain was loaded in Arma, reset map and switch to new terrain
        /// </summary>
        /// <param name="terrainName"></param>
        private void OnNewTerrainLoaded(string terrainName)
        {

            MapControl.Map.Layers.Clear();
            MapControl.Map.Layers.Add(MapMarkersLayer);
            MapControl.Map.Layers.Add(GPSTrackerLayer);

            Helper.ParseLayers().ContinueWith(x => Networking.Instance.MainThreadInvoke(() => GenerateLayers(x.Result)));
        }


        private void OnWaitingForTerrainData(object thisArgs, bool isWaiting)
        {
            WaitingForTerrainDataLabel.Visibility = isWaiting ? Visibility.Visible : Visibility.Hidden;
        }
        //#TODO performance, reimplement MapControl using SKGLControl (Hardware accelerated rendering)
        //https://github.com/Mapsui/Mapsui/blob/master/Mapsui.UI.Wpf/MapControl.cs
        //https://docs.microsoft.com/en-us/dotnet/api/skiasharp.views.desktop.skglcontrol?view=skiasharp-views-1.68.2

        //Reimplement renderer to see why stuff doesn't work?
        // https://github.com/Mapsui/Mapsui/blob/master/Mapsui.UI.Wpf/MapControl.cs#L117
        // https://github.com/Mapsui/Mapsui/blob/7ac1e6eb1e04456ba92257cd5b350e4a9ed6bf16/Mapsui.Rendering.Skia/MapRenderer.cs#L110
        // Check VisibleFeatureIterator?

        private double resolution = 6;

        private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MapControl.Map.ZoomLock) return;
            if (!MapControl.Viewport.HasSize) return;

            var _currentMousePosition = e.GetPosition(this).ToMapsui();

            resolution = Math.Exp((e.Delta/120) * -0.1f) * resolution;
            // Limit target resolution before animation to avoid an animation that is stuck on the max resolution, which would cause a needless delay
            resolution = MapControl.Map.Limiter.LimitResolution(resolution, MapControl.Viewport.Width, MapControl.Viewport.Height, MapControl.Map.Resolutions, MapControl.Map.Envelope);
            MapControl.Navigator.ZoomTo(resolution, _currentMousePosition, MapControl.MouseWheelAnimation.Duration, MapControl.MouseWheelAnimation.Easing);
        }





        private int markNum = 0;

        private void GenerateLayers(List<Helper.SvgLayer> layers)
        {
            List<(BaseLayer, Task)> layerLoadTasks = new List<(BaseLayer, Task)>();
            int terrainWidth = 0;
            int index = 0;
            foreach (var svgLayer in layers)
            {
                if (svgLayer.content.GetSize() > 5e7) //> 50MB
                {
                   
                    //#TODO tell the user, this layer is too big and is skipped for safety. TacControl would use TONS of ram, very bad, usually an issue with Forest layer
                
                    continue;
                }



                var layer = new MemoryLayer(svgLayer.name);
                var renderLayer = new RasterizingLayer(layer, 100, 1D, MapControl.Renderer);

                if (svgLayer.name == "forests" || svgLayer.name == "countLines" || svgLayer.name == "rocks" ||
                    svgLayer.name == "grid")
                {
                    renderLayer.Enabled = false;
                }

                terrainWidth = svgLayer.width;

                currentBounds = new Mapsui.Geometries.BoundingBox(0, 0, terrainWidth, terrainWidth);

                var features = new Features();
                var feature = new Feature {Geometry = new BoundBox(currentBounds), ["Label"] = svgLayer.name};

             
                if (renderLayer.Enabled)
                {
                    var x = new SvgStyle { image = new Svg.Skia.SKSvg() };
                    renderLayer.Enabled = false;
                    layerLoadTasks.Add((renderLayer,
                        Task.Run(() =>
                        {
                            using (var stream = svgLayer.content.GetStream())
                            {
                                //var file = File.Create($"P:/{layer.Name}.svg");
                                //stream.CopyTo(file);
                                //file.Dispose();

                                x.image.Load(stream);
                            }
                        })));

                    feature.Styles.Add(x);
                }
                else
                {
                    var x = new SvgStyleLazy { data = svgLayer.content };
                    x.DoLoad = () =>
                    {
                        using (var stream = svgLayer.content.GetStream())
                        {
                            var image = new Svg.Skia.SKSvg();
                            image.Load(stream);
                            x.image = image;
                        }

                        layer.DataHasChanged();
                    };

                    feature.Styles.Add(x);
                }

                features.Add(feature);


                layer.DataSource = new MemoryProvider(features);
                layer.MinVisible = 0;
                layer.MaxVisible = double.MaxValue;
                MapControl.Map.Layers.Insert(index++, renderLayer);
            }

            MapControl.Map.Limiter.PanLimits = new Mapsui.Geometries.BoundingBox(0, 0, terrainWidth, terrainWidth);


            Task.WhenAll(layerLoadTasks.Select(x => x.Item2).ToArray()).ContinueWith(x =>
            {
                Networking.Instance.MainThreadInvoke(() =>
                {
                    foreach (var (memoryLayer, item2) in layerLoadTasks)
                    {
                        memoryLayer.Enabled = true;

                        memoryLayer.DataHasChanged();
                    }

                    LayerList.Initialize(MapControl.Map.Layers);

                    MapControl.Navigator.NavigateTo(new BoundingBox(new Point(0, 0), new Point(terrainWidth, terrainWidth)));
                    MapControl.RefreshGraphics();
                });
            });

            foreach (var markerMarkerType in GameState.Instance.marker.markerTypes)
            {
                markNum++;
                MarkerCache.Instance.GetImage(markerMarkerType.Value, (MarkerColor)null).ContinueWith((x) =>
                {
                    markerMarkerType.Value.iconImage = x.Result; //#TODO use this for markerCache caching in general, store cached images in there
                    markNum--;
                });
            }
        }

        private void MapControlOnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            GPSEditPopup.IsOpen = false;
            MarkerCreatePopup.IsOpen = false;
            if (args.ClickCount > 1)
            {

                var mapsPos = args.GetPosition(MapControl).ToMapsui();
                var info = MapControl.GetMapInfo(mapsPos, 12);

                if (info.Feature is GPSTrackerFeature gpsTrackerFeature)
                {

                    GPSEdit.Tracker = gpsTrackerFeature.Tracker;
                    GPSEditPopup.Placement = PlacementMode.Mouse;
                    GPSEditPopup.StaysOpen = true;
                    GPSEditPopup.AllowsTransparency = true;
                    GPSEditPopup.IsOpen = true;
                }
                else if(info.Feature is MarkerFeature editMarker && editMarker.marker.polyline.Count == 0) // cannot edit polyline marker
                {

                    MarkerCreate.Init(); // #TODO add this to init once
                    MarkerCreate.MarkerRef = editMarker.marker;
                    MarkerCreate.IsEdit = true;


                    // #TODO add this to init once
                    MarkerCreatePopup.Placement = PlacementMode.Mouse;
                    MarkerCreatePopup.HorizontalOffset = 5;
                    MarkerCreatePopup.StaysOpen = true;
                    MarkerCreatePopup.AllowsTransparency = true;

                    MarkerCreatePopup.IsOpen = true;
                }
                else
                {
                    MarkerCreate.Init();
                    MarkerCreate.MarkerRef = new ActiveMarker
                    {
                        id = GameState.Instance.marker.GenerateMarkerName(MarkerChannel.Global),
                        channel = 0,
                        color = "Default",
                        type = "hd_dot",
                        shape = "ICON",
                        text = "",
                        size = "1,1",
                        alpha = 1,
                        dir = 0,
                        brush= "Solid"
                    };
                    MarkerCreate.IsEdit = false;

                    MarkerCreate.MarkerRef.pos.Clear();
                    MarkerCreate.MarkerRef.pos.Add((float)info.WorldPosition.X);
                    MarkerCreate.MarkerRef.pos.Add((float)info.WorldPosition.Y);
                   

                    MarkerCreatePopup.Placement = PlacementMode.Mouse;
                    MarkerCreatePopup.HorizontalOffset = 5;
                    MarkerCreatePopup.StaysOpen = true;
                    MarkerCreatePopup.AllowsTransparency = true;

                    var markerProvider = MapMarkersLayer.DataSource as MapMarkerProvider;
                    markerProvider?.AddMarker(MarkerCreate.MarkerRef);
                    MarkerCreatePopup.IsOpen = true;
                }

                args.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                var mapsPos = args.GetPosition(MapControl).ToMapsui();
                var info = MapControl.GetMapInfo(mapsPos, 12);


                polyDraw = new ActiveMarker
                {
                    id = GameState.Instance.marker.GenerateMarkerName(MarkerChannel.Global),
                    channel = 0,
                    color = "ColorBlack",
                    type = "hd_dot",
                    shape = "POLYLINE",
                    text = "",
                    size = "1,1",
                    alpha = 1,
                    dir = 0,
                    brush = "Solid"
                };


                polyDraw.pos.Clear();
                polyDraw.pos.Add((float)info.WorldPosition.X);
                polyDraw.pos.Add((float)info.WorldPosition.Y);

                polyDraw.polyline.Add(new float[] { (float)info.WorldPosition.X, (float)info.WorldPosition.Y });

                var markerProvider = MapMarkersLayer.DataSource as MapMarkerProvider;
                markerProvider?.AddMarker(polyDraw, false);
                MapMarkersLayer.DataHasChanged();
                //MapControl.Refresh(ChangeType.Discrete);
                args.Handled = true;
            }
        }

        private ActiveMarker polyDraw = null;
        private bool _disposed;

        private void MapControlOnMouseLeftButtonUp(object sender, MouseButtonEventArgs args)
        {
            if (polyDraw == null) return;
            //MapMarkersLayer.Delayer.MillisecondsToWait = 500;
            if (polyDraw.polyline.Count > 1)
                GameState.Instance.marker.CreateMarker(polyDraw);
            var markerProvider = MapMarkersLayer.DataSource as MapMarkerProvider;
            markerProvider?.RemoveMarker(polyDraw.id);
            polyDraw = null;
        }

        private void MapControlOnMouseMove(object sender, MouseEventArgs args)
        {
            var mapsPos = args.GetPosition(MapControl);
            var info = MapControl.GetMapInfo(mapsPos.ToMapsui(), 12);

            MapCursor.RenderTransform = new TranslateTransform(mapsPos.X - MapCursor.ActualWidth/2, mapsPos.Y - MapCursor.ActualHeight / 2);
            MapCursor.UnderCursor = info;

            if (polyDraw == null) return;

            var lastPos = polyDraw.polyline.Last();
            
            
            if (new Point(lastPos[0], lastPos[1]).Distance(new Point(info.WorldPosition.X, info.WorldPosition.Y)) > 5)
            {
                polyDraw.polyline.Add(new float[] { (float)info.WorldPosition.X, (float)info.WorldPosition.Y });
                MapMarkersLayer.DataHasChanged();
            }
            

            

        }


        private void MapControlOnMouseLeave(object sender, MouseEventArgs e)
        {
            MapCursor.Visibility = Visibility.Hidden;
            Mouse.OverrideCursor = null;
        }

        private void MapControlOnMouseEnter(object sender, MouseEventArgs e)
        {
            MapCursor.Visibility = Visibility.Visible;
            Mouse.OverrideCursor = Cursors.None;
        }


        private void MapControlOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && MapCursor.Visibility == Visibility.Visible && MapCursor.UnderCursor.Feature is MarkerFeature markerToDelete)
            {
                // request marker delete
                GameState.Instance.marker.DeleteMarker(markerToDelete.marker);

            }
        }

    }
}
