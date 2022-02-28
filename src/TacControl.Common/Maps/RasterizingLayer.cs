using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mapsui;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Providers;
using Mapsui.Rendering;

namespace TacControl.Common.Maps
{
    // https://github.com/Mapsui/Mapsui/blob/7ce9087a1cfb3dcfb6550ace05a33db4021c2443/Mapsui/Layers/RasterizingLayer.cs#L11
    public class RasterizingLayer : BaseLayer, IAsyncDataFetcher
    {
        private readonly ConcurrentStack<RasterFeature> _cache;
        private readonly ILayer _layer;
        private readonly bool _onlyRerasterizeIfOutsideOverscan;
        private readonly double _overscan;
        private readonly double _renderResolutionMultiplier;
        private readonly float _pixelDensity;
        private readonly object _syncLock = new object();
        private bool _busy;
        private Viewport _currentViewport;
        private MRect _extent;
        private bool _modified;
        private IEnumerable<IFeature> _previousFeatures;
        private readonly IRenderer _rasterizer = DefaultRendererFactory.Create();
        private FetchInfo? _fetchInfo;
        public Delayer Delayer { get; } = new Delayer();
        private Delayer _rasterizeDelayer = new Delayer();

        /// <summary>
        ///     Creates a RasterizingLayer which rasterizes a layer for performance
        /// </summary>
        /// <param name="layer">The Layer to be rasterized</param>
        /// <param name="delayBeforeRasterize">Delay after viewport change to start rerasterising</param>
        /// <param name="renderResolutionMultiplier"></param>
        /// <param name="rasterizer">Rasterizer to use. null will use the default</param>
        /// <param name="overscanRatio">The ratio of the size of the rasterized output to the current viewport</param>
        /// <param name="onlyRerasterizeIfOutsideOverscan">
        ///     Set the rasterization policy. false will trigger a Rasterization on
        ///     every viewport change. true will trigger a Rerasterization only if the viewport moves outside the existing
        ///     rasterization.
        /// </param>
        public RasterizingLayer(
            ILayer layer,
            int delayBeforeRasterize = 1000,
            double renderResolutionMultiplier = 1,
            IRenderer rasterizer = null,
            double overscanRatio = 1,
            bool onlyRerasterizeIfOutsideOverscan = false,
            float pixelDensity = 1)
        {
            if (overscanRatio < 1)
                throw new ArgumentException($"{nameof(overscanRatio)} must be >= 1", nameof(overscanRatio));

            _layer = layer;
            Name = layer.Name;
            _renderResolutionMultiplier = renderResolutionMultiplier;
            _rasterizer = (rasterizer is Common.Maps.MapRenderer) ? rasterizer : new Common.Maps.MapRenderer(); //rasterizer;
            _cache = new ConcurrentStack<RasterFeature>();
            _overscan = overscanRatio;
            _onlyRerasterizeIfOutsideOverscan = onlyRerasterizeIfOutsideOverscan;
            _pixelDensity = pixelDensity;
            _layer.DataChanged += LayerOnDataChanged;
            Delayer.StartWithDelay = true;
            Delayer.MillisecondsToWait = delayBeforeRasterize;
        }

        public override MRect Extent => _layer.Extent;

        public ILayer ChildLayer => _layer;

        private void LayerOnDataChanged(object sender, DataChangedEventArgs dataChangedEventArgs)
        {
            if (!Enabled) return;
            if (MinVisible > _fetchInfo.Resolution) return;
            if (MaxVisible < _fetchInfo.Resolution) return;
            if (_busy) return;

            _modified = true;

            // Will start immediately if it is not currently waiting. This well be in most cases.
            _rasterizeDelayer.ExecuteDelayed(Rasterize);
        }

