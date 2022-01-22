using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Mapsui;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.UI;
using Mapsui.UI.Utils;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using Mapsui.Rendering;
using Mapsui.Widgets;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Point = System.Windows.Point;
using VerticalAlignment = System.Windows.VerticalAlignment;
using XamlVector = System.Windows.Vector;

namespace TacControl.Misc
{
    public enum RenderMode
    {
        Skia,
        Wpf
    }


    public interface ISkiaCanvas
    {

        [Category("Appearance")]
        event EventHandler<SKPaintSurfaceEventArgs> PaintSurface;

        [Category("Appearance")]
        event EventHandler<SKPaintGLSurfaceEventArgs> PaintSurfaceGL;

        Visibility Visibility{ get; set; }

        void InvalidateVisual();


    }

    public partial class MapControl : Grid, IMapControl
    {
        //https://github.com/Mapsui/Mapsui/blob/af2bf64d3f45c0a3a7b91d2d58cc2a4fff3d13d3/Mapsui.UI.Shared/MapControl.cs with Renderer swapped out

        private Map _map;
        private double _unSnapRotationDegrees;
        // Flag indicating if a drawing process is running
        private bool _drawing = false;
        // Flag indicating if a new drawing process should start
        private bool _refresh = false;
        // Action to call for a redraw of the control
        private Action _invalidate;
        // Timer for loop to invalidating the control
        private System.Threading.Timer _invalidateTimer;
        // Interval between two calls of the invalidate function in ms
        private int _updateInterval = 16;
        // Stopwatch for measuring drawing times
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

        void CommonInitialize()
        {
            // Create map
            Map = new Map();
            // Create timer for invalidating the control
            _invalidateTimer = new System.Threading.Timer((state) => InvalidateTimerCallback(state), null, System.Threading.Timeout.Infinite, 16);
            // Start the invalidation timer
            StartUpdates(false);
        }

        void CommonDrawControl(object canvas)
        {
            if (_drawing)
                return;
            if (Renderer == null)
                return;
            if (_map == null)
                return;
            if (!Viewport.HasSize)
                return;

            // Start drawing
            _drawing = true;

            // Start stopwatch before updating animations and drawing control
            _stopwatch.Restart();

            // All requested updates up to this point will be handled by this redraw
            _refresh = false;
            Navigator.UpdateAnimations();
            Renderer.Render(canvas, new Viewport(Viewport), _map.Layers, _map.Widgets, _map.BackColor);

            // Stop stopwatch after drawing control
            _stopwatch.Stop();

            // If we are interessted in performance measurements, we save the new drawing time
            if (_performance != null)
                _performance.Add(_stopwatch.Elapsed.TotalMilliseconds);

            // Log drawing time
            Logger.Log(LogLevel.Information, $"Time for drawing control [ms]: {_stopwatch.Elapsed.TotalMilliseconds}");

            // End drawing
            _drawing = false;
        }

        void InvalidateTimerCallback(object state)
        {
            if (!_refresh)
                return;

            if (_drawing)
            {
                if (_performance != null)
                    _performance.Dropped++;

                return;
            }

            _invalidate?.Invoke();
        }

        /// <summary>
        /// Start updates for control
        /// </summary>
        /// <remarks>
        /// When this function is called, the control is redrawn if needed
        /// </remarks>
        public void StartUpdates(bool refresh = true)
        {
            _refresh = refresh;
            _invalidateTimer.Change(0, _updateInterval);
        }

        /// <summary>
        /// Stop updates for control
        /// </summary>
        /// <remarks>
        /// When this function is called, the control stops to redraw itself, 
        /// even if it is needed
        /// </remarks>
        public void StopUpdates()
        {
            _invalidateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Force a update of control
        /// </summary>
        /// <remarks>
        /// When this function is called, the control draws itself once 
        /// </remarks>
        public void ForceUpdate()
        {
            _invalidate?.Invoke();
        }

        /// <summary>
        /// Interval between two redraws of the MapControl in ms
        /// </summary>
        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("UpdateInterval must be greater than 0");

                if (_updateInterval != value)
                {
                    _updateInterval = value;
                    StartUpdates();
                }
            }
        }

        private Performance _performance;

