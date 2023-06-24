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
using Mapsui.UI.Wpf.Extensions;
using Mapsui.Widgets;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Point = System.Windows.Point;
using VerticalAlignment = System.Windows.VerticalAlignment;
using XamlVector = System.Windows.Vector;
using Mapsui.Extensions;
using System.Linq;
using Mapsui.Animations;

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

        Visibility Visibility { get; set; }

        void InvalidateVisual();


    }

    public partial class MapControl : Grid, IMapControl
    {
        //https://github.com/Mapsui/Mapsui/blob/31b9099c758f93daa5aded630e4ac45ec4308cab/Mapsui.UI.Shared/MapControl.cs with Renderer swapped out


        private double _unSnapRotationDegrees;
        // Flag indicating if a drawing process is running
        private bool _drawing = false;
        // Flag indicating if the control has to be redrawn
        private bool _invalidated;
        // Flag indicating if a new drawing process should start
        private bool _refresh = false;
        // Action to call for a redraw of the control
        private protected Action? _invalidate;
        // Timer for loop to invalidating the control
        private System.Threading.Timer? _invalidateTimer;
        // Interval between two calls of the invalidate function in ms
        private int _updateInterval = 16;
        // Stopwatch for measuring drawing times
        private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        // saving list of extended Widgets
        private List<IWidgetExtended>? _extendedWidgets;
        // saving list of touchable Widgets
        private List<IWidget>? _touchableWidgets;
        // keeps track of the widgets count to see if i need to recalculate the extended widgets.
        private int _updateWidget = 0;
        // keeps track of the widgets count to see if i need to recalculate the touchable widgets.
        private int _updateTouchableWidget;

        private protected void CommonInitialize()
        {
            // Create map
            Map = new Map();
            // Create timer for invalidating the control
            _invalidateTimer?.Dispose();
            _invalidateTimer = new System.Threading.Timer(InvalidateTimerCallback, null, System.Threading.Timeout.Infinite, 16);
            // Start the invalidation timer
            StartUpdates(false);
        }

        private protected void CommonDrawControl(object canvas)
        {
            if (_drawing) return;
            if (Renderer is null) return;
            if (Map is null) return;
            if (!Map.Navigator.Viewport.HasSize()) return;

            // Start drawing
            _drawing = true;

            // Start stopwatch before updating animations and drawing control
            _stopwatch.Restart();

            // All requested updates up to this point will be handled by this redraw
            _refresh = false;
            Renderer.Render(canvas, Map.Navigator.Viewport, Map.Layers, Map.Widgets, Map.BackColor);

            // Stop stopwatch after drawing control
            _stopwatch.Stop();

            // If we are interested in performance measurements, we save the new drawing time
            _performance?.Add(_stopwatch.Elapsed.TotalMilliseconds);

            // End drawing
            _drawing = false;
            _invalidated = false;
        }

        private void InvalidateTimerCallback(object? state)
        {
            // In MAUI if you use binding there is an event where the new value is null even though
            // the current value en the value you are binding to are not null. Perhaps this should be
            // considered a bug.
            if (Map is null) return;

            // Check, if we have to redraw the screen

            if (Map.UpdateAnimations() == true)
                _refresh = true;

            if (Map.Navigator.UpdateAnimations())
                _refresh = true;

            if (!_refresh)
                return;

            if (_drawing)
            {
                if (_performance != null)
                    _performance.Dropped++;

                return;
            }

            if (_invalidated)
            {
                return;
            }

            _invalidated = true;
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
            _invalidateTimer?.Change(0, _updateInterval);
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
            _invalidateTimer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Force a update of control
        /// </summary>
        /// <remarks>
        /// When this function is called, the control draws itself once 
        /// </remarks>
        public void ForceUpdate()
        {
            _invalidated = true;
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
                    throw new ArgumentOutOfRangeException($"{nameof(UpdateInterval)} must be greater than 0");

                if (_updateInterval != value)
                {
                    _updateInterval = value;
                    StartUpdates();
                }
            }
        }

        private Performance? _performance;

        /// <summary>
        /// Object to save performance information about the drawing of the map
        /// </summary>
        /// <remarks>
        /// If this is null, no performance information is saved.
        /// </remarks>
        public Performance? Performance
        {
            get => _performance;
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
            get => _unSnapRotationDegrees;
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
            get => _reSnapRotationDegrees;
            set
            {
                if (_reSnapRotationDegrees != value)
                {
                    _reSnapRotationDegrees = value;
                    OnPropertyChanged();
                }
            }
        }

        public float PixelDensity => GetPixelDensity();

        private IRenderer _renderer = new MapRenderer();

        /// <summary>
        /// Renderer that is used from this MapControl
        /// </summary>
        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                if (value is null) throw new NullReferenceException(nameof(Renderer));
                if (_renderer != value)
                {
                    _renderer = value;
                    OnPropertyChanged();
                }
            }
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
#if __FORMS__ || __MAUI__ || __AVALONIA__
        public new event PropertyChangedEventHandler? PropertyChanged;