        private void Rasterize()
        {
            if (!Enabled) return;
            if (_busy) return;
            _busy = true;
            _modified = false;

            lock (_syncLock)
            {
                try
                {
                    if (_fetchInfo == null) return;
                    if (double.IsNaN(_fetchInfo.Resolution) || _fetchInfo.Resolution <= 0) return;
                    if (_fetchInfo.Extent == null || _fetchInfo.Extent?.Width <= 0 || _fetchInfo.Extent?.Height <= 0) return;
                    var viewport = CreateViewport(_fetchInfo.Extent!, _fetchInfo.Resolution, _renderResolutionMultiplier, _overscan);

                    _currentViewport = viewport;

                    var bitmapStream = _rasterizer.RenderToBitmapStream(viewport, new[] { _layer }, pixelDensity: _pixelDensity);
                    RemoveExistingFeatures();

                    if (bitmapStream != null)
                    {
                        _cache.Clear();
                        var features = new RasterFeature[1];
                        features[0] = new RasterFeature(new MRaster(bitmapStream.ToArray(), viewport.Extent));
                        _cache.PushRange(features);
#if DEBUG
                        Logger.Log(LogLevel.Debug, $"Memory after rasterizing layer {GC.GetTotalMemory(true):N0}");
#endif

                        OnDataChanged(new DataChangedEventArgs());
                    }

                    if (_modified) Delayer.ExecuteDelayed(() => _layer.RefreshData(_fetchInfo));
                }
                finally
                {
                    _busy = false;
                }
            }
        }

        private void RemoveExistingFeatures()
        {
            var features = _cache.ToArray();
            _cache.Clear(); // clear before dispose to prevent possible null disposed exception on render

            // Disposing previous and storing current in the previous field to prevent dispose during rendering.
            if (_previousFeatures != null) DisposeRenderedGeometries(_previousFeatures);
            _previousFeatures = features;
        }

        private static void DisposeRenderedGeometries(IEnumerable<IFeature> features)
        {
            foreach (var feature in features.Cast<RasterFeature>())
            {
                foreach (var key in feature.RenderedGeometry.Keys)
                {
                    var disposable = feature.RenderedGeometry[key] as IDisposable;
                    disposable?.Dispose();
                }
            }
        }

        public static double SymbolSize { get; set; } = 64;

        public override IEnumerable<IFeature> GetFeatures(MRect box, double resolution)
        {
            if (box == null) throw new ArgumentNullException(nameof(box));

            var features = _cache.ToArray();

            // Use a larger extent so that symbols partially outside of the extent are included
            var biggerBox = box.Grow(resolution * SymbolSize * 0.5);

            return features.Where(f => f.Raster != null && f.Raster.Intersects(biggerBox)).ToList();
        }

        public void AbortFetch()
        {
            if (_layer is IAsyncDataFetcher asyncLayer) asyncLayer.AbortFetch();
        }

        public override void RefreshData(FetchInfo fetchInfo)
        {
            if (fetchInfo.Extent == null)
                return;
            var newViewport = CreateViewport(fetchInfo.Extent, fetchInfo.Resolution, _renderResolutionMultiplier, 1);

            if (!Enabled) return;
            if (MinVisible > fetchInfo.Resolution) return;
            if (MaxVisible < fetchInfo.Resolution) return;

            if (!_onlyRerasterizeIfOutsideOverscan ||
                (_currentViewport == null) ||
                (_currentViewport.Resolution != newViewport.Resolution) ||
                !_currentViewport.Extent.Contains(newViewport.Extent))
            {
                // Explicitly set the change type to discrete for rasterization
                _fetchInfo = new FetchInfo(fetchInfo.Extent, fetchInfo.Resolution, fetchInfo.CRS);
                if (_layer is IAsyncDataFetcher)
                    Delayer.ExecuteDelayed(() => _layer.RefreshData(_fetchInfo));
                else
                    Delayer.ExecuteDelayed(Rasterize);
            }
        }

        public void ClearCache()
        {
            if (_layer is IAsyncDataFetcher asyncLayer) asyncLayer.ClearCache();
        }

        private static Viewport CreateViewport(MRect extent, double resolution, double renderResolutionMultiplier,
            double overscan)
        {
            var renderResolution = resolution / renderResolutionMultiplier;
            return new Viewport
            {
                Resolution = renderResolution,
                CenterX = extent.Centroid.X,
                CenterY = extent.Centroid.Y,
                Width = extent.Width * overscan / renderResolution,
                Height = extent.Height * overscan / renderResolution
            };
        }
    }
}