        /// <summary>
        /// Object to save performance information about the drawing of the map
        /// </summary>
        /// <remarks>
        /// If this is null, no performance information is saved.
        /// </remarks>
        public Performance Performance
        {
            get { return _performance; }
            set
            {
                if (_performance != value)
                {
                    _performance = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// After how many degrees start rotation to take place
        /// </summary>
        public double UnSnapRotationDegrees
        {
            get { return _unSnapRotationDegrees; }
            set
            {
                if (_unSnapRotationDegrees != value)
                {
                    _unSnapRotationDegrees = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _reSnapRotationDegrees;

        /// <summary>
        /// With how many degrees from 0 should map snap to 0 degrees
        /// </summary>
        public double ReSnapRotationDegrees
        {
            get { return _reSnapRotationDegrees; }
            set
            {
                if (_reSnapRotationDegrees != value)
                {
                    _reSnapRotationDegrees = value;
                    OnPropertyChanged();
                }
            }
        }

        public float PixelDensity
        {
            get => GetPixelDensity();
        }

        private IRenderer _renderer = new MapRenderer();

        /// <summary>
        /// Renderer that is used from this MapControl
        /// </summary>
        public IRenderer Renderer
        {
            get { return _renderer; }
            set
            {
                if (_renderer != value)
                {
                    _renderer = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly LimitedViewport _viewport = new LimitedViewport();
        private INavigator _navigator;

        /// <summary>
        /// Viewport holding information about visible part of the map. Viewport can never be null.
        /// </summary>
        public IReadOnlyViewport Viewport => _viewport;

        /// <summary>
        /// Handles all manipulations of the map viewport
        /// </summary>
        public INavigator Navigator
        {
            get => _navigator;
            set
            {
                if (_navigator != null)
                {
                    _navigator.Navigated -= Navigated;
                }
                _navigator = value ?? throw new ArgumentException($"{nameof(Navigator)} can not be null");
                _navigator.Navigated += Navigated;
            }
        }

        private void Navigated(object sender, ChangeType changeType)
        {
            _map.Initialized = true;
            Refresh(changeType);
        }

        /// <summary>
        /// Called when the viewport is initialized
        /// </summary>
        public event EventHandler ViewportInitialized; //todo: Consider to use the Viewport PropertyChanged

        /// <summary>
        /// Called whenever a feature in one of the layers in InfoLayers is hitten by a click 
        /// </summary>
        public event EventHandler<MapInfoEventArgs> Info;

        /// <summary>
        /// Called whenever a property is changed
        /// </summary>
#if __FORMS__
        public new event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#else
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#endif

        /// <summary>
        /// Unsubscribe from map events 
        /// </summary>
        public void Unsubscribe()
        {
            UnsubscribeFromMapEvents(_map);
        }

        /// <summary>
        /// Subscribe to map events
        /// </summary>
        /// <param name="map">Map, to which events to subscribe</param>
        private void SubscribeToMapEvents(Map map)
        {
            map.DataChanged += MapDataChanged;
            map.PropertyChanged += MapPropertyChanged;
        }

        /// <summary>
        /// Unsubcribe from map events
        /// </summary>
        /// <param name="map">Map, to which events to unsubscribe</param>
        private void UnsubscribeFromMapEvents(Map map)
        {
            var temp = map;
            if (temp != null)
            {
                temp.DataChanged -= MapDataChanged;
                temp.PropertyChanged -= MapPropertyChanged;
                temp.AbortFetch();
            }
        }

        /// <summary>
        /// Refresh data of the map and than repaint it
        /// </summary>
        public void Refresh(ChangeType changeType = ChangeType.Discrete)
        {
            RefreshData(changeType);
            RefreshGraphics();
        }

        public void RefreshGraphics()
        {
            _refresh = true;
        }

        private void MapDataChanged(object sender, DataChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    if (e == null)
                    {
                        Logger.Log(LogLevel.Warning, "Unexpected error: DataChangedEventArgs can not be null");
                    }
                    else if (e.Cancelled)
                    {
                        Logger.Log(LogLevel.Warning, "Fetching data was cancelled", e.Error);
                    }
                    else if (e.Error is WebException)
                    {
                        Logger.Log(LogLevel.Warning, "A WebException occurred. Do you have internet?", e.Error);
                    }
                    else if (e.Error != null)
                    {
                        Logger.Log(LogLevel.Warning, "An error occurred while fetching data", e.Error);
                    }
                    else // no problems
                    {
                        RefreshGraphics();
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.Warning, $"Unexpected exception in {nameof(MapDataChanged)}", exception);
                }
            });
        }
        // ReSharper disable RedundantNameQualifier - needed for iOS for disambiguation

        private void MapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Mapsui.Layers.Layer.Enabled))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Mapsui.Layers.Layer.Opacity))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Map.BackColor))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Mapsui.Layers.Layer.DataSource))
            {
                Refresh(); // There is a new DataSource so let's fetch the new data.
            }
            else if (e.PropertyName == nameof(Map.Envelope))
            {
                CallHomeIfNeeded();
                Refresh();
            }
            else if (e.PropertyName == nameof(Map.Layers))
            {
                CallHomeIfNeeded();
                Refresh();
            }
            if (e.PropertyName.Equals(nameof(Map.Limiter)))
            {
                _viewport.Limiter = Map.Limiter;
            }
        }
        // ReSharper restore RedundantNameQualifier

        public void CallHomeIfNeeded()
        {
            if (_map != null && !_map.Initialized && _viewport.HasSize && _map?.Envelope != null)
            {
                _map.Home?.Invoke(Navigator);
                _map.Initialized = true;
            }
        }

        /// <summary>
        /// Map holding data for which is shown in this MapControl
        /// </summary>
        public Map Map
        {
            get => _map;
            set
            {
                if (_map != null)
                {
                    UnsubscribeFromMapEvents(_map);
                    _map = null;
                }

                _map = value;

                if (_map != null)
                {
                    SubscribeToMapEvents(_map);
                    Navigator = new Navigator(_map, _viewport);
                    _viewport.Map = Map;
                    _viewport.Limiter = Map.Limiter;
                    CallHomeIfNeeded();
                }

                Refresh();
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public Mapsui.Geometries.Point ToPixels(Mapsui.Geometries.Point coordinateInDeviceIndependentUnits)
        {
            return new Mapsui.Geometries.Point(
                coordinateInDeviceIndependentUnits.X * PixelDensity,
                coordinateInDeviceIndependentUnits.Y * PixelDensity);
        }

        /// <inheritdoc />
        public Mapsui.Geometries.Point ToDeviceIndependentUnits(Mapsui.Geometries.Point coordinateInPixels)
        {
            return new Mapsui.Geometries.Point(coordinateInPixels.X / PixelDensity, coordinateInPixels.Y / PixelDensity);
        }

        private void OnViewportSizeInitialized()
        {
            ViewportInitialized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Refresh data of Map, but don't paint it
        /// </summary>
        public void RefreshData(ChangeType changeType = ChangeType.Discrete)
        {
            _map?.RefreshData(Viewport.Extent, Viewport.Resolution, changeType);
        }

        private void OnInfo(MapInfoEventArgs mapInfoEventArgs)
        {
            if (mapInfoEventArgs == null) return;

            Info?.Invoke(this, mapInfoEventArgs);
        }

        private bool WidgetTouched(IWidget widget, Mapsui.Geometries.Point screenPosition)
        {
            var result = widget.HandleWidgetTouched(Navigator, screenPosition);

            if (!result && widget is Hyperlink hyperlink && !string.IsNullOrWhiteSpace(hyperlink.Url))
            {
                OpenBrowser(hyperlink.Url);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public MapInfo GetMapInfo(Mapsui.Geometries.Point screenPosition, int margin = 0)
        {
            return Renderer.GetMapInfo(screenPosition.X, screenPosition.Y, Viewport, Map.Layers, margin);
        }

        /// <inheritdoc />
        public byte[] GetSnapshot(IEnumerable<ILayer> layers = null)
        {
            byte[] result = null;

            using (var stream = Renderer.RenderToBitmapStream(Viewport, layers ?? Map.Layers, pixelDensity: PixelDensity))
            {
                if (stream != null)
                    result = stream.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Check if a widget or feature at a given screen position is clicked/tapped
        /// </summary>
        /// <param name="screenPosition">Screen position to check for widgets and features</param>
        /// <param name="startScreenPosition">Screen position of Viewport/MapControl</param>
        /// <param name="numTaps">Number of clickes/taps</param>
        /// <returns>True, if something done </returns>
        private MapInfoEventArgs InvokeInfo(Mapsui.Geometries.Point screenPosition, Mapsui.Geometries.Point startScreenPosition, int numTaps)
        {
            return InvokeInfo(
                Map.GetWidgetsOfMapAndLayers(),
                screenPosition,
                startScreenPosition,
                WidgetTouched,
                numTaps);
        }

        /// <summary>
        /// Check if a widget or feature at a given screen position is clicked/tapped
        /// </summary>
        /// <param name="widgets">The Map widgets</param>
        /// <param name="screenPosition">Screen position to check for widgets and features</param>
        /// <param name="startScreenPosition">Screen position of Viewport/MapControl</param>
        /// <param name="widgetCallback">Callback, which is called when Widget is hit</param>
        /// <param name="numTaps">Number of clickes/taps</param>
        /// <returns>True, if something done </returns>
        private MapInfoEventArgs InvokeInfo(IEnumerable<IWidget> widgets, Mapsui.Geometries.Point screenPosition,
            Mapsui.Geometries.Point startScreenPosition, Func<IWidget, Mapsui.Geometries.Point, bool> widgetCallback, int numTaps)
        {
            // Check if a Widget is tapped. In the current design they are always on top of the map.
            var touchedWidgets = WidgetTouch.GetTouchedWidget(screenPosition, startScreenPosition, widgets);

            foreach (var widget in touchedWidgets)
            {
                var result = widgetCallback(widget, screenPosition);

                if (result)
                {
                    return new MapInfoEventArgs
                    {
                        Handled = true
                    };
                }
            }

            // Check which features in the map were tapped.
            var mapInfo = Renderer.GetMapInfo(screenPosition.X, screenPosition.Y, Viewport, Map.Layers);

            if (mapInfo != null)
            {
                return new MapInfoEventArgs
                {
                    MapInfo = mapInfo,
                    NumTaps = numTaps,
                    Handled = false
                };
            }

            return null;
        }

        private void SetViewportSize()
        {
            var hadSize = Viewport.HasSize;
            _viewport.SetSize(ViewportWidth, ViewportHeight);
            if (!hadSize && Viewport.HasSize) OnViewportSizeInitialized();
            CallHomeIfNeeded();
            Refresh();
        }

        /// <summary>
        /// Clear cache and repaint map
        /// </summary>
        public void Clear()
        {
            // not sure if we need this method
            _map?.ClearCache();
            RefreshGraphics();
        }

































        // https://github.com/Mapsui/Mapsui/blob/af2bf64d3f45c0a3a7b91d2d58cc2a4fff3d13d3/Mapsui.UI.Wpf/MapControl.cs





        private readonly Rectangle _selectRectangle = CreateSelectRectangle();
        private Mapsui.Geometries.Point _currentMousePosition;
        private Mapsui.Geometries.Point _downMousePosition;
        private bool _mouseDown;
        private Mapsui.Geometries.Point _previousMousePosition;
        private RenderMode _renderMode;
        private bool _hasBeenManipulated;
        private double _innerRotation;
        private readonly FlingTracker _flingTracker = new FlingTracker();

        public MouseWheelAnimation MouseWheelAnimation { get; } = new MouseWheelAnimation();

        /// <summary>
        /// Fling is called, when user release mouse button or lift finger while moving with a certain speed, higher than speed of swipe 
        /// </summary>
        public event EventHandler<SwipedEventArgs> Fling;

        static private bool GLRunning = false; // true == GL rendering completely disabled, false == one GL window allowed


        public MapControl()
        {
            CommonInitialize();
            Initialize();
        }

        void Initialize()
        {
            _invalidate = () => {
                if (Dispatcher.CheckAccess()) InvalidateCanvas();
                else RunOnUIThread(InvalidateCanvas);
            };

            Children.Add(WpfCanvas);

            if (!GLRunning)
            {
                SkiaCanvas = CreateSkiaGLRenderElement();
                GLRunning = true;

                Children.Add(SkiaCanvas as SKGLWpfControl);
            }
            else
            {
                SkiaCanvas = CreateSkiaRenderElement();

                Children.Add(SkiaCanvas as SKElement);
            }

            SkiaCanvas.PaintSurfaceGL += SKGLElementOnPaintSurface;
            SkiaCanvas.PaintSurface += SKElementOnPaintSurface;


            Children.Add(_selectRectangle);

            Map = new Map();

            Loaded += MapControlLoaded;
            MouseRightButtonDown += MapControlMouseLeftButtonDown;
            MouseRightButtonUp += MapControlMouseLeftButtonUp;

            TouchUp += MapControlTouchUp;

            MouseMove += MapControlMouseMove;
            MouseLeave += MapControlMouseLeave;
            MouseWheel += MapControlMouseWheel;

            SizeChanged += MapControlSizeChanged;

            ManipulationStarted += OnManipulationStarted;
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            ManipulationInertiaStarting += OnManipulationInertiaStarting;

            IsManipulationEnabled = true;

            RenderMode = RenderMode.Skia;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (RenderMode == RenderMode.Wpf) PaintWpf();
            base.OnRender(dc);
        }

        private static Rectangle CreateSelectRectangle()
        {
            return new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                RadiusX = 0.5,
                RadiusY = 0.5,
                StrokeDashArray = new DoubleCollection { 3.0 },
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed
            };
        }

        public Canvas WpfCanvas { get; } = CreateWpfRenderCanvas();

        private ISkiaCanvas SkiaCanvas { get; private set; }

        public RenderMode RenderMode
        {
            get => _renderMode;
            set
            {
                _renderMode = value;
                if (_renderMode == RenderMode.Skia)
                {
                    WpfCanvas.Visibility = Visibility.Collapsed;
                    SkiaCanvas.Visibility = Visibility.Visible;
                    Renderer = new MapRenderer();
                    RefreshGraphics();
                }
                else
                {
                    SkiaCanvas.Visibility = Visibility.Collapsed;
                    WpfCanvas.Visibility = Visibility.Visible;
                    Renderer = new Mapsui.Rendering.Xaml.MapRenderer();
                    RefreshGraphics();
                }
                OnPropertyChanged();
            }
        }

        private static Canvas CreateWpfRenderCanvas()
        {
            return new Canvas
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private static int mVersion = 0;
        private static SKGLWpfControl CreateSkiaGLRenderElement()
        {

            return new SKGLWpfControl(mVersion++);
        }

        private static SKElement CreateSkiaRenderElement()
        {
            return new SKElement
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        public event EventHandler<FeatureInfoEventArgs> FeatureInfo; // todo: Remove and add sample for alternative

        internal void InvalidateCanvas()
        {
            if (RenderMode == RenderMode.Wpf) InvalidateVisual(); // To trigger OnRender of this MapControl
            else SkiaCanvas.InvalidateVisual();
        }

        private void MapControlLoaded(object sender, RoutedEventArgs e)
        {
            SetViewportSize();

            Focusable = true;
        }

        private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Map.ZoomLock) return;
            if (!Viewport.HasSize) return;

            _currentMousePosition = e.GetPosition(this).ToMapsui();

            var resolution = MouseWheelAnimation.GetResolution(e.Delta, _viewport, _map);
            // Limit target resolution before animation to avoid an animation that is stuck on the max resolution, which would cause a needless delay
            resolution = Map.Limiter.LimitResolution(resolution, Viewport.Width, Viewport.Height, Map.Resolutions, Map.Envelope);
            Navigator.ZoomTo(resolution, _currentMousePosition, MouseWheelAnimation.Duration, MouseWheelAnimation.Easing);
        }

        private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
            SetViewportSize();
        }

        private void MapControlMouseLeave(object sender, MouseEventArgs e)
        {
            _previousMousePosition = new Mapsui.Geometries.Point();
            ReleaseMouseCapture();
        }

        private void RunOnUIThread(Action action)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private void MapControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();

            var touchPosition = e.GetPosition(this).ToMapsui();
            _previousMousePosition = touchPosition;
            _downMousePosition = touchPosition;
            _mouseDown = true;
            _flingTracker.Clear();
            CaptureMouse();

            if (!IsInBoxZoomMode())
            {
                if (IsClick(_currentMousePosition, _downMousePosition))
                {
                    HandleFeatureInfo(e);
                    var mapInfoEventArgs = InvokeInfo(touchPosition, _downMousePosition, e.ClickCount);
                    OnInfo(mapInfoEventArgs);
                }
            }
        }

        private static bool IsInBoxZoomMode()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private void MapControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(this).ToMapsui();

            if (IsInBoxZoomMode())
            {
                var previous = Viewport.ScreenToWorld(_previousMousePosition.X, _previousMousePosition.Y);
                var current = Viewport.ScreenToWorld(mousePosition.X, mousePosition.Y);
                ZoomToBox(previous, current);
            }

            RefreshData();
            _mouseDown = false;

            double velocityX;
            double velocityY;

            (velocityX, velocityY) = _flingTracker.CalcVelocity(1, DateTime.Now.Ticks);

            if (Math.Abs(velocityX) > 200 || Math.Abs(velocityY) > 200)
            {
                // This was the last finger on screen, so this is a fling
                e.Handled = OnFlinged(velocityX, velocityY);
            }
            _flingTracker.RemoveId(1);

            _previousMousePosition = new Mapsui.Geometries.Point();
            ReleaseMouseCapture();
        }

        /// <summary>
        /// Called, when mouse/finger/pen flinged over map
        /// </summary>
        /// <param name="velocityX">Velocity in x direction in pixel/second</param>
        /// <param name="velocityY">Velocity in y direction in pixel/second</param>
        private bool OnFlinged(double velocityX, double velocityY)
        {
            var args = new SwipedEventArgs(velocityX, velocityY);

            Fling?.Invoke(this, args);

            if (args.Handled)
                return true;

            Navigator.FlingWith(velocityX, velocityY, 1000);

            return true;
        }

        private static bool IsClick(Mapsui.Geometries.Point currentPosition, Mapsui.Geometries.Point previousPosition)
        {
            return
                Math.Abs(currentPosition.X - previousPosition.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - previousPosition.Y) < SystemParameters.MinimumVerticalDragDistance;
        }

        private void MapControlTouchUp(object sender, TouchEventArgs e)
        {
            if (!_hasBeenManipulated)
            {
                var touchPosition = e.GetTouchPoint(this).Position.ToMapsui();
                // todo: Pass the touchDown position. It needs to be set at touch down.

                // todo: Figure out how to do a number of taps for WPF
                OnInfo(InvokeInfo(touchPosition, touchPosition, 1));
            }
        }

        public void OpenBrowser(string url)
        {
            Process.Start(url);
        }

        private void HandleFeatureInfo(MouseButtonEventArgs e)
        {
            if (FeatureInfo == null) return; // don't fetch if you the call back is not set.

            if (_downMousePosition == e.GetPosition(this).ToMapsui())
                foreach (var layer in Map.Layers)
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    (layer as IFeatureInfo)?.GetFeatureInfo(Viewport, _downMousePosition.X, _downMousePosition.Y,
                        OnFeatureInfo);
                }
        }

        private void OnFeatureInfo(IDictionary<string, IEnumerable<IFeature>> features)
        {
            FeatureInfo?.Invoke(this, new FeatureInfoEventArgs { FeatureInfo = features });
        }

        private void MapControlMouseMove(object sender, MouseEventArgs e)
        {
            if (IsInBoxZoomMode())
            {
                DrawBbox(e.GetPosition(this));
                return;
            }

            _currentMousePosition = e.GetPosition(this).ToMapsui(); //Needed for both MouseMove and MouseWheel event

            if (_mouseDown)
            {
                if (_previousMousePosition == null || _previousMousePosition.IsEmpty())
                {
                    // Usually MapControlMouseLeftButton down initializes _previousMousePosition but in some
                    // situations it can be null. So far I could only reproduce this in debug mode when putting
                    // a breakpoint and continuing.
                    return;
                }

                _flingTracker.AddEvent(1, _currentMousePosition, DateTime.Now.Ticks);

                _viewport.Transform(_currentMousePosition, _previousMousePosition);
                RefreshGraphics();
                _previousMousePosition = _currentMousePosition;
            }
            else
            {
                if (MouseWheelAnimation.IsAnimating())
                {
                    // Disabled because not performing:
                    // Navigator.ZoomTo(_toResolution, _currentMousePosition, _mouseWheelAnimationDuration, Easing.QuarticOut);
                }

            }
        }

        public void ZoomToBox(Mapsui.Geometries.Point beginPoint, Mapsui.Geometries.Point endPoint)
        {
            var width = Math.Abs(endPoint.X - beginPoint.X);
            var height = Math.Abs(endPoint.Y - beginPoint.Y);
            if (width <= 0) return;
            if (height <= 0) return;

            ZoomHelper.ZoomToBoudingbox(beginPoint.X, beginPoint.Y, endPoint.X, endPoint.Y,
                ActualWidth, ActualHeight, out var x, out var y, out var resolution);

            Navigator.NavigateTo(new Mapsui.Geometries.Point(x, y), resolution, 384);

            RefreshData();
            RefreshGraphics();
            ClearBBoxDrawing();
        }

        private void ClearBBoxDrawing()
        {
            RunOnUIThread(() => _selectRectangle.Visibility = Visibility.Collapsed);
        }

        private void DrawBbox(Point newPos)
        {
            if (_mouseDown)
            {
                if (_previousMousePosition == null || _previousMousePosition.IsEmpty()) return;

                var from = _previousMousePosition;
                var to = newPos;

                if (from.X > to.X)
                {
                    var temp = from;
                    from.X = to.X;
                    to.X = temp.X;
                }

                if (from.Y > to.Y)
                {
                    var temp = from;
                    from.Y = to.Y;
                    to.Y = temp.Y;
                }

                _selectRectangle.Width = to.X - from.X;
                _selectRectangle.Height = to.Y - from.Y;
                _selectRectangle.Margin = new Thickness(from.X, from.Y, 0, 0);
                _selectRectangle.Visibility = Visibility.Visible;
            }
        }

        private float ViewportWidth => (float)ActualWidth;
        private float ViewportHeight => (float)ActualHeight;

        private static void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 25 * 96.0 / (1000.0 * 1000.0);
        }

        private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            _hasBeenManipulated = false;
            _innerRotation = _viewport.Rotation;
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var translation = e.DeltaManipulation.Translation;
            var center = e.ManipulationOrigin.ToMapsui().Offset(translation.X, translation.Y);
            var radius = GetDeltaScale(e.DeltaManipulation.Scale);
            var angle = e.DeltaManipulation.Rotation;
            var previousCenter = e.ManipulationOrigin.ToMapsui();
            var previousRadius = 1f;
            var prevAngle = 0f;

            _hasBeenManipulated |= Math.Abs(e.DeltaManipulation.Translation.X) > SystemParameters.MinimumHorizontalDragDistance
                     || Math.Abs(e.DeltaManipulation.Translation.Y) > SystemParameters.MinimumVerticalDragDistance;

            double rotationDelta = 0;

            if (!Map.RotationLock)
            {
                _innerRotation += angle - prevAngle;
                _innerRotation %= 360;

                if (_innerRotation > 180)
                    _innerRotation -= 360;
                else if (_innerRotation < -180)
                    _innerRotation += 360;

                if (Viewport.Rotation == 0 && Math.Abs(_innerRotation) >= Math.Abs(UnSnapRotationDegrees))
                    rotationDelta = _innerRotation;
                else if (Viewport.Rotation != 0)
                {
                    if (Math.Abs(_innerRotation) <= Math.Abs(ReSnapRotationDegrees))
                        rotationDelta = -Viewport.Rotation;
                    else
                        rotationDelta = _innerRotation - Viewport.Rotation;
                }
            }

            _viewport.Transform(center, previousCenter, radius / previousRadius, rotationDelta);
            RefreshGraphics();
            e.Handled = true;
        }

        private double GetDeltaScale(XamlVector scale)
        {
            if (Map.ZoomLock) return 1;
            var deltaScale = (scale.X + scale.Y) / 2;
            if (Math.Abs(deltaScale) < Constants.Epsilon)
                return 1; // If there is no scaling the deltaScale will be 0.0 in Windows Phone (while it is 1.0 in wpf)
            if (!(Math.Abs(deltaScale - 1d) > Constants.Epsilon)) return 1;
            return deltaScale;
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            Refresh();
        }

        private void SKElementOnPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            if (PixelDensity <= 0)
                return;

            var canvas = args.Surface.Canvas;

            canvas.Scale(PixelDensity, PixelDensity);

            CommonDrawControl(canvas);
        }

        private void SKGLElementOnPaintSurface(object sender, SKPaintGLSurfaceEventArgs args) //  SKPaintSurfaceEventArgs
        {
            if (PixelDensity <= 0)
                return;

            var canvas = args.Surface.Canvas;

            canvas.Scale(PixelDensity, PixelDensity);

            CommonDrawControl(canvas);
        }


        private void PaintWpf()
        {
            CommonDrawControl(WpfCanvas);
        }

        private float GetPixelDensity()
        {
            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource == null) throw new Exception("PresentationSource is null");
            var compositionTarget = presentationSource.CompositionTarget;
            if (compositionTarget == null) throw new Exception("CompositionTarget is null");

            var matrix = compositionTarget.TransformToDevice;

            var dpiX = matrix.M11;
            var dpiY = matrix.M22;

            if (dpiX != dpiY) throw new ArgumentException();

            return (float)dpiX;
        }
    }
}
