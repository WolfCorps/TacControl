using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using HarfBuzzSharp;
using HarmonyLib;
using Mapsui;
using Mapsui.Animations;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Limiting;
using Mapsui.Logging;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using Mapsui.UI.Wpf.Extensions;
using Mapsui.Utilities;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using Mapsui.Widgets.ScaleBar;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using TacControl.Annotations;
using TacControl.Common;
using TacControl.Common.Config.Section;
using TacControl.Common.Maps;
using TacControl.Common.Modules;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using static HarmonyLib.AccessTools;
using static TacControl.Common.Modules.ModuleMarker;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using LineStringRenderer = Mapsui.Rendering.Skia.LineStringRenderer;
using Math = System.Math;
using Path = System.IO.Path;
using Point = Mapsui.MPoint;
using Polygon = NetTopologySuite.Geometries.Polygon;

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


    [HarmonyPatch(typeof(MapControl), "IsHovering")] // https://github.com/Mapsui/Mapsui/blob/3df38358202f42334cff68ee87cb283dcb1db02b/Mapsui.UI.Wpf/MapControl.cs#L236
    class Patch01
    {
        static bool Prefix(MouseEventArgs e, MapControl __instance, ref bool __result)
        {
            __result = e.RightButton != MouseButtonState.Pressed; // Check right instead of left
            return false; // Don't run original
        }
    }

    public partial class MapView : UserControl, IDisposable, INotifyPropertyChanged
    {

        private Mapsui.Layers.Layer GPSTrackerLayer = new Mapsui.Layers.Layer("GPS Trackers");
        private Mapsui.Layers.Layer MapMarkersLayer = new Mapsui.Layers.Layer("Map Markers");
        private List<ILayer> MapInfoLayers = new List<ILayer>();
        public static MRect currentBounds = new Mapsui.MRect(0, 0, 0, 0);
        public readonly MarkerVisibilityManager MarkerVisibilityManager = new MarkerVisibilityManager();

        public MapView()
        {
            var harmony = new Harmony("com.dedmen.taccontrol");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);


            InitializeComponent();

            // HACK to swap the moving map mouse button from left to right button

            {

                FieldInfo leftDownType = typeof(System.Windows.UIElement).GetEventField("MouseLeftButtonDownEvent");
                RoutedEvent leftDownRE = (RoutedEvent)leftDownType.GetValue(MapControl);
                FieldInfo leftUpType = typeof(System.Windows.UIElement).GetEventField("MouseLeftButtonUpEvent");
                RoutedEvent leftUpRE = (RoutedEvent)leftUpType.GetValue(MapControl);

                FieldInfo rightDownType = typeof(System.Windows.UIElement).GetEventField("MouseRightButtonDownEvent");
                RoutedEvent rightDownRE = (RoutedEvent)rightDownType.GetValue(MapControl);
                FieldInfo rightUpType = typeof(System.Windows.UIElement).GetEventField("MouseRightButtonUpEvent");
                RoutedEvent rightUpRE = (RoutedEvent)rightUpType.GetValue(MapControl);

                var xp = MapControl.GetType().GetProperties();



                PropertyInfo EventHandlersStoreType = MapControl.GetType().GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
                object EventHandlersStore = EventHandlersStoreType.GetValue(MapControl, null);
                Type storeType = EventHandlersStore.GetType();
                MethodInfo GetEventHandlers = storeType.GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public);

                var leftDownEvents = (System.Windows.RoutedEventHandlerInfo[])GetEventHandlers.Invoke(EventHandlersStore, new object[] { UIElement.MouseLeftButtonDownEvent as RoutedEvent });
                var leftUpEvents = (System.Windows.RoutedEventHandlerInfo[])GetEventHandlers.Invoke(EventHandlersStore, new object[] { UIElement.MouseLeftButtonUpEvent as RoutedEvent });

                // Swap them
                //MapControl.RemoveHandler(leftDownRE, leftDownEvents.First().Handler);
                //MapControl.RemoveHandler(leftUpRE, leftUpEvents.First().Handler);

                //MapControl.AddHandler(rightDownRE, leftDownEvents.First().Handler);
                //MapControl.AddHandler(rightUpRE, leftUpEvents.First().Handler);
                //
                //
                //
                //MapControl.MouseRightButtonDown += (x,y) =>
                //{
                //    MethodInfo doThing = MapControl.GetType().GetMethod("MapControlMouseLeftButtonDown", BindingFlags.Instance | BindingFlags.NonPublic);
                //    doThing.Invoke(MapControl, new object[] { x, y });
                //};
                //MapControl.MouseLeftButtonUp += (x, y) =>
                //{
                //    MethodInfo doThing = MapControl.GetType().GetMethod("MapControlMouseLeftButtonUp", BindingFlags.Instance | BindingFlags.NonPublic);
                //    doThing.Invoke(MapControl, new object[] { x, y });
                //};





            }


            // The PerformanceWidget is created as part of the map.
            var performanceWidget = MapControl.Map.Widgets.OfType<PerformanceWidget>().First();
            // The default is ActiveMode.OnlyInDebugMode, which is usually the best option.
            performanceWidget.Performance.IsActive = ActiveMode.Yes;
            performanceWidget.BackColor = Color.FromRgba(255, 255, 32, 32);
            performanceWidget.Opacity = 1;





            //MouseWheel += MapControlMouseWheel;
            MapControl.MouseLeftButtonDown += MapControlOnMouseLeftButtonDown;
            MapControl.MouseLeftButtonUp += MapControlOnMouseLeftButtonUp;
            MapControl.MouseMove += MapControlOnMouseMove;
            MapControl.MouseEnter += MapControlOnMouseEnter;
            MapControl.MouseLeave += MapControlOnMouseLeave;

            Mapsui.Experimental.Rendering.Skia.MapRenderer.RegisterStyleRenderer(typeof(SvgStyle), new SvgStyleRenderer());
            Mapsui.Experimental.Rendering.Skia.MapRenderer.RegisterStyleRenderer(typeof(SvgStyleLazy), new SvgStyleRenderer());
            Mapsui.Experimental.Rendering.Skia.MapRenderer.RegisterStyleRenderer(typeof(TiledBitmapStyle), new TiledBitmapRenderer());
            Mapsui.Experimental.Rendering.Skia.MapRenderer.RegisterStyleRenderer(typeof(PolylineMarkerStyle), new PolylineMarkerRenderer());
            Mapsui.Experimental.Rendering.Skia.MapRenderer.RegisterStyleRenderer(typeof(MarkerIconStyle), new MarkerIconRenderer());
            Mapsui.Experimental.Rendering.Skia.MapRenderer.RegisterWidgetRenderer(typeof(GridWidget), new GridWidgetRenderer());

            Mapsui.Logging.Logger.LogDelegate += (level, message, ex) =>
            {
                Console.WriteLine($"{message} {ex?.Message}"); // <-- Put a break point here, most UI platforms do not show the console logging.
                                                               // todo: Forward to your own logger
            };
            //MapControl.Map = new MyMap();


            // Overwrite the default renderer for everything. This is important for RasterizingLayer, which uses default
            Mapsui.Rendering.DefaultRendererFactory.Create = () => {

                var rndr = new Mapsui.Experimental.Rendering.Skia.MapRenderer();
                return rndr; //#TODO SKGLWpfControl. This renderer should actull
            };
            //MapControl.SetMapRenderer(new Common.Maps.MapRenderer());
            MapControl.SetMapRenderer(new Mapsui.Experimental.Rendering.Skia.MapRenderer());
            MapControl.MouseWheel += MapControlMouseWheel;
            //MapControl.MouseWheelAnimation.Duration = 0;

            MapControl.UseFling = false;



            Helper.WaitingForTerrain += OnWaitingForTerrainData;

            



            EventSystem.CenterMap += (MPoint position) => MapControl.Map.Navigator.CenterOn(position.X, position.Y, 1);
            //MapControl.Map.Resolutions;

            GameState.Instance.gameInfo.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(ModuleGameInfo.worldName))
                    OnNewTerrainLoaded(GameState.Instance.gameInfo.worldName);
            };


            var gridWidget = new GridWidget();
            MapControl.Map.Widgets.Add(gridWidget);
            MapControl.Map.Navigator.OverrideZoomBounds = new MMinMax(0.01, 40);

            MapMarkersLayer.DataSource = new MapMarkerProvider(MapMarkersLayer, currentBounds, MarkerVisibilityManager);
            MapMarkersLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(MapMarkersLayer);
            MapInfoLayers.Add(MapMarkersLayer);
            MapMarkersLayer.DataChanged += (a, b) => MapControl.RefreshData();
            // ^ without this create/delete only updates when screen is moved

            GPSTrackerLayer.DataSource = new GPSTrackerProvider(GPSTrackerLayer, currentBounds);
            GPSTrackerLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(GPSTrackerLayer);
            MapInfoLayers.Add(GPSTrackerLayer);
            GPSTrackerLayer.DataChanged += (a, b) => MapControl.RefreshData();
            // ^ without this create/delete only updates when screen is moved

            LayerList.AddWidget("Grid", gridWidget);
            MarkerVisibilityList.Initialize(MarkerVisibilityManager);

            var markerProvider = MapMarkersLayer.DataSource as MapMarkerProvider;

            MarkerVisibilityList.OnCurrentChannelSelected += (x) =>
            {
                DefaultMarkerChannel = x;
                markerProvider.SetForegroundChannel(DefaultMarkerChannel);
            };
            markerProvider.SetForegroundChannel(DefaultMarkerChannel);

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

        public string DefaultMarkerColor { get; set; }
        public MarkerChannel DefaultMarkerChannel { get; set; } = MarkerChannel.Global; // Default also has to be set in MarkerVisibilityList
        //#TODO make config option




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
            Console.WriteLine($"MapView OnNewTerrain {terrainName}, Now loading terrain data...");
            MapControl.Map.Layers.Clear();
            MapControl.Map.Layers.Add(MapMarkersLayer);
            MapControl.Map.Layers.Add(GPSTrackerLayer);

            Helper.ParseLayers().ContinueWith(x => Common.Networking.Instance.MainThreadInvoke(() => GenerateLayers(x.Result)));
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
            var mouseWheelDelta = e.Delta;
            var _currentMousePosition = e.GetPosition(this).ToScreenPosition();
            MapControl.Map.Navigator.MouseWheelZoom(mouseWheelDelta, _currentMousePosition);
        }





        private int markNum = 0;

        private void GenerateLayers(List<Helper.SvgLayer> layers)
        {
            Console.WriteLine($"MapView Layerdata arrived, creating layers...");
            List<(ILayer, Task)> layerLoadTasks = new List<(ILayer, Task)>();
            int terrainWidth = 0;
            int index = 0;
            foreach (var svgLayer in layers)
            {
                if (svgLayer.content.GetSize() > 5e7) //> 50MB
                {
                    Console.WriteLine($"MapView !! Layer is too big, skipping layer {svgLayer.name}");
                    //#TODO tell the user, this layer is too big and is skipped for safety. TacControl would use TONS of ram, very bad, usually an issue with Forest layer

                    continue;
                }



                var layer = new Mapsui.Layers.Layer(svgLayer.name);
                var renderLayer = new RasterizingLayer(layer, 100); //#TODO RasterizingTileLayer?

                if (svgLayer.name == "forests" || svgLayer.name == "countLines" || svgLayer.name == "rocks" ||
                    svgLayer.name == "grid")
                {
                    renderLayer.Enabled = false;
                }

                terrainWidth = svgLayer.width;

                currentBounds = new Mapsui.MRect(0, 0, terrainWidth, terrainWidth);

                var features = new List<IFeature>();
                var feature = new GeometryFeature() {Geometry = currentBounds.ToPolygon(), ["Label"] = svgLayer.name};

             
                if (renderLayer.Enabled)
                {
                    var x = new SvgStyle { image = new Svg.Skia.SKSvg() };
                    x.dbgSrc = svgLayer.name;
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
                layer.Opacity = 0; // Opacity just turns our "transparent" from the SVG, into white..
                MapControl.Map.Layers.Insert(index++, renderLayer);

                //MapControl.Map.Layers.Insert(index++, layer);
                layer.DataHasChanged();
                renderLayer.ClearCache();
                renderLayer.DataHasChanged();
            }

            MapControl.Map.Navigator.OverridePanBounds = new Mapsui.MRect(0, 0, terrainWidth, terrainWidth);


            Task.WhenAll(layerLoadTasks.Select(x => x.Item2).ToArray()).ContinueWith(x =>
            {
                Common.Networking.Instance.MainThreadInvoke(() =>
                {
                    foreach (var (memoryLayer, item2) in layerLoadTasks)
                    {
                        memoryLayer.Enabled = true;

                        memoryLayer.DataHasChanged();
                    }

                    LayerList.Initialize(MapControl.Map.Layers);

                    MapControl.Map.Navigator.ZoomToBox(new MRect(0, 0, terrainWidth, terrainWidth));
                    MapControl.RefreshGraphics();
                });
            });

            Console.WriteLine($"MapView Layers loaded, preloading marker images from Arma...");

            foreach (var markerMarkerType in GameState.Instance.marker.markerTypes)
            {
                markNum++;
                MarkerCache.Instance.GetImage(markerMarkerType.Value, (MarkerColor)null).ContinueWith((x) =>
                {
                    markerMarkerType.Value.iconImage = x.Result; //#TODO use this for markerCache caching in general, store cached images in there
                    markNum--;
                    //if (markNum == 3) // && !ImageDirectory.Instance.HasPendingRequests()
                    //{
                    //    // all markers loaded
                    //    ImageDirectory.Instance.ExportImagesToZip("P:/markers.zip");
                    //}
                });
            }

            // hacky, we might not have markerColors available before this method.
            cmbColors.ItemsSource = GameState.Instance.marker.markerColors.Values;
            DefaultMarkerColor = "Default";

            cmbColors.DropDownClosed += (x, y) =>
            {
                var conv = new MarkerColorStringConverter();
                DefaultMarkerColor = conv.Convert(cmbColors.SelectedItem as MarkerColor, typeof(string), null, null) as string;
            };
        }


        // marker being dragged&moved via Alt+LMB
        private ActiveMarker movingMarker = null;


        private void MapControlOnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            GPSEditPopup.IsOpen = false;
            MarkerCreatePopup.IsOpen = false;
            if (args.ClickCount > 1)
            {

                var mapsPos = args.GetPosition(MapControl).ToScreenPosition();
                var info = MapControl.GetMapInfo(mapsPos, MapInfoLayers);

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
                        color = DefaultMarkerColor,
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
                var mapsPos = args.GetPosition(MapControl).ToScreenPosition();
                var info = MapControl.GetMapInfo(mapsPos, MapInfoLayers);

                polyDraw = new ActiveMarker
                {
                    id = GameState.Instance.marker.GenerateMarkerName(MarkerChannel.Global),
                    channel = (int)DefaultMarkerChannel,
                    color = DefaultMarkerColor,
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
            else if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                var mapsPos = args.GetPosition(MapControl).ToScreenPosition();
                var info = MapControl.GetMapInfo(mapsPos, MapInfoLayers);

                if (info.Feature is MarkerFeature marker)
                {
                    // cannot move global editor markers
                    if (marker.marker.channel != -1)
                        movingMarker = marker.marker;
                }

                args.Handled = true;
            }
        }

        private ActiveMarker polyDraw = null;
        private bool _disposed;

        private void MapControlOnMouseLeftButtonUp(object sender, MouseButtonEventArgs args)
        {
            if (polyDraw != null)
            {
                //MapMarkersLayer.Delayer.MillisecondsToWait = 500;
                if (polyDraw.polyline.Count > 1)
                    GameState.Instance.marker.CreateMarker(polyDraw);
                var markerProvider = MapMarkersLayer.DataSource as MapMarkerProvider;
                markerProvider?.RemoveMarker(polyDraw.id);
                polyDraw = null;
            }

            if (movingMarker != null) {
                GameState.Instance.marker.EditMarker(movingMarker);
                movingMarker = null;
            }
        }

        private void MapControlOnMouseMove(object sender, MouseEventArgs args)
        {
            var mapsPos = args.GetPosition(MapControl);
            var info = MapControl.GetMapInfo(mapsPos.ToScreenPosition(), MapInfoLayers); //#TODO , 12 precision

            MapCursor.RenderTransform = new TranslateTransform(mapsPos.X - MapCursor.ActualWidth/2, mapsPos.Y - MapCursor.ActualHeight / 2);
            MapCursor.UnderCursor = info;

            if (polyDraw != null)
            {
                var lastPos = polyDraw.polyline.Last();


                if (new Point(lastPos[0], lastPos[1]).Distance(new Point(info.WorldPosition.X, info.WorldPosition.Y)) > 5)
                {
                    polyDraw.polyline.Add(new float[] { (float)info.WorldPosition.X, (float)info.WorldPosition.Y });
                    MapMarkersLayer.DataHasChanged();
                }
            }

            
            if (movingMarker != null)
            {
                movingMarker.SetPos((float)info.WorldPosition.X, (float)info.WorldPosition.Y);
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
