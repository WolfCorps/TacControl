using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Mapsui;
using Mapsui.Animations;
using Mapsui.Disposing;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Manipulations;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using Mapsui.UI.Wpf.Extensions;
using Mapsui.Utilities;
using Mapsui.Widgets;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using TacControl.Common.Maps;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MapsuiManipulation = Mapsui.Manipulations.Manipulation;
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

        Visibility Visibility { get; set; }

        void InvalidateVisual();


    }

    public partial class MapControl : Grid, INotifyPropertyChanged, IDisposable, IMapControl
    {
        //https://github.com/Mapsui/Mapsui/blob/3df38358202f42334cff68ee87cb283dcb1db02b/Mapsui.UI.Shared/MapControl.cs with Renderer swapped out


        private readonly TapGestureTracker _tapGestureTracker = new();
        private readonly FlingTracker _flingTracker = new();
        private ScreenSize _mapControlScreenSize = new(0, 0);
        private RenderController? _renderController;

        /// <summary>
        /// The movement allowed between a touch down and touch up in a touch gestures in device independent pixels.
        /// </summary>
#if __WINDOWSFORMS__
    [DefaultValue(8)] // Fix WOF1000 Error
#endif
        public int MaxTapGestureMovement { get; set; } = 8;

        /// <summary>
        /// Use fling gesture to move the map. Default is true. Fling means that the map will continue to move for a 
        /// short time after the user has lifted the finger.
        /// </summary>
#if __WINDOWSFORMS__
    [DefaultValue(true)] // Fix WOF1000 Error
#endif
        public bool UseFling { get; set; } = true;

        /// <summary>
        /// Called whenever the map is clicked. The MapInfoEventArgs contain the features that were hit in
        /// the layers that have IsMapInfoLayer set to true. 
        /// </summary>
        /// <remarks>
        /// The Map.Tapped event is preferred over the Info event. This event is kept for backwards compatibility.
        /// </remarks>
        public event EventHandler<MapInfoEventArgs>? Info;
        /// <summary>
        /// Event that is triggered when the map is tapped. Can be a single tap, double tap or long press.
        /// </summary>
        public event EventHandler<MapEventArgs>? MapTapped;
        /// <summary>
        /// Event that is triggered when on pointer down.
        /// </summary>
        public event EventHandler<MapEventArgs>? MapPointerPressed;
        /// <summary>
        /// Event that is triggered when on pointer move. Can be a drag or hover.
        /// </summary>
        public event EventHandler<MapEventArgs>? MapPointerMoved;
        /// <summary>
        /// Event that is triggered when on pointer up.
        /// </summary>
        public event EventHandler<MapEventArgs>? MapPointerReleased;

        private void SharedConstructor()
        {
            //PlatformUtilities.SetOpenInBrowserFunc(OpenInBrowser);
            Map = new Map();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // Mapsui.Rendering.Skia use Mapsui.Nts where GetDbaseLanguageDriver need encoding providers
            _renderController = new(() => Map, InvalidateCanvas);
        }

        private void SharedOnSizeChanged(double width, double height)
        {
            _mapControlScreenSize = new ScreenSize(width, height);
            TryUpdateViewportSize();
        }

        public void SetMapRenderer(IMapRenderer mapRenderer)
        {
            if (_renderController is null)
                return;
            _renderController.SetMapRenderer(mapRenderer);
        }


        /// <summary>
        /// Force a update of control
        /// </summary>
        /// <remarks>
        /// When this function is called, the control draws itself once 
        /// </remarks>
        public void ForceUpdate()
        {
            InvalidateCanvas();
        }

        /// <summary>
        /// Called whenever a property is changed
        /// </summary>
#if __MAUI__ || __AVALONIA__
    public new event PropertyChangedEventHandler? PropertyChanged;
#else
        public event PropertyChangedEventHandler? PropertyChanged;
#endif

#if __MAUI__
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
            var request = (e as RefreshGraphicsEventArgs)?.Request;
            _renderController?.RefreshGraphics(request);
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
            _renderController?.RefreshGraphics();
        }

        private void Map_DataChanged(object? sender, DataChangedEventArgs? e)
        {
            try
            {
                if (sender is ILayer layer)
                {
                    _renderController?.UpdateDrawables(Map.Navigator.Viewport, layer, Map.RenderService);
                }

                if (e == null)
                {
                    Logger.Log(LogLevel.Warning, "Unexpected error: DataChangedEventArgs can not be null");
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
                Refresh();
            }
            else if (e.PropertyName == nameof(Map.Layers))
            {
                Refresh();
            }
        }

#pragma warning disable IDISP002 // Is Disposed in SharedDispose
        private DisposableWrapper<Map>? _map;
#pragma warning restore IDISP002

#if __MAUI__

    public static readonly BindableProperty MapProperty = BindableProperty.Create(nameof(Map),
        typeof(Map), typeof(MapControl), default(Map), defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: MapPropertyChanged, propertyChanging: MapPropertyChanging);

    private static void MapPropertyChanging(BindableObject bindable,
        object oldValue, object newValue)
    {
        var mapControl = (MapControl)bindable;
        mapControl.BeforeSetMap();
    }

    private static void MapPropertyChanged(BindableObject bindable,
        object oldValue, object newValue)
    {
        var mapControl = (MapControl)bindable;
        mapControl.AfterSetMap((Map)newValue);
    }

    public Map Map
    {
        get => (Map)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

#else
        /// <summary>
        /// Map holding data for which is shown in this MapControl
        /// </summary>
#if __BLAZOR__
    [Parameter]
    [SuppressMessage("Usage", "BL0007:Component parameters should be auto properties")]
#endif
#if __WINDOWSFORMS__
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
#endif
        public Map Map
        {
            get
            {
                if (_map == null)
                {
                    _map = new DisposableWrapper<Map>(new Map(), true);
                    AfterSetMap(_map.WrappedObject);
                    OnPropertyChanged();
                }

                return _map.WrappedObject;
            }
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));

                BeforeSetMap();
                _map?.Dispose();
                _map = new DisposableWrapper<Map>(value, false);
                AfterSetMap(value);
                OnPropertyChanged();
            }
        }
