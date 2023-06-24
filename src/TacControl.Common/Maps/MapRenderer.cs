using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Nts;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Rendering.Skia.SkiaWidgets;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Widgets.Zoom;
using Mapsui.Extensions;
using NetTopologySuite.Geometries;
using SkiaSharp;
using Mapsui.Rendering.Skia.Cache;

namespace TacControl.Common.Maps
{
    public class MapRenderer : IRenderer
    {
        private const int TilesToKeepMultiplier = 3;
        private const int MinimumTilesToKeep = 32;
        private readonly IRenderCache _renderCache = new RenderCache();
        public IRenderCache RenderCache => _renderCache;
        private readonly SymbolCache _symbolCache = new SymbolCache();
        private readonly IDictionary<object, BitmapInfo> _tileCache =
            new Dictionary<object, BitmapInfo>(new IdentityComparer<object>());
        private long _currentIteration;

        public ISymbolCache SymbolCache => _symbolCache;

        public IDictionary<Type, IWidgetRenderer> WidgetRenders { get; } = new Dictionary<Type, IWidgetRenderer>();

        /// <summary>
        /// Dictionary holding all special renderers for styles
        /// </summary>
        public IDictionary<Type, IStyleRenderer> StyleRenderers { get; } = new Dictionary<Type, IStyleRenderer>();

        static MapRenderer()
        {
            DefaultRendererFactory.Create = () => new MapRenderer();
        }

        public MapRenderer()
        {

            // These I don't use
            WidgetRenders[typeof(Hyperlink)] = new HyperlinkWidgetRenderer();
            WidgetRenders[typeof(ScaleBarWidget)] = new ScaleBarWidgetRenderer();
            WidgetRenders[typeof(ZoomInOutWidget)] = new ZoomInOutWidgetRenderer();






            StyleRenderers[typeof(SvgStyle)] = new SvgStyleRenderer();
            StyleRenderers[typeof(SvgStyleLazy)] = new SvgStyleRenderer();
            StyleRenderers[typeof(TiledBitmapStyle)] = new TiledBitmapRenderer();
            StyleRenderers[typeof(VelocityIndicatorStyle)] = new VelocityIndicatorRenderer();
            StyleRenderers[typeof(PolylineMarkerStyle)] = new PolylineMarkerRenderer();
            StyleRenderers[typeof(MarkerIconStyle)] = new MarkerIconRenderer();
            StyleRenderers[typeof(MarkerLabelStyle)] = new MarkerLabelRenderer();
            WidgetRenders[typeof(GridWidget)] = new GridWidgetRenderer();

            // https://github.com/Mapsui/Mapsui/pull/1482/files#diff-2134ffb2ebcb3764845b9bef16171ef809edcfadd385b0f2101bb06f0fd33ec1R41
            StyleRenderers[typeof(RasterStyle)] = new RasterStyleRenderer();
            StyleRenderers[typeof(VectorStyle)] = new VectorStyleRenderer();
            StyleRenderers[typeof(LabelStyle)] = new LabelStyleRenderer();
            StyleRenderers[typeof(SymbolStyle)] = new SymbolStyleRenderer();
            StyleRenderers[typeof(CalloutStyle)] = new CalloutStyleRenderer();

        }

        public void Render(object target, Viewport viewport, IEnumerable<ILayer> layers,
            IEnumerable<IWidget> widgets, Color background = null)
        {
            var attributions = layers.Where(l => l.Enabled).Select(l => l.Attribution).Where(w => w != null).ToList();

            var allWidgets = widgets.Concat(attributions);

            RenderTypeSave((SKCanvas)target, viewport, layers, allWidgets, background);
        }

        private void RenderTypeSave(SKCanvas canvas, Viewport viewport, IEnumerable<ILayer> layers,
            IEnumerable<IWidget> widgets, Color background = null)
        {
            if (!viewport.HasSize()) return;

            if (background != null) canvas.Clear(background.ToSkia(1));
            Render(canvas, viewport, layers);
            Render(canvas, viewport, widgets, 1);
        }

