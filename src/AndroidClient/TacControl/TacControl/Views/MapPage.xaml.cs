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

            //MapControl.TouchStarted += MapControlOnMouseLeftButtonDown;
            MapControl_OnLoaded();
        }

        //public MapPage(Action<IMapControl> setup, Func<MapControl, MapClickedEventArgs, bool> c = null)
        //{
        //    InitializeComponent();
        //
        //    MapControl.RotationLock = false;
        //    MapControl.UnSnapRotationDegrees = 30;
        //    MapControl.ReSnapRotationDegrees = 5;
        //    MapControl.Info += MapControl_Info;
        //
        //    setup(MapControl);
        //
        //    Clicker = c;
        //   
        //}

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

           

            var wantedDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string filePath = "";
            if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svgz")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svgz");
            else if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svg")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svg");

            if (string.IsNullOrEmpty(filePath))
            {
                ImageDirectory.Instance.RequestMapfile(GameState.Instance.gameInfo.worldName, wantedDirectory)
                    .ContinueWith(
                        (x) =>
                        {
                            MapControl_OnLoaded();
                        });
                return ret;
            }

            bool isCompressed = filePath.EndsWith("z");

            using (var fileStream = File.OpenRead(filePath))
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

        private void MapControl_OnLoaded()
        {


            //MapControl.Map.Layers.Clear();
            //
            //
            //MapControl.Map = new Map();


            var layers = ParseLayers();
            if (layers.Count == 0) return;
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

                //
                var features = new Features();
                var feature = new Feature { Geometry = new BoundBox(currentBounds), ["Label"] = svgLayer.name };

                var x = new SvgStyle { image = new Svg.Skia.SKSvg() };
          
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgLayer.content)))
                {
                    //using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext())) {
                        x.image.Load(stream);
                    //}
                }


                feature.Styles.Add(x);
                features.Add(feature);

                //
                layer.DataSource = new MemoryProvider(features);
                MapControl.Map.Layers.Add(layer);
            }

            //var layer = new Mapsui.Layers.ImageLayer("Base");
            //layer.DataSource = CreateMemoryProviderWithDiverseSymbols();
            //MapControl.Map.Layers.Add(layer);


            var svgRender = new SvgStyleRenderer();
            MapControl.Renderer.StyleRenderers[typeof(SvgStyle)] = svgRender;
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



            //LayerList.Initialize(MapControl.Map.Layers);
            //MapControl.ZoomToBox(new Point(0, 0), new Point(8192, 8192));
            //MapControl.Navigator.ZoomTo(1, new Point(512,512), 5);
        }

        //private void MapControlOnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        //{
        //    GPSEditPopup.IsOpen = false;
        //    if (args.ClickCount > 1)
        //    {
        //
        //
        //        var info = MapControl.GetMapInfo(args.GetPosition(MapControl).ToMapsui(), 12);
        //
        //        if (info.Feature is GPSTrackerFeature gpsTrackerFeature)
        //        {
        //
        //            GPSEdit.Tracker = gpsTrackerFeature.Tracker;
        //            GPSEditPopup.Placement = PlacementMode.Mouse;
        //            GPSEditPopup.StaysOpen = false;
        //            GPSEditPopup.AllowsTransparency = true;
        //            GPSEditPopup.IsOpen = true;
        //        }
        //
        //        args.Handled = true;
        //    }
        //    else
        //    {
        //
        //    }
        //}


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

        public override double Distance(Mapsui.Geometries.Point point)
        {
            return BoundingBox.Distance(point);
        }

        public override bool Contains(Mapsui.Geometries.Point point)
        {
            return BoundingBox.Contains(point);
        }
    }


    public class SvgStyleRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, IReadOnlyViewport viewport, ILayer layer, IFeature feature, IStyle style,
            ISymbolCache symbolCache)
        {

            var image = ((SvgStyle)style).image;

            var center = viewport.Center;

            //SvgRenderer.Draw(canvas, image, -(float)viewport.Center.X, (float)viewport.Center.Y, (float)viewport.Rotation, 0,0,default, default, default, (float)viewport.Resolution);

            canvas.Save();


            var zoom = 1 / (float)viewport.Resolution;

            var canvasSize = canvas.LocalClipBounds;

            var canvasCenterX = canvasSize.Width / 2;
            var canvasCenterY = canvasSize.Height / 2;


            float width = (float)MapPage.currentBounds.Width;
            float height = (float)MapPage.currentBounds.Height;


            float num1 = (width / 2) * zoom;
            float num2 = (-height) * zoom;
            canvas.Translate(canvasCenterX, num2 + canvasCenterY);

            canvas.Translate(-(float)viewport.Center.X * zoom, (float)viewport.Center.Y * zoom);

            canvas.Scale(zoom, zoom);


            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);


            canvas.DrawPicture(image.Picture, new SKPaint()
            {
                IsAntialias = true
            });
            canvas.Restore();






            return true;
        }
    }
}