#endif

        private void BeforeSetMap()
        {
            if (Map is null) return; // Although the Map property can not null the map argument can null during initializing and binding.

            UnsubscribeFromMapEvents(Map);
        }

        private void AfterSetMap(Map? map)
        {
            if (map is null)
                return; // Although the Map property can not null the map argument can null during initializing and binding.
            TryUpdateViewportSize();
            SubscribeToMapEvents(map);
            _renderController?.SetupDrawableFactory(map.RenderService);
            Refresh();
        }

        /// <summary>
        /// Refresh data of Map, but don't paint it
        /// </summary>
        public void RefreshData(ChangeType changeType = ChangeType.Discrete)
        {
            Map.RefreshData(changeType);
        }

        protected void OnMapInfo(MapInfoEventArgs mapInfoEventArgs)
        {
            Map?.OnMapInfo(mapInfoEventArgs); // Also propagate to Map
            Info?.Invoke(this, mapInfoEventArgs);
        }

        /// <inheritdoc />
        public byte[] GetSnapshot(IEnumerable<ILayer>? layers = null, RenderFormat renderFormat = RenderFormat.Png, int quality = 100)
        {
            var pixelDensity = GetPixelDensity();
            if (!pixelDensity.HasValue)
                throw new Exception("PixelDensity is not initialized");

            using var stream = _renderController?.RenderToBitmapStream(Map.Navigator.Viewport, layers ?? Map.Layers ?? [], Map.RenderService, pixelDensity: pixelDensity.Value, renderFormat: renderFormat, quality: quality)
                 ?? throw new ArgumentNullException(nameof(_renderController));
            return stream.ToArray();
        }

        private MapInfoEventArgs CreateMapInfoEventArgs(ScreenPosition screenPosition, MPoint worldPosition, GestureType gestureType)
        {
            return new MapInfoEventArgs(screenPosition, worldPosition, gestureType, Map, GetMapInfo, GetRemoteMapInfoAsync);
        }

        public MapInfo GetMapInfo(ScreenPosition screenPosition, IEnumerable<ILayer> layers)
        {
            return _renderController?.GetMapInfo(screenPosition, Map.Navigator.Viewport, layers, Map.RenderService)
                ?? throw new ArgumentNullException(nameof(_renderController));
        }

        protected Task<MapInfo> GetRemoteMapInfoAsync(ScreenPosition screenPosition, Viewport viewport, IEnumerable<ILayer> layers)
        {
            return RemoteMapInfoFetcher.GetRemoteMapInfoAsync(screenPosition, viewport, layers);
        }

        /// <summary>
        /// Tries to set the size of the MapControl.Map.Viewport.
        /// </summary>
        private void TryUpdateViewportSize()
        {
            if (_mapControlScreenSize.Width <= 0 || _mapControlScreenSize.Height <= 0)
                return;

            if (Map is Map map)
            {
                var hadSize = map.Navigator.Viewport.HasSize();
                map.Navigator.SetSize(_mapControlScreenSize.Width, _mapControlScreenSize.Height);
                if (!hadSize && map.Navigator.Viewport.HasSize()) map.OnViewportSizeInitialized();
            }
        }

        private void SharedDispose(bool disposing)
        {
            if (disposing)
            {
                _renderController?.Dispose();
                _renderController = null;
                Unsubscribe();
                _map?.Dispose();
                _map = null;
            }
        }

        private bool OnWidgetTapped(ScreenPosition screenPosition, MPoint worldPosition, GestureType gestureType, bool shiftPressed)
        {
            var eventArgs = new WidgetEventArgs(screenPosition, worldPosition, gestureType, Map, shiftPressed, GetMapInfo, GetRemoteMapInfoAsync);

            var touchedWidgets = WidgetInput.GetWidgetsAtPosition(screenPosition, Map);
            foreach (var widget in touchedWidgets)
            {
                if (Logger.Settings.LogWidgetEvents)
                    Logger.Log(LogLevel.Information, $"{nameof(OnWidgetTapped)} - {widget.GetType().Name} {nameof(GestureType)}: {gestureType} KeyState: {shiftPressed}");
                widget.OnTapped(eventArgs);
                if (eventArgs.Handled)
                    return true;
            }
            return false;
        }

        private bool OnWidgetPointerPressed(ScreenPosition screenPosition, MPoint worldPosition, bool shiftPressed)
        {
            var eventArgs = new WidgetEventArgs(screenPosition, worldPosition, GestureType.Press, Map, shiftPressed, GetMapInfo, GetRemoteMapInfoAsync);

            foreach (var widget in WidgetInput.GetWidgetsAtPosition(screenPosition, Map))
            {
                if (Logger.Settings.LogWidgetEvents)
                    Logger.Log(LogLevel.Information, $"{nameof(OnWidgetPointerPressed)} - {widget.GetType().Name}");
                widget.OnPointerPressed(eventArgs);
                if (eventArgs.Handled)
                    return true;
            }
            return false;
        }

        private bool OnWidgetPointerMoved(ScreenPosition screenPosition, MPoint worldPosition, GestureType gestureType, bool shiftPressed)
        {
            var eventArgs = new WidgetEventArgs(screenPosition, worldPosition, gestureType, Map, shiftPressed, GetMapInfo, GetRemoteMapInfoAsync);

            foreach (var widget in WidgetInput.GetWidgetsAtPosition(screenPosition, Map))
            {
                widget.OnPointerMoved(eventArgs);
                if (eventArgs.Handled)
                    return true;
            }
            return false;
        }

        private bool OnWidgetPointerReleased(ScreenPosition screenPosition, MPoint worldPosition, bool shiftPressed)
        {
            var eventArgs = new WidgetEventArgs(screenPosition, worldPosition, GestureType.Release, Map, shiftPressed, GetMapInfo, GetRemoteMapInfoAsync);

            foreach (var widget in WidgetInput.GetWidgetsAtPosition(screenPosition, Map))
            {
                if (Logger.Settings.LogWidgetEvents)
                    Logger.Log(LogLevel.Information, $"{nameof(OnWidgetPointerReleased)} - {widget.GetType().Name}");
                widget.OnPointerReleased(eventArgs);
                if (eventArgs.Handled)
                    return true;
            }
            return false;
        }

        private bool OnTapped(ScreenPosition screenPosition, GestureType gestureType)
        {
            var worldPosition = Map.Navigator.Viewport.ScreenToWorld(screenPosition);
            if (OnWidgetTapped(screenPosition, worldPosition, gestureType, GetShiftPressed()))
                return true;
            if (Map is null)
                return false;
            if (OnMapTapped(screenPosition, worldPosition, gestureType))
                return true;
            OnMapInfo(CreateMapInfoEventArgs(screenPosition, worldPosition, gestureType));
            return false;
        }

        private bool OnPointerPressed(ReadOnlySpan<ScreenPosition> positions)
        {
            if (positions.Length != 1)
                return false;

            _flingTracker.Restart();
            _tapGestureTracker.Restart(positions[0]);
            var screenPosition = positions[0];
            var worldPosition = Map.Navigator.Viewport.ScreenToWorld(screenPosition);
            if (OnWidgetPointerPressed(screenPosition, worldPosition, GetShiftPressed()))
                return true;
            return OnMapPointerPressed(screenPosition, worldPosition);
        }

        private bool OnPointerMoved(ReadOnlySpan<ScreenPosition> screenPositions, bool isHovering)
        {
            if (screenPositions.Length != 1)
                return false;

            var gestureType = isHovering ? GestureType.Hover : GestureType.Drag;
            var screenPosition = screenPositions[0];
            var worldPosition = Map.Navigator.Viewport.ScreenToWorld(screenPosition);
            if (OnWidgetPointerMoved(screenPosition, worldPosition, gestureType, GetShiftPressed()))
                return true;
            if (OnMapPointerMoved(screenPosition, worldPosition, gestureType))
                return true;
            if (!isHovering)
                _flingTracker.AddEvent(screenPosition, DateTime.Now.Ticks);
            return false;
        }

        private bool OnPointerReleased(ReadOnlySpan<ScreenPosition> screenPositions)
        {
            if (screenPositions.Length != 1)
                return false;
            if (GetPixelDensity() is not float pixelDensity)
                return false;

            var handled = false;
            var screenPosition = screenPositions[0];
            var worldPosition = Map.Navigator.Viewport.ScreenToWorld(screenPosition);
            if (OnWidgetPointerReleased(screenPosition, worldPosition, GetShiftPressed()))
                handled = true; // Set to handled but still handle tap in the next line
            if (!handled && OnMapPointerReleased(screenPosition, worldPosition))
                handled = true;
            if (_tapGestureTracker.TapIfNeeded(screenPositions[0], MaxTapGestureMovement * pixelDensity, OnTapped))
                handled = true;
            if (UseFling)
                _flingTracker.FlingIfNeeded((vX, vY) => Map.Navigator.Fling(vX, vY, 1000));
            // Only refresh when nothing claimed the event. A handler that sets e.Handled = true
            // takes ownership of the event and is responsible for calling map.RefreshGraphics()
            // itself — either directly or via a side effect such as a viewport change. Calling
            // Refresh() unconditionally would upgrade any targeted partial refresh to a full one.
            if (!handled)
                Refresh();
            return handled;
        }

        protected virtual bool OnMapTapped(ScreenPosition screenPosition, MPoint worldPosition, GestureType gestureType)
        {
            if (Logger.Settings.LogMapEvents)
                Logger.Log(LogLevel.Information, $"{nameof(OnMapTapped)} - {nameof(GestureType)}: {gestureType}");

            var eventArgs = new MapEventArgs(screenPosition, worldPosition, gestureType, Map, GetMapInfo,
                GetRemoteMapInfoAsync);
            Map.OnTapped(eventArgs);
            if (!eventArgs.Handled)
                MapTapped?.Invoke(this, eventArgs);

            return eventArgs.Handled;
        }

        protected virtual bool OnMapPointerPressed(ScreenPosition screenPosition, MPoint worldPosition)
        {
            if (Logger.Settings.LogMapEvents)
                Logger.Log(LogLevel.Information, $"{nameof(OnMapPointerPressed)}");

            var eventArgs = new MapEventArgs(screenPosition, worldPosition, GestureType.Press, Map, GetMapInfo,
                GetRemoteMapInfoAsync);
            Map.OnPointerPressed(eventArgs);
            if (!eventArgs.Handled)
                MapPointerPressed?.Invoke(this, eventArgs);

            return eventArgs.Handled;
        }

        protected virtual bool OnMapPointerMoved(ScreenPosition screenPosition, MPoint worldPosition, GestureType gestureType)
        {
            var eventArgs = new MapEventArgs(screenPosition, worldPosition, gestureType,
                Map, GetMapInfo, GetRemoteMapInfoAsync);
            Map.OnPointerMoved(eventArgs);
            if (!eventArgs.Handled)
                MapPointerMoved?.Invoke(this, eventArgs);

            return eventArgs.Handled;
        }

        protected virtual bool OnMapPointerReleased(ScreenPosition screenPosition, MPoint worldPosition)
        {
            if (Logger.Settings.LogMapEvents)
                Logger.Log(LogLevel.Information, $"{nameof(OnMapPointerReleased)}");

            var eventArgs = new MapEventArgs(screenPosition, worldPosition, GestureType.Release, Map, GetMapInfo,
                GetRemoteMapInfoAsync);
            Map.OnPointerReleased(eventArgs);
            if (!eventArgs.Handled)
                MapPointerReleased?.Invoke(this, eventArgs);

            return eventArgs.Handled;
        }

        private record ScreenSize(double Width, double Height);
















        // These are mine, added to WPF MapControl

        private RenderMode _renderMode;
        private double _innerRotation;

        public MouseWheelAnimation MouseWheelAnimation { get; } = new MouseWheelAnimation();

        /// <summary>
        /// Fling is called, when user release mouse button or lift finger while moving with a certain speed, higher than speed of swipe 
        /// </summary>
        //public event EventHandler<SwipedEventArgs>? Fling;

        static private bool GLRunning = false; // true == GL rendering completely disabled, false == one GL window allowed

        public RenderMode RenderMode
        {
            get => _renderMode;
            set
            {
                _renderMode = value;
                if (_renderMode == RenderMode.Skia)
                {
                    //WpfCanvas.Visibility = Visibility.Collapsed;
                    SkiaCanvas.Visibility = Visibility.Visible;
                    _renderController.SetMapRenderer(new MapRenderer());
                    RefreshGraphics();
                }
                else
                {
                    SkiaCanvas.Visibility = Visibility.Collapsed;
                    //WpfCanvas.Visibility = Visibility.Visible;
                    _renderController.SetMapRenderer(new Mapsui.Rendering.Skia.MapRenderer());
                    RefreshGraphics();
                }
                OnPropertyChanged();
            }
        }








        // https://github.com/Mapsui/Mapsui/blob/3df38358202f42334cff68ee87cb283dcb1db02b/Mapsui.UI.Wpf/MapControl.cs





        public static readonly DependencyProperty MapProperty = DependencyProperty.Register(
    nameof(Map),
    typeof(Map),
    typeof(MapControl),
    new PropertyMetadata(null, OnMapPropertyChanged));

        private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MapControl mapControl && e.NewValue is Map map)
            {
                mapControl.Map = map;
            }
        }

        private readonly ManipulationTracker _manipulationTracker = new();

        public MapControl()
        {
           //Children.Add(SkiaCanvas);


            //> ADD
            if (!GLRunning)
            {
                GLRunning = true;
                try
                {
                    var el = CreateSkiaGLRenderElement();
                    SkiaCanvas = el;
                    Children.Add(el as SKGLWpfControl);
                    el.PaintSurfaceGL += SKGLElementOnPaintSurface;
                }
                catch (System.AccessViolationException) { }
                catch (GLFWException) { }
            }

            if (SkiaCanvas == null) // either GL didn't run or failed
            {
                SkiaCanvas = CreateSkiaRenderElement();

                Children.Add(SkiaCanvas as SKElement);

                (SkiaCanvas as SKElement).PaintSurface += SKElementOnPaintSurface;
            }


            //< ADD


            Loaded += MapControlLoaded;
            SizeChanged += MapControlSizeChanged;

            MouseRightButtonDown += MapControlMouseLeftButtonDown; // ADD, Swap mouse buttons
            MouseRightButtonUp += MapControlMouseLeftButtonUp; // ADD, Swap mouse buttons

            MouseMove += MapControlMouseMove;
            MouseLeave += MapControlMouseLeave;
            // MouseWheel += MapControlMouseWheel; // ADD, Disable mouse wheel

            ManipulationInertiaStarting += OnManipulationInertiaStarting;
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;

            TouchDown += MapControl_TouchDown;
            TouchUp += MapControlTouchUp;

            IsManipulationEnabled = true;

            SkiaCanvas.Visibility = Visibility.Visible;

            RenderMode = RenderMode.Skia;

            SharedConstructor();
        }

        ////> ADD
        //protected override void OnRender(DrawingContext dc)
        //{
        //    if (RenderMode == RenderMode.Wpf) PaintWpf();
        //    base.OnRender(dc);
        //}
        ////< ADD

        public void InvalidateCanvas()
        {
            if (RenderMode == RenderMode.Wpf) InvalidateVisual(); // To trigger OnRender of this MapControl
            else
                if (Dispatcher.CheckAccess()) SkiaCanvas.InvalidateVisual();
            else RunOnUIThread(SkiaCanvas.InvalidateVisual);
        }

        private FrameworkElement SkiaCanvas { get; } = CreateSkiaRenderElement();

        private static SKElement CreateSkiaRenderElement() => new()
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        private static int mVersion = 0;
        private static SKGLWpfControl CreateSkiaGLRenderElement()
        {
            return new SKGLWpfControl(mVersion++);
        }

        private void MapControlLoaded(object sender, RoutedEventArgs e)
        {
            Focusable = true;
        }

        private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var mouseWheelDelta = e.Delta;
            var mousePosition = e.GetPosition(this).ToScreenPosition();
            Map.Navigator.MouseWheelZoom(mouseWheelDelta, mousePosition);
        }

        private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Accessing ActualWidth and ActualHeight before size changed causes an exception, so we need to do it here.
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
            SharedOnSizeChanged(ActualWidth, ActualHeight);
        }

        private void MapControlMouseLeave(object sender, MouseEventArgs e)
        {
            ReleaseMouseCapture();
        }

        private void RunOnUIThread(Action action)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.BeginInvoke(action);
            else
                action();
        }

        private void MapControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this).ToScreenPosition();
            _manipulationTracker.Restart([position]);

            if (OnPointerPressed([position]))
                return;

            CaptureMouse();
        }

        private void MapControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this).ToScreenPosition();
            OnPointerReleased([position]);
            ReleaseMouseCapture();
        }

        private void MapControl_TouchDown(object? sender, TouchEventArgs e)
        {
            var position = e.GetTouchPoint(this).Position.ToScreenPosition();
            if (OnPointerPressed([position]))
                return;
        }

        private void MapControlTouchUp(object? sender, TouchEventArgs e)
        {
            var position = e.GetTouchPoint(this).Position.ToScreenPosition();
            if (OnPointerReleased([position]))
                return;
        }

        public void OpenInBrowser(string url)
        {
            Catch.TaskRun(() =>
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    // The default for this has changed in .net core, you have to explicitly set if to true for it to work.
                    UseShellExecute = true
                });
            });
        }

        private void MapControlMouseMove(object sender, MouseEventArgs e)
        {
            var isHovering = IsHovering(e);
            var position = e.GetPosition(this).ToScreenPosition();

            if (OnPointerMoved([position], isHovering))
                return;

            if (!isHovering)
                _manipulationTracker.Manipulate([position], Map.Navigator.Manipulate);
        }

        private static void OnManipulationInertiaStarting(object? sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 25 * 96.0 / (1000.0 * 1000.0);
        }

        private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            Map.Navigator.Manipulate(ToManipulation(e));
        }

        private static MapsuiManipulation ToManipulation(ManipulationDeltaEventArgs e)
        {
            var translation = e.DeltaManipulation.Translation;

            var previousCenter = e.ManipulationOrigin.ToScreenPosition();
            var center = previousCenter.Offset(translation.X, translation.Y);
            var scaleFactor = GetScaleFactor(e.DeltaManipulation.Scale);
            var rotationChange = e.DeltaManipulation.Rotation;

            return new MapsuiManipulation(center, previousCenter, scaleFactor, rotationChange, e.CumulativeManipulation.Rotation);
        }

        private static double GetScaleFactor(Vector scale)
        {
            var deltaScale = (scale.X + scale.Y) / 2;
            if (Math.Abs(deltaScale) < Constants.Epsilon)
                return 1; // If there is no scaling the deltaScale will be 0.0 in Windows Phone (while it is 1.0 in wpf)
            if (!(Math.Abs(deltaScale - 1d) > Constants.Epsilon)) return 1;
            return deltaScale;
        }

        private void OnManipulationCompleted(object? sender, ManipulationCompletedEventArgs e) => Refresh();

        private void SKElementOnPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
            => _renderController?.Render(args.Surface.Canvas, GetPixelDensity());

        //> ADD
        private void SKGLElementOnPaintSurface(object sender, SKPaintGLSurfaceEventArgs args) //  SKPaintSurfaceEventArgs
            => _renderController?.Render(args.Surface.Canvas, GetPixelDensity());
        //< ADD

        public float? GetPixelDensity()
        {
            if (PresentationSource.FromVisual(this) is not PresentationSource presentationSource)
                return null;
            if (presentationSource.CompositionTarget is not CompositionTarget compositionTarget)
                return null;

            var matrix = compositionTarget.TransformToDevice;

            var dpiX = matrix.M11;
            var dpiY = matrix.M22;

            if (dpiX != dpiY) throw new ArgumentException();

            return (float?)dpiX;
        }

        protected virtual void Dispose(bool disposing)
        {
            SharedDispose(disposing);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static bool GetShiftPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        private static bool IsHovering(MouseEventArgs e)
        {
            return e.RightButton != MouseButtonState.Pressed;
        }
    }

}