        public MemoryStream RenderToBitmapStream(Viewport viewport, IEnumerable<ILayer> layers, Color background = null, float pixelDensity = 1)
        {
            try
            {
                var width = (int)viewport.Width;
                var height = (int)viewport.Height;
                var imageInfo = new SKImageInfo((int)Math.Round(width * pixelDensity), (int)Math.Round(height * pixelDensity),
                    SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);

                using (var surface = SKSurface.Create(imageInfo))
                {
                    if (surface == null) return null;
                    // Not sure if this is needed here:
                    if (background != null) surface.Canvas.Clear(background.ToSkia(1));
                    surface.Canvas.Scale(pixelDensity, pixelDensity);
                    Render(surface.Canvas, viewport, layers);
                    using (var image = surface.Snapshot())
                    {
                        using (var data = image.Encode())
                        {
                            var memoryStream = new MemoryStream();
                            data.SaveTo(memoryStream);
                            return memoryStream;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                return null;
            }
        }

        private void Render(SKCanvas canvas, Viewport viewport, IEnumerable<ILayer> layers)
        {
            try
            {
                layers = layers.ToList();

                VisibleFeatureIterator.IterateLayers(viewport, layers, _currentIteration, (v, l, s, f, o, i) => { RenderFeature(canvas, v, l, s, f, o, i); });

                RemovedUnusedBitmapsFromCache();

                _currentIteration++;
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.Error, "Unexpected error in skia renderer", exception);
            }
        }

        private void RemovedUnusedBitmapsFromCache()
        {
            var tilesUsedInCurrentIteration =
                _tileCache.Values.Count(i => i.IterationUsed == _currentIteration);
            var tilesToKeep = tilesUsedInCurrentIteration * TilesToKeepMultiplier;
            tilesToKeep = Math.Max(tilesToKeep, MinimumTilesToKeep);
            var tilesToRemove = _tileCache.Keys.Count - tilesToKeep;

            if (tilesToRemove > 0) RemoveOldBitmaps(_tileCache, tilesToRemove);
        }

        private static void RemoveOldBitmaps(IDictionary<object, BitmapInfo> tileCache, int numberToRemove)
        {
            var counter = 0;
            var orderedKeys = tileCache.OrderBy(kvp => kvp.Value?.IterationUsed).Select(kvp => kvp.Key).ToList();
            foreach (var key in orderedKeys)
            {
                if (counter >= numberToRemove) break;
                var textureInfo = tileCache[key];
                tileCache.Remove(key);
                textureInfo?.Bitmap?.Dispose();
                counter++;
            }
        }

        private void RenderFeature(SKCanvas canvas, Viewport viewport, ILayer layer, IStyle style, IFeature feature, float layerOpacity, long iteration)
        {
            // Check, if we have a special renderer for this style
            if (StyleRenderers.ContainsKey(style.GetType()))
            {
                // Save canvas
                canvas.Save();
                // We have a special renderer, so try, if it could draw this
                bool result = false;
                lock (_renderCache)
                {
                    result = ((ISkiaStyleRenderer)StyleRenderers[style.GetType()]).Draw(canvas, viewport, layer, feature, style, _renderCache, iteration);
                }
                // Restore old canvas
                canvas.Restore();
                // Was it drawn?
                if (result)
                    // Yes, special style renderer drawn correct
                    return;
            }

            // https://github.com/Mapsui/Mapsui/blob/a9c28e1f111605775881fa57382f7142b5c2ade9/Mapsui.Rendering.Skia/MapRenderer.cs#L147

            //if (feature is GeometryFeature geometryFeatureNts)
            //{
            //    GeometryRenderer.Draw(canvas, viewport, style, layerOpacity, geometryFeatureNts, _symbolCache);
            //    return;
            //}
            //else if (feature is RasterFeature rasterFeature)
            //{
            //    RasterRenderer.Draw(canvas, viewport, style, rasterFeature, rasterFeature.Raster, layerOpacity * style.Opacity, _tileCache, _currentIteration);
            //    return;
            //}
            //else if (feature is PointFeature pointFeature)
            //{
            //    PointRenderer.Draw(canvas, viewport, style, pointFeature, pointFeature.Point.X, pointFeature.Point.Y, _symbolCache, layerOpacity * style.Opacity);
            //    return;
            //}
            //else
            if (feature is GPSTrackerFeature gpsFeature) 
            {
                // GPSTrackerFeature uses MarkerIconStyle to render itself
                return;
            }



#if DEBUG
            Debugger.Break();
#endif
        }

        private void Render(object canvas, Viewport viewport, IEnumerable<IWidget> widgets, float layerOpacity)
        {
            WidgetRenderer.Render(canvas, viewport, widgets, WidgetRenders, layerOpacity);
        }

        public MapInfo GetMapInfo(double x, double y, Viewport viewport, IEnumerable<ILayer> layers, int margin = 0)
        {
            // todo: use margin to increase the pixel area
            // todo: We will need to select on style instead of layer

            layers = layers
                .Select(l => (l is RasterizingLayer rl) ? rl.SourceLayer : l)
                .Where(l => l.IsMapInfoLayer);

            var list = new List<MapInfoRecord>();
            var result = new MapInfo()
            {
                ScreenPosition = new MPoint(x, y),
                WorldPosition = viewport.ScreenToWorld(x, y),
                Resolution = viewport.Resolution
            };

            try
            {
                var width = (int)viewport.Width;
                var height = (int)viewport.Height;

                var imageInfo = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);

                var intX = (int)x;
                var intY = (int)y;

                if (intX >= width || intY >= height)
                    return result;

                using (var surface = SKSurface.Create(imageInfo))
                {
                    if (surface == null) return null;

                    surface.Canvas.ClipRect(new SKRect((float)(x - 1), (float)(y - 1), (float)(x + 1), (float)(y + 1)));
                    surface.Canvas.Clear(SKColors.Transparent);

                    var pixmap = surface.PeekPixels();
                    var color = pixmap.GetPixelColor(intX, intY);

                    VisibleFeatureIterator.IterateLayers(viewport, layers, 0, (v, layer, style, feature, opacity, iteration) => {
                        surface.Canvas.Save();
                        // 1) Clear the entire bitmap
                        surface.Canvas.Clear(SKColors.Transparent);
                        // 2) Render the feature to the clean canvas
                        RenderFeature(surface.Canvas, v, layer, style, feature, opacity, 0);
                        // 3) Check if the pixel has changed.
                        if (color != pixmap.GetPixelColor(intX, intY))
                            // 4) Add feature and style to result
                            list.Add(new MapInfoRecord(feature, style, layer));
                        surface.Canvas.Restore();
                    });
                }

                if (list.Count == 0)
                    return result;

                list.Reverse();
                var itemDrawnOnTop = list.First();

                result.Feature = itemDrawnOnTop.Feature;
                result.Style = itemDrawnOnTop.Style;
                result.Layer = itemDrawnOnTop.Layer;
                result.MapInfoRecords = list;

            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.Error, "Unexpected error in skia renderer", exception);
            }

            return result;
        }

