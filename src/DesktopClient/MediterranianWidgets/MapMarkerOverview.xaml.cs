using System;
using System.Collections.Generic;
using System.Linq;
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
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Providers;
using Mapsui.UI;
using TacControl.Common;
using TacControl.Common.Maps;
using TacControl.Common.Modules;
using Point = Mapsui.MPoint;
using RasterizingLayer = TacControl.Common.Maps.RasterizingLayer;

namespace TacControl.MediterranianWidgets
{
    /// <summary>
    /// Simplified MapView, only terrain and marker layer. And takes custom marker module to show only subset of markers
    /// </summary>
    public partial class MapMarkerOverview : UserControl
    {
        private MemoryLayer MapMarkersLayer = new Mapsui.Layers.MemoryLayer("Map Markers");
        public MRect currentBounds = new Mapsui.MRect(0, 0, 0, 0);

        public ModuleMarker ModuleMarkerRef
        {
            get => (ModuleMarker)GetValue(ModuleMarkerRefProperty);
            set => SetValue(ModuleMarkerRefProperty, value);
        }

        public static readonly DependencyProperty ModuleMarkerRefProperty = DependencyProperty.Register(nameof(ModuleMarkerRef), typeof(ModuleMarker), typeof(MapMarkerOverview));


        public IMarkerVisibilityManager VisibilityManager { get; set; }


        public MapMarkerOverview()
        {
            InitializeComponent();

            SizeChanged += MapMarkerOverview_SizeChanged;
            
        }

        private void MapMarkerOverview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MapControl.Navigator.NavigateTo(new MRect(0, 0, terrainWidth, terrainWidth));
        }

        private void MapMarkerOverview_OnLoaded(object sender, RoutedEventArgs e)
        {

            MapControl.Renderer = new Common.Maps.MapRenderer();

            MapControl.Renderer.StyleRenderers[typeof(SvgStyle)] = new SvgStyleRenderer();
            MapControl.Renderer.StyleRenderers[typeof(SvgStyleLazy)] = new SvgStyleRenderer();
            MapControl.Renderer.StyleRenderers[typeof(TiledBitmapStyle)] = new TiledBitmapRenderer();
            MapControl.Renderer.StyleRenderers[typeof(VelocityIndicatorStyle)] = new VelocityIndicatorRenderer();
            MapControl.Renderer.StyleRenderers[typeof(PolylineMarkerStyle)] = new PolylineMarkerRenderer();
            MapControl.Renderer.StyleRenderers[typeof(MarkerIconStyle)] = new MarkerIconRenderer();
            MapControl.Renderer.WidgetRenders[typeof(GridWidget)] = new GridWidgetRenderer();




            MapControl.Map.Limiter = new ViewportLimiter();
            MapControl.Map.Limiter.ZoomLimits = new MinMax(0.01, 40);

            MapMarkersLayer.DataSource = new MapMarkerProvider(MapMarkersLayer, currentBounds, VisibilityManager, ModuleMarkerRef);
            MapMarkersLayer.IsMapInfoLayer = true;
            MapMarkersLayer.Style = null; // remove white circle https://github.com/Mapsui/Mapsui/issues/760
            MapControl.Map.Layers.Add(MapMarkersLayer);

            Helper.ParseLayers().ContinueWith(x => Networking.Instance.MainThreadInvoke(() => GenerateLayers(x.Result)));
        }
        int terrainWidth = 0;

        private void GenerateLayers(List<Helper.SvgLayer> layers)
        {
            int index = 0;
            foreach (var svgLayer in layers)
            {
                if (svgLayer.name != "terrain" && svgLayer.name != "roads")
                    continue;

                if (svgLayer.content.GetSize() > 5e7) //> 50MB
                {
                    //#TODO tell the user, this layer is too big and is skipped for safety. TacControl would use TONS of ram, very bad, usually an issue with Forest layer
                    continue;
                }



                var layer = new MemoryLayer(svgLayer.name);
                var renderLayer = new RasterizingLayer(layer, 100, 1D, MapControl.Renderer);

                terrainWidth = svgLayer.width;

                currentBounds = new Mapsui.MRect(0, 0, terrainWidth, terrainWidth);

                var features = new List<IFeature>();
                var feature = new GeometryFeature() { Geometry = new BoundBox(currentBounds), ["Label"] = svgLayer.name };


                if (renderLayer.Enabled)
                {
                    var x = new SvgStyle { image = new Svg.Skia.SKSvg() };
                    using (var stream = svgLayer.content.GetStream())
                    {
                        x.image.Load(stream);
                    }

                    feature.Styles.Add(x);
                }

                features.Add(feature);

                layer.Enabled = true;
                layer.DataSource = new MemoryProvider<IFeature>(features);
                layer.MinVisible = 0;
                layer.MaxVisible = double.MaxValue;
                MapControl.Map.Layers.Insert(index++, renderLayer);
            }

            MapControl.RefreshGraphics();

            MapControl.Navigator.NavigateTo(new MRect(0, 0, terrainWidth, terrainWidth));
        }

    }
}
