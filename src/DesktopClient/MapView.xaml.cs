using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
using Color = Mapsui.Styles.Color;
using Geometry = Mapsui.Geometries.Geometry;
using LineStringRenderer = Mapsui.Rendering.Skia.LineStringRenderer;
using MultiLineStringRenderer = Mapsui.Rendering.Skia.MultiLineStringRenderer;
using MultiPolygonRenderer = Mapsui.Rendering.Skia.MultiPolygonRenderer;
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
        public MapView()
        {
            InitializeComponent();
            //MouseWheel += MapControlMouseWheel;
            ParseLayers();
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
            using (var stream = File.OpenRead(@"J:/dev/Arma/SVG/Stratis.svg"))
            {
                using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext()))
                {
                    var xdoc = XDocument.Load(reader);
                    var svg = xdoc.Root;
                    var ns = svg.Name.Namespace;

                    var mainAttributes = svg.Attributes();
                    var defs = svg.Element("defs");
                    List<XElement> layers = new List<XElement>();
                    List<XElement> rootElements = new List<XElement>();

                    foreach (var xElement in svg.Elements()) {
                        if (xElement.Name == ns+"g") {
                            layers.Add(xElement);
                        } else
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


            var layers = ParseLayers();

            foreach (var svgLayer in layers)
            {

                var layer = new Mapsui.Layers.ImageLayer(svgLayer.name);

                //
                var features = new Features();
                var feature = new Feature { Geometry = new BoundBox(new Mapsui.Geometries.BoundingBox(-8192, -8192, 8192, 8192)), ["Label"] = svgLayer.name };

                var x = new SvgStyle {image = new SkiaSharp.Extended.Svg.SKSvg(new SKSize(8192, 8192))};


                using (var stream = new StringReader(svgLayer.content))
                {
                    using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext()))
                    {
                        x.image.Load(reader);
                    }
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
            MapControl.Map.Limiter.PanLimits = new Mapsui.Geometries.BoundingBox(0, 0, 8192, 8192);
            LayerList.Initialize(MapControl.Map.Layers);
            //MapControl.ZoomToBox(new Point(0, 0), new Point(8192, 8192));
            //MapControl.Navigator.ZoomTo(1, new Point(512,512), 5);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MapControl.Navigator.NavigateTo( new Point(512, 512), 2);
        }


        private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MapControl.Map.ZoomLock) return;
            if (!MapControl.Viewport.HasSize) return;

            var mousePos = e.GetPosition(MapControl).ToMapsui();

            var resolution = MapControl.MouseWheelAnimation.GetResolution(e.Delta, (IViewport)MapControl.Viewport, MapControl.Map);
            // Limit target resolution before animation to avoid an animation that is stuck on the max resolution, which would cause a needless delay
            resolution = MapControl.Map.Limiter.LimitResolution(resolution, MapControl.Viewport.Width, MapControl.Viewport.Height, MapControl.Map.Resolutions, MapControl.Map.Envelope);
            MapControl.Navigator.NavigateTo(mousePos, resolution, MapControl.MouseWheelAnimation.Duration, MapControl.MouseWheelAnimation.Easing);
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


    public class SvgStyleRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, IReadOnlyViewport viewport, ILayer layer, IFeature feature, IStyle style,
            ISymbolCache symbolCache)
        {

            var image = ((SvgStyle) style).image;

            var center = viewport.Center;

            //SvgRenderer.Draw(canvas, image, -(float)viewport.Center.X, (float)viewport.Center.Y, (float)viewport.Rotation, 0,0,default, default, default, (float)viewport.Resolution);

            canvas.Save();


            var zoom = 1 / (float)viewport.Resolution;

            var canvasSize = canvas.LocalClipBounds;

            var canvasCenterX = canvasSize.Width / 2;
            var canvasCenterY = canvasSize.Height / 2;


            float num1 = (image.CanvasSize.Width /2 ) * zoom;
            float num2 = (-image.CanvasSize.Height) * zoom;
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

    public class SvgStyle: VectorStyle
    {
        public SkiaSharp.Extended.Svg.SKSvg image;
    }






}