        public MapInfo GetMapInfo(MPoint screenPosition, Viewport viewport, IEnumerable<ILayer> layers, int margin = 0)
        {
            return GetMapInfo(screenPosition.X, screenPosition.Y, viewport, layers, margin);
        }

        public MemoryStream RenderToBitmapStream(Viewport viewport, IEnumerable<ILayer> layers, Color background = null, float pixelDensity = 1, IEnumerable<IWidget> widgets = null, RenderFormat renderFormat = RenderFormat.Png)
        {
            // https://github.com/Mapsui/Mapsui/blob/master/Mapsui.Rendering.Skia/MapRenderer.cs
            try
            {
                var width = viewport.Width;
                var height = viewport.Height;

                var imageInfo = new SKImageInfo((int)Math.Round(width * pixelDensity), (int)Math.Round(height * pixelDensity),
                    SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);

                MemoryStream memoryStream = new MemoryStream();

                switch (renderFormat)
                {
                    case RenderFormat.Skp:
                        {
                            using var pictureRecorder = new SKPictureRecorder();
                            using var skCanvas = pictureRecorder.BeginRecording(new SKRect(0, 0, Convert.ToSingle(width), Convert.ToSingle(height)));
                            RenderTo(viewport, layers, background, pixelDensity, widgets, skCanvas);
                            using var skPicture = pictureRecorder.EndRecording();
                            skPicture?.Serialize(memoryStream);
                            break;
                        }
                    case RenderFormat.Png:
                        {
                            using var surface = SKSurface.Create(imageInfo);
                            using var skCanvas = surface.Canvas;
                            RenderTo(viewport, layers, background, pixelDensity, widgets, skCanvas);
                            using var image = surface.Snapshot();
                            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                            data.SaveTo(memoryStream);
                            break;
                        }
                    case RenderFormat.WebP:
                        {
                            using var surface = SKSurface.Create(imageInfo);
                            using var skCanvas = surface.Canvas;
                            RenderTo(viewport, layers, background, pixelDensity, widgets, skCanvas);
                            using var image = surface.Snapshot();
                            var options = new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100);
                            using var peekPixels = image.PeekPixels();
                            using var data = peekPixels.Encode(options);
                            data.SaveTo(memoryStream);
                            break;
                        }
                }

                return memoryStream;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                throw;
            }
        }

        private void RenderTo(Viewport viewport, IEnumerable<ILayer> layers, Color? background, float pixelDensity,
    IEnumerable<IWidget>? widgets, SKCanvas skCanvas)
        {
            if (skCanvas == null) throw new ArgumentNullException(nameof(viewport));

            // Not sure if this is needed here:
            if (background != null) skCanvas.Clear(background.ToSkia());
            skCanvas.Scale(pixelDensity, pixelDensity);
            Render(skCanvas, viewport, layers);
            if (widgets != null)
                Render(skCanvas, viewport, widgets, 1);
        }

        public class IdentityComparer<T> : IEqualityComparer<T> where T : class
        {
            public bool Equals(T obj, T otherObj)
            {
                return obj == otherObj;
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
