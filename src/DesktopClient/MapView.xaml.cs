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
using TacControl.Common;
using TacControl.Common.Maps;
using TacControl.Common.Modules;
using static TacControl.Common.Modules.ModuleMarker;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Geometry = Mapsui.Geometries.Geometry;
using LineStringRenderer = Mapsui.Rendering.Skia.LineStringRenderer;
using MultiLineStringRenderer = Mapsui.Rendering.Skia.MultiLineStringRenderer;
using MultiPolygonRenderer = Mapsui.Rendering.Skia.MultiPolygonRenderer;
using Path = System.IO.Path;
using Point = Mapsui.Geometries.Point;
using Polygon = Mapsui.Geometries.Polygon;
using SymbolCache = Mapsui.Rendering.Skia.SymbolCache;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for MapView.xaml
    /// </summary>
    public partial class MapView : UserControl
    {

        private Layer GPSTrackerLayer = new Mapsui.Layers.Layer("GPS Trackers");
        private Layer MapMarkersLayer = new Mapsui.Layers.Layer("Map Markers");
        public static BoundingBox currentBounds = new Mapsui.Geometries.BoundingBox(0, 0, 0, 0);

        public MapView()
        {
            InitializeComponent();
            //MouseWheel += MapControlMouseWheel;
            MapControl.MouseLeftButtonDown += MapControlOnMouseLeftButtonDown;
        }

        private void MapControl_OnInitialized(object sender, EventArgs e)
        {


            
        }



        private static readonly XNamespace xlink = "http://www.w3.org/1999/xlink";
        private static readonly XNamespace svg = "http://www.w3.org/2000/svg";
        private readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();


        public struct SvgLayer
        {
            public string name;
            public string content;
        }

        public List<SvgLayer> ParseLayers()
        {
            List<SvgLayer> ret = new List<SvgLayer>();
            var mapPath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "maps"), $"{GameState.Instance.gameInfo.worldName}.svg");

            bool isCompressed = !File.Exists(mapPath) && File.Exists(mapPath + "z"); //gzip
            if (isCompressed) mapPath = mapPath + "z";

            //var mapPath = @$"J:/dev/Arma/SVG/{GameState.Instance.gameInfo.worldName}.svg";
            using (var fileStream = File.OpenRead(mapPath))
            {
                GZipStream decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress);

                Stream stream = isCompressed ? decompressionStream : (Stream)fileStream;

                using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext()))
                {
                    var xdoc = XDocument.Load(reader);
                    var svg = xdoc.Root;
                    var ns = svg.Name.Namespace;

                    var mainAttributes = svg.Attributes();
                    var defs = svg.Element("defs");
                    List<XElement> layers = new List<XElement>();
                    List<XElement> rootElements = new List<XElement>();

                    foreach (var xElement in svg.Elements())
                    {
                        if (xElement.Name == ns + "g")
                        {
                            layers.Add(xElement);
                        }
                        else
                            rootElements.Add(xElement);
                    }

                    XDocument bareDoc = new XDocument(xdoc);
                    List<XElement> toRemove = new List<XElement>();
                    foreach (var xElement in bareDoc.Root.Elements())
                    {
                        if (xElement.Name == ns + "g")
                            toRemove.Add(xElement);
                    }
                    toRemove.ForEach(x => x.Remove());

                    var test = bareDoc.ToString();
                    foreach (var xElement in layers)
                    {
                        XDocument newDoc = new XDocument(bareDoc);
                        newDoc.Root.Add(xElement);
                        SvgLayer x;
                        x.content = newDoc.ToString();
                        x.name = xElement.Attribute("id").Value;
                        ret.Add(x);
                    }



                }

                decompressionStream.Dispose();
            }

            return ret;
        }

        private static XmlParserContext CreateSvgXmlContext()
        {
            var table = new NameTable();
            var manager = new XmlNamespaceManager(table);
            manager.AddNamespace(string.Empty, svg.NamespaceName);
            manager.AddNamespace("xlink", xlink.NamespaceName);
            return new XmlParserContext(null, manager, null, XmlSpace.None);
        }

        private void MapControl_OnLoaded(object sender, RoutedEventArgs e)
        {


            //MapControl.Map.Layers.Clear();
            //
            //
            //MapControl.Map = new Map();
            List<Task> layerLoadTasks = new List<Task>();

            var layers = ParseLayers();
            int terrainWidth = 0;
            foreach (var svgLayer in layers)
            {

                var layer = new Mapsui.Layers.ImageLayer(svgLayer.name);


                if (svgLayer.name == "forests" || svgLayer.name == "countLines" || svgLayer.name == "rocks" || svgLayer.name == "grid")
                {
                    layer.Enabled = false;
                }
                
                var head = svgLayer.content.Substring(0, svgLayer.content.IndexOf('\n'));
                var widthSub = head.Substring(head.IndexOf("width"));
                var width = widthSub.Substring(7, widthSub.IndexOf('"', 7) - 7);
                terrainWidth = int.Parse(width);

                currentBounds = new Mapsui.Geometries.BoundingBox(0, 0, terrainWidth, terrainWidth);

                var features = new Features();
                var feature = new Feature { Geometry = new BoundBox(currentBounds), ["Label"] = svgLayer.name };

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


            var svgRender = new SvgStyleRenderer();
            MapControl.Renderer.StyleRenderers[typeof(SvgStyle)] = svgRender;
            var tbitmRender = new TiledBitmapRenderer();
            MapControl.Renderer.StyleRenderers[typeof(TiledBitmapStyle)] = tbitmRender;
            MapControl.Map.Limiter = new ViewportLimiter();
            MapControl.Map.Limiter.PanLimits = new Mapsui.Geometries.BoundingBox(0, 0, terrainWidth, terrainWidth);

            GPSTrackerLayer.IsMapInfoLayer = true;
            GPSTrackerLayer.DataSource = new GPSTrackerProvider(GPSTrackerLayer, currentBounds);
            GPSTrackerLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(GPSTrackerLayer);
            //GPSTrackerLayer.DataChanged += (a,b) => MapControl.RefreshData();

            MapMarkersLayer.IsMapInfoLayer = true;
            MapMarkersLayer.DataSource = new MapMarkerProvider(MapMarkersLayer, currentBounds);
            MapMarkersLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(MapMarkersLayer);
            //MapMarkersLayer.DataChanged += (a, b) => MapControl.RefreshData();



            LayerList.Initialize(MapControl.Map.Layers);
            //MapControl.ZoomToBox(new Point(0, 0), new Point(8192, 8192));
            //MapControl.Navigator.ZoomTo(1, new Point(512,512), 5);
        }

        private void MapControlOnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            GPSEditPopup.IsOpen = false;
            if (args.ClickCount > 1)
            {


                var info = MapControl.GetMapInfo(args.GetPosition(MapControl).ToMapsui(), 12);

                if (info.Feature is GPSTrackerFeature gpsTrackerFeature)
                {

                    GPSEdit.Tracker = gpsTrackerFeature.Tracker;
                    GPSEditPopup.Placement = PlacementMode.Mouse;
                    GPSEditPopup.StaysOpen = false;
                    GPSEditPopup.AllowsTransparency = true;
                    GPSEditPopup.IsOpen = true;
                }

                args.Handled = true;
            }
            else
            {
                
            }
        }



    }


    public class BoundBox : Mapsui.Geometries.Geometry
    {
        public BoundBox(BoundingBox x)
        {
            BoundingBox = x;
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override bool Equals(Geometry geom)
        {
            var point = geom as BoundBox;
            if (point == null) return false;
            return BoundingBox.Equals(point.BoundingBox);
        }

        public override BoundingBox BoundingBox { get; }

        public override double Distance(Point point)
        {
            return BoundingBox.Distance(point);
        }

        public override bool Contains(Point point)
        {
            return BoundingBox.Contains(point);
        }
    }


   

}