#else
        public event PropertyChangedEventHandler? PropertyChanged;
#endif

#if __FORMS__ || __MAUI__
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#else
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
            UnsubscribeFromMapEvents(Map);
        }

        /// <summary>
        /// Subscribe to map events
        /// </summary>
        /// <param name="map">Map, to which events to subscribe</param>
        private void SubscribeToMapEvents(Map map)
        {
            map.DataChanged += Map_DataChanged;
            map.PropertyChanged += Map_PropertyChanged;
            map.RefreshGraphicsRequest += Map_RefreshGraphicsRequest;
        }

        private void Map_RefreshGraphicsRequest(object? sender, EventArgs e)
        {
            RefreshGraphics();
        }

        /// <summary>
        /// Unsubscribe from map events
        /// </summary>
        /// <param name="map">Map, to which events to unsubscribe</param>
        private void UnsubscribeFromMapEvents(Map map)
        {
            var localMap = map;
            localMap.DataChanged -= Map_DataChanged;
            localMap.PropertyChanged -= Map_PropertyChanged;
            localMap.RefreshGraphicsRequest -= Map_RefreshGraphicsRequest;
            localMap.AbortFetch();
        }

        public void Refresh(ChangeType changeType = ChangeType.Discrete)
        {
            Map.Refresh(changeType);
        }

        public void RefreshGraphics()
        {
            _refresh = true;
        }

        private void Map_DataChanged(object? sender, DataChangedEventArgs? e)
        {
            try
            {
                if (e == null)
                {
                    Logger.Log(LogLevel.Warning, "Unexpected error: DataChangedEventArgs can not be null");
                }
                else if (e.Cancelled)
                {
                    Logger.Log(LogLevel.Warning, "Fetching data was cancelled.");
                }
                else if (e.Error is WebException)
                {
                    Logger.Log(LogLevel.Warning, $"A WebException occurred. Do you have internet? Exception: {e.Error?.Message}", e.Error);
                }
                else if (e.Error != null)
                {
                    Logger.Log(LogLevel.Warning, $"An error occurred while fetching data. Exception: {e.Error?.Message}", e.Error);
                }
                else // no problems
                {
                    RefreshGraphics();
                }
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.Warning, $"Unexpected exception in {nameof(Map_DataChanged)}", exception);
            }
        }
        // ReSharper disable RedundantNameQualifier - needed for iOS for disambiguation

        private void Map_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            else if (e.PropertyName == nameof(Map.Extent))
            {
                CallHomeIfNeeded();
                Refresh();
            }
            else if (e.PropertyName == nameof(Map.Layers))
            {
                CallHomeIfNeeded();
                Refresh();
            }
            //if (e.PropertyName == nameof(Map.Limiter))
            //{
            //    _viewport.Limiter = Map?.Limiter;
            //}
        }
        // ReSharper restore RedundantNameQualifier

        public void CallHomeIfNeeded()
        {
            if (!Map.HomeIsCalledOnce && Map.Navigator.Viewport.HasSize() && Map?.Extent is not null)
            {
                Map.Home?.Invoke(Map.Navigator);
                Map.HomeIsCalledOnce = true;
            }
        }

        private Map _map = new Map();

        /// <summary>
        /// Map holding data for which is shown in this MapControl
        /// </summary>
        public Map? Map
        {
            get => _map;
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));

                BeforeSetMap();
                _map = value;
                AfterSetMap(_map);
                OnPropertyChanged();
            }
        }

        private void BeforeSetMap()
        {
            if (Map is null) return; // Although the Map property can not null the map argument can null during initializing and binding.

            UnsubscribeFromMapEvents(Map);
        }

        private void AfterSetMap(Map? map)
        {
            if (map is null) return; // Although the Map property can not null the map argument can null during initializing and binding.

            map.Navigator.SetSize(ViewportWidth, ViewportHeight);
            SubscribeToMapEvents(map);
            CallHomeIfNeeded();
            Refresh();
        }

        /// <inheritdoc />
        public Mapsui.MPoint ToPixels(Mapsui.MPoint coordinateInDeviceIndependentUnits)
        {
            return new Mapsui.MPoint(
                coordinateInDeviceIndependentUnits.X * PixelDensity,
                coordinateInDeviceIndependentUnits.Y * PixelDensity);
        }

        /// <inheritdoc />
        public Mapsui.MPoint ToDeviceIndependentUnits(Mapsui.MPoint coordinateInPixels)
        {
            return new Mapsui.MPoint(coordinateInPixels.X / PixelDensity, coordinateInPixels.Y / PixelDensity);
        }

        /// <summary>
        /// Refresh data of Map, but don't paint it
        /// </summary>
        public void RefreshData(ChangeType changeType = ChangeType.Discrete)
        {
            Map.RefreshData(changeType);
        }



        private protected void OnInfo(MapInfoEventArgs? mapInfoEventArgs)
        {
            if (mapInfoEventArgs == null) return;

            Map?.OnInfo(mapInfoEventArgs); // Also propagate to Map
            Info?.Invoke(this, mapInfoEventArgs);
        }

        private bool WidgetTouched(IWidget widget, MPoint screenPosition)
        {
            var result = widget.HandleWidgetTouched(Map.Navigator, screenPosition);

            if (!result && widget is Hyperlink hyperlink && !string.IsNullOrWhiteSpace(hyperlink.Url))
            {
                OpenBrowser(hyperlink.Url!);
            }

            return result;
        }

        /// <inheritdoc />
        public MapInfo GetMapInfo(Mapsui.MPoint screenPosition, int margin = 0)
        {
            if (screenPosition == null)
                return null;

            return Renderer?.GetMapInfo(screenPosition.X, screenPosition.Y, Map.Navigator.Viewport, Map?.Layers ?? new LayerCollection(), margin);
        }

        /// <inheritdoc />
        public byte[] GetSnapshot(IEnumerable<ILayer>? layers = null)
        {
            using var stream = Renderer.RenderToBitmapStream(Map.Navigator.Viewport, layers ?? Map?.Layers ?? new LayerCollection(), pixelDensity: PixelDensity);
            return stream.ToArray();
        }

        /// <summary>
        /// Check if a widget or feature at a given screen position is clicked/tapped
        /// </summary>
        /// <param name="screenPosition">Screen position to check for widgets and features</param>
        /// <param name="startScreenPosition">Screen position of Viewport/MapControl</param>
        /// <param name="numTaps">Number of clickes/taps</param>
        /// <returns>True, if something done </returns>
        private protected MapInfoEventArgs? CreateMapInfoEventArgs(MPoint? screenPosition, MPoint? startScreenPosition, int numTaps)
        {
            return CreateMapInfoEventArgs(
                    Map?.GetWidgetsOfMapAndLayers() ?? new List<IWidget>(),
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
        private MapInfoEventArgs? CreateMapInfoEventArgs(IEnumerable<IWidget> widgets, MPoint? screenPosition,
            MPoint? startScreenPosition, Func<IWidget, MPoint, bool> widgetCallback, int numTaps)
        {
            if (screenPosition == null || startScreenPosition == null)
                return null;

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
            var mapInfo = Renderer?.GetMapInfo(screenPosition.X, screenPosition.Y, Map.Navigator.Viewport, Map?.Layers ?? new LayerCollection());

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

        private protected void SetViewportSize()
        {
            var hadSize = Map.Navigator.Viewport.HasSize();
            Map.Navigator.SetSize(ViewportWidth, ViewportHeight);
            if (!hadSize && Map.Navigator.Viewport.HasSize()) Map.OnViewportSizeInitialized();
            CallHomeIfNeeded();
            Refresh();
        }

        private protected void CommonDispose(bool disposing)
        {
            if (disposing)
            {
                Unsubscribe();
                StopUpdates();
                _invalidateTimer?.Dispose();
            }
            _invalidateTimer = null;
        }

        private bool HandleMoving(MPoint position, bool leftButton, int clickCount, bool shift)
        {
            var extendedWidgets = GetExtendedWidgets();
            if (extendedWidgets.Count == 0)
                return false;

            var widgetArgs = new WidgetArgs(clickCount, leftButton, shift);
            foreach (var extendedWidget in extendedWidgets)
            {
                if (extendedWidget.HandleWidgetMoving(Map.Navigator, position, widgetArgs))
                    return true;
            }

            return false;
        }

        private bool HandleTouchingTouched(MPoint position, bool leftButton, int clickCount, bool shift)
        {
            if (HandleTouching(position, leftButton, clickCount, shift))
            {
                return true;
            }

            if (HandleTouched(position, leftButton, clickCount, shift))
            {
                return true;
            }

            return false;
        }


        private bool HandleTouching(MPoint position, bool leftButton, int clickCount, bool shift)
        {
            var extendedWidgets = GetExtendedWidgets();
            if (extendedWidgets.Count == 0)
                return false;

            // Exit on Touchable Widgets or else the Button Handling for example does not work
            // TODO: In the Next Mapsui Major Version handle Touch Events here
            var touchableWidgets = GetTouchableWidgets();
            var touchedWidgets = WidgetTouch.GetTouchedWidget(position, position, touchableWidgets);
            if (touchedWidgets.Any())
                return false;

            var widgetArgs = new WidgetArgs(clickCount, leftButton, shift);
            foreach (var extendedWidget in extendedWidgets)
            {
                if (extendedWidget.HandleWidgetTouching(Map.Navigator, position, widgetArgs))
                    return true;
            }

            return false;
        }

        private bool HandleTouched(MPoint position, bool leftButton, int clickCount, bool shift)
        {
            var extendedWidgets = GetExtendedWidgets();
            if (extendedWidgets.Count == 0)
                return false;

            // Exit on Touchable Widgets or else the Button Handling for example does not work
            // TODO: In the Next Mapsui Major Version handle Touch Events here
            var touchableWidgets = GetTouchableWidgets();
            var touchedWidgets = WidgetTouch.GetTouchedWidget(position, position, touchableWidgets);
            if (touchedWidgets.Any())
                return false;

            var widgetArgs = new WidgetArgs(clickCount, leftButton, shift);
            foreach (var extendedWidget in extendedWidgets)
            {
                if (extendedWidget.HandleWidgetTouched(Map.Navigator, position, widgetArgs))
                    return true;
            }

            return false;
        }

        private List<IWidgetExtended> GetExtendedWidgets()
        {
            if (_updateWidget != Map.Widgets.Count || _extendedWidgets == null)
            {
                _updateWidget = Map.Widgets.Count;
                _extendedWidgets = new List<IWidgetExtended>();
                var widgetsOfMapAndLayers = Map.GetWidgetsOfMapAndLayers().ToList();
                foreach (var widget in widgetsOfMapAndLayers)
                {
                    if (widget is IWidgetExtended extendedWidget)
                    {
                        _extendedWidgets.Add(extendedWidget);
                    }
                }
            }

            return _extendedWidgets;
        }

        private List<IWidget> GetTouchableWidgets()
        {
            if (_updateTouchableWidget != Map.Widgets.Count || _touchableWidgets == null)
            {
                _updateTouchableWidget = Map.Widgets.Count;
                _touchableWidgets = new List<IWidget>();
                var touchableWidgets = Map.GetWidgetsOfMapAndLayers().ToList();
                foreach (var widget in touchableWidgets)
                {
                    if (widget is IWidgetExtended)
                        continue;

                    if (widget is IWidgetTouchable { Touchable: false }) continue;

                    _touchableWidgets.Add(widget);
                }
            }

            return _touchableWidgets;
        }

































        // https://github.com/Mapsui/Mapsui/blob/31b9099c758f93daa5aded630e4ac45ec4308cab/Mapsui.UI.Wpf/MapControl.cs





        private readonly Rectangle _selectRectangle = CreateSelectRectangle();
        private Mapsui.MPoint? _downMousePosition;
        private bool _mouseDown;
        private Mapsui.MPoint? _previousMousePosition;
        private RenderMode _renderMode;
        private bool _hasBeenManipulated;
        private double _virtualRotation;
        private double _innerRotation;
        private readonly FlingTracker _flingTracker = new();
        private MPoint? _currentMousePosition;

        public MouseWheelAnimation MouseWheelAnimation { get; } = new MouseWheelAnimation();

        /// <summary>
        /// Fling is called, when user release mouse button or lift finger while moving with a certain speed, higher than speed of swipe 
        /// </summary>
        public event EventHandler<SwipedEventArgs>? Fling;

        static private bool GLRunning = false; // true == GL rendering completely disabled, false == one GL window allowed


        public MapControl()
        {
            CommonInitialize();
            Initialize();
        }

        private void Initialize()
        {
            _invalidate = () =>
            {
                if (Dispatcher.CheckAccess()) InvalidateCanvas();
                else RunOnUIThread(InvalidateCanvas);
            };

            Children.Add(WpfCanvas);

            if (!GLRunning)
            {
                GLRunning = true;
                try
                {
                    SkiaCanvas = CreateSkiaGLRenderElement();
                    Children.Add(SkiaCanvas as SKGLWpfControl);
                }
                catch (System.AccessViolationException) { }
                catch (GLFWException) { }
            }

            if (SkiaCanvas == null) // either GL didn't run or failed
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
            //MouseWheel += MapControlMouseWheel;

            SizeChanged += MapControlSizeChanged;

            ManipulationStarted += OnManipulationStarted;
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            ManipulationInertiaStarting += OnManipulationInertiaStarting;

            IsManipulationEnabled = true;

            WpfCanvas.Visibility = Visibility.Collapsed;
            SkiaCanvas.Visibility = Visibility.Visible;


            RenderMode = RenderMode.Skia;
            RefreshGraphics();
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

        private ISkiaCanvas SkiaCanvas { get; set; }

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
                    Renderer = new Mapsui.Rendering.Skia.MapRenderer();
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

        public event EventHandler<FeatureInfoEventArgs>? FeatureInfo; // todo: Remove and add sample for alternative

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
            var mouseWheelDelta = e.Delta;
            _currentMousePosition = e.GetPosition(this).ToMapsui();
            Map.Navigator.MouseWheelZoom(mouseWheelDelta, _currentMousePosition);
        }

        private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
            SetViewportSize();
        }

        private void MapControlMouseLeave(object sender, MouseEventArgs e)
        {
            _previousMousePosition = null;
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
            if (HandleTouching(e.GetPosition(this).ToMapsui(), true, e.ClickCount, ShiftPressed))
                return;

            var touchPosition = e.GetPosition(this).ToMapsui();
            _previousMousePosition = touchPosition;
            _downMousePosition = touchPosition;
            _mouseDown = true;
            _flingTracker.Clear();
            CaptureMouse();

        }

        private static bool IsInBoxZoomMode()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private void MapControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(this).ToMapsui();
            if (HandleTouched(mousePosition, true, e.ClickCount, ShiftPressed))
                return;

            if (_previousMousePosition != null)
            {
                if (IsInBoxZoomMode())
                {
                    var previous = Map.Navigator.Viewport.ScreenToWorld(_previousMousePosition.X, _previousMousePosition.Y);
                    var current = Map.Navigator.Viewport.ScreenToWorld(mousePosition.X, mousePosition.Y);
                    ZoomToBox(previous, current);
                }
                else if (_downMousePosition != null && IsClick(mousePosition, _downMousePosition))
                {
                    HandleFeatureInfo(e);
                    OnInfo(CreateMapInfoEventArgs(mousePosition, _downMousePosition, e.ClickCount));
                }
            }

            RefreshData();
            _mouseDown = false;

            // TacControl doesn't fling
            //double velocityX;
            //double velocityY;
            //
            //(velocityX, velocityY) = _flingTracker.CalcVelocity(1, DateTime.Now.Ticks);
            //
            //if (Math.Abs(velocityX) > 200 || Math.Abs(velocityY) > 200)
            //{
            //    // This was the last finger on screen, so this is a fling
            //    e.Handled = OnFlinged(velocityX, velocityY);
            //}
            //_flingTracker.RemoveId(1);

            _previousMousePosition = new MPoint();
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

            Map.Navigator.Fling(velocityX, velocityY, 1000);

            return true;
        }

        private static bool IsClick(Mapsui.MPoint currentPosition, Mapsui.MPoint previousPosition)
        {
            return
                Math.Abs(currentPosition.X - previousPosition.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - previousPosition.Y) < SystemParameters.MinimumVerticalDragDistance;
        }

        private void MapControlTouchUp(object? sender, TouchEventArgs e)
        {
            if (!_hasBeenManipulated)
            {
                var touchPosition = e.GetTouchPoint(this).Position.ToMapsui();
                // todo: Pass the touchDown position. It needs to be set at touch down.

                // todo: Figure out how to do a number of taps for WPF
                OnInfo(CreateMapInfoEventArgs(touchPosition, touchPosition, 1));
            }
        }

        public void OpenBrowser(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                // The default for this has changed in .net core, you have to explicitly set if to true for it to work.
                UseShellExecute = true
            });
        }

        private void HandleFeatureInfo(MouseButtonEventArgs e)
        {
            if (FeatureInfo == null) return; // don't fetch if you the call back is not set.

            if (_downMousePosition == e.GetPosition(this).ToMapsui())
                foreach (var layer in Map.Layers)
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    (layer as IFeatureInfo)?.GetFeatureInfo(Map.Navigator.Viewport, _downMousePosition.X, _downMousePosition.Y,
                                OnFeatureInfo);
                }

        }

        private void OnFeatureInfo(IDictionary<string, IEnumerable<IFeature>> features)
        {
            FeatureInfo?.Invoke(this, new FeatureInfoEventArgs { FeatureInfo = features });
        }

        private void MapControlMouseMove(object sender, MouseEventArgs e)
        {
            if (HandleMoving(e.GetPosition(this).ToMapsui(), e.LeftButton == MouseButtonState.Pressed, 0, ShiftPressed))
                return;

            if (IsInBoxZoomMode())
            {
                DrawBbox(e.GetPosition(this));
                return;
            }

            _currentMousePosition = e.GetPosition(this).ToMapsui();

            if (_mouseDown)
            {
                if (_previousMousePosition == null)
                {
                    // Usually MapControlMouseLeftButton down initializes _previousMousePosition but in some
                    // situations it can be null. So far I could only reproduce this in debug mode when putting
                    // a breakpoint and continuing.
                    return;
                }

                _flingTracker.AddEvent(1, _currentMousePosition, DateTime.Now.Ticks);
                Map.Navigator.Drag(_currentMousePosition, _previousMousePosition);
                _previousMousePosition = _currentMousePosition;
            }
        }

        public void ZoomToBox(MPoint beginPoint, MPoint endPoint)
        {
            var box = new MRect(beginPoint.X, beginPoint.Y, endPoint.X, endPoint.Y);
            Map.Navigator.ZoomToBox(box, duration: 300); ;
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
                if (_previousMousePosition == null) return; // can happen during debug

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

        private static void OnManipulationInertiaStarting(object? sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 25 * 96.0 / (1000.0 * 1000.0);
        }

        private void OnManipulationStarted(object? sender, ManipulationStartedEventArgs e)
        {
            _hasBeenManipulated = false;
            _virtualRotation = Map.Navigator.Viewport.Rotation;
        }

        private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
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

            if (Map.Navigator.RotationLock == false)
            {
                _virtualRotation += angle - prevAngle;

                rotationDelta = RotationCalculations.CalculateRotationDeltaWithSnapping(
                    _virtualRotation, Map.Navigator.Viewport.Rotation, _unSnapRotationDegrees, _reSnapRotationDegrees);
            }

            Map.Navigator.Pinch(center, previousCenter, radius / previousRadius, rotationDelta);
            e.Handled = true;
        }

        private double GetDeltaScale(XamlVector scale)
        {
            if (Map.Navigator.ZoomLock) return 1;
            var deltaScale = (scale.X + scale.Y) / 2;
            if (Math.Abs(deltaScale) < Constants.Epsilon)
                return 1; // If there is no scaling the deltaScale will be 0.0 in Windows Phone (while it is 1.0 in wpf)
            if (!(Math.Abs(deltaScale - 1d) > Constants.Epsilon)) return 1;
            return deltaScale;
        }

        private void OnManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            Refresh();
        }

        private void SKElementOnPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _map?.Dispose();
            }

            CommonDispose(disposing);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool ShiftPressed => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
    }

}
