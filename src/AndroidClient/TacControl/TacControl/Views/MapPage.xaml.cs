using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Mapsui;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.UI.Forms;
using SkiaSharp;
using TacControl.Common;
using TacControl.Common.Modules;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using TacControl.Models;
using TacControl.Views;
using TacControl.ViewModels;
using Bitmap = Mapsui.Styles.Bitmap;
using Color = Svg.Picture.Color;
using Image = Xamarin.Forms.Image;
using Point = Xamarin.Forms.Point;
using TacControl.Common.Maps;

namespace TacControl.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapPage : ContentPage
    {
        public Func<MapControl, MapClickedEventArgs, bool> Clicker { get; set; }

        private Layer GPSTrackerLayer = new Mapsui.Layers.Layer("GPS Trackers");
        private Layer MapMarkersLayer = new Mapsui.Layers.Layer("Map Markers");
        public static BoundingBox currentBounds = new Mapsui.Geometries.BoundingBox(0, 0, 0, 0);



        public MapPage()
        {
            InitializeComponent();

            MapControl.RotationLock = true;

            MapControl.TouchStarted += MapControlOnMouseLeftButtonDown;
            MapControl.MapClicked += MapControlOnClicked;


            MapControl.Renderer.StyleRenderers[typeof(SvgStyle)] = new SvgStyleRenderer();
            MapControl.Renderer.StyleRenderers[typeof(TiledBitmapStyle)] = new TiledBitmapRenderer();
            MapControl.Renderer.StyleRenderers[typeof(VelocityIndicatorStyle)] = new VelocityIndicatorRenderer();
            MapControl.Renderer.StyleRenderers[typeof(PolylineMarkerStyle)] = new PolylineMarkerRenderer();
            MapControl.Renderer.StyleRenderers[typeof(MarkerIconStyle)] = new MarkerIconRenderer();

            MapControl_OnLoaded();
        }

        protected override void OnAppearing()
        {
            MapControl.IsVisible = true;
            MapControl.Refresh();
        }

        private void MapControl_Info(object sender, Mapsui.UI.MapInfoEventArgs e)
        {
            if (e?.MapInfo?.Feature != null)
            {
                foreach (var style in e.MapInfo.Feature.Styles)
                {
                    if (style is CalloutStyle)
                    {
                        style.Enabled = !style.Enabled;
                        e.Handled = true;
                    }
                }

                MapControl.Refresh();
            }
        }

        private void MapControl_OnLoaded()
        {
            Helper.ParseLayers().ContinueWith(x => Networking.Instance.MainThreadInvoke(() => GenerateLayers(x.Result)));
        }

        private void GenerateLayers(List<Helper.SvgLayer> layers)
        {
            List<Task> layerLoadTasks = new List<Task>();
            int terrainWidth = 0;
            foreach (var svgLayer in layers)
            {
                var layer = new Mapsui.Layers.MemoryLayer(svgLayer.name);

                if (
                    svgLayer.name == "forests" ||
                    svgLayer.name == "countLines" ||
                    svgLayer.name == "rocks" ||
                    svgLayer.name == "grid"
                    )
                {
                    layer.Enabled = false;
                    if (System.Environment.OSVersion.Platform == PlatformID.Unix) //Android
                        continue;
                }

                var head = svgLayer.content.Substring(0, svgLayer.content.IndexOf('\n'));
                var widthSub = head.Substring(head.IndexOf("width"));
                var width = widthSub.Substring(7, widthSub.IndexOf('"', 7) - 7);
                terrainWidth = int.Parse(width);

                currentBounds = new Mapsui.Geometries.BoundingBox(0, 0, terrainWidth, terrainWidth);

                var features = new Features();
                var feature = new Feature {Geometry = new BoundBox(currentBounds), ["Label"] = svgLayer.name};

                var x = new SvgStyle {image = new Svg.Skia.SKSvg()};

                layerLoadTasks.Add(
                    Task.Run(() =>
                    {
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgLayer.content)))
                        {
                            x.image.Load(stream);
                        }
                    }));

                feature.Styles.Add(x);
                features.Add(feature);

                //
                layer.DataSource = new MemoryProvider(features);
                MapControl.Map.Layers.Add(layer);
            }

            Task.WaitAll(layerLoadTasks.ToArray());
            //var layer = new Mapsui.Layers.ImageLayer("Base");
            //layer.DataSource = CreateMemoryProviderWithDiverseSymbols();
            //MapControl.Map.Layers.Add(layer);
            
            MapControl.Map.Limiter = new ViewportLimiter();
            MapControl.Map.Limiter.PanLimits = new Mapsui.Geometries.BoundingBox(0, 0, terrainWidth, terrainWidth);
            MapControl.Map.Limiter.ZoomLimits = new MinMax(0.01, 7);

            GPSTrackerLayer.IsMapInfoLayer = true;
            GPSTrackerLayer.DataSource = new GPSTrackerProvider(GPSTrackerLayer, currentBounds);
            GPSTrackerLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(GPSTrackerLayer);
            GPSTrackerLayer.DataChanged += (a,b) => MapControl.RefreshData();
            // ^ without this create/delete only updates when screen is moved

            MapMarkersLayer.IsMapInfoLayer = true;
            MapMarkersLayer.DataSource = new MapMarkerProvider(MapMarkersLayer, currentBounds);
            MapMarkersLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(MapMarkersLayer);
            MapMarkersLayer.DataChanged += (a, b) => MapControl.RefreshData();
            // ^ without this create/delete only updates when screen is moved

            //LayerList.Initialize(MapControl.Map.Layers);
            //MapControl.ZoomToBox(new Point(0, 0), new Point(8192, 8192));
            MapControl.Navigator.ZoomTo(1, 0);
        }


        private void MapControlOnMouseLeftButtonDown(object sender, TouchedEventArgs e)
        {
            var info = MapControl.GetMapInfo(e.ScreenPoints.First(), 12);

            //GPSEditPopup.IsOpen = false;
            if (info.Feature is GPSTrackerFeature gpsTrackerFeature)
            {

                //GPSEdit.Tracker = gpsTrackerFeature.Tracker;
                //GPSEditPopup.Placement = PlacementMode.Mouse;
                //GPSEditPopup.StaysOpen = false;
                //GPSEditPopup.AllowsTransparency = true;
                //GPSEditPopup.IsOpen = true;
                //e.Handled = true;
            }
        }


        private void MapControlOnClicked(object sender, MapClickedEventArgs e)
        {
            //Doesn't work.
            //var info = MapControl.GetMapInfo(e.Point.ToMapsui(), 12);
        }



    }
}
