using System;
using System.ComponentModel;
using System.Windows;
using OpenTK.Graphics.OpenGL;
using OpenTK.Wpf;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace TacControl.Misc
{
    // https://github.com/mono/SkiaSharp/issues/745 
    // https://github.com/enissimsek


    public struct SizeWithDpi
    {
        public static SizeWithDpi Empty = new SizeWithDpi(0, 0);

        public int Width;
        public int Height;
        public double DpiX;
        public double DpiY;

        public SizeWithDpi(int width, int height, double dpiX = 96.0, double dpiY = 96.0)
        {
            Width = width;
            Height = height;
            DpiX = dpiX;
            DpiY = dpiY;
        }

        public override bool Equals(object obj)
        {
            return obj is SizeWithDpi dpi &&
                   Width == dpi.Width &&
                   Height == dpi.Height &&
                   DpiX == dpi.DpiX &&
                   DpiY == dpi.DpiY;
        }

        public bool Equals(SizeWithDpi dpi)
        {
            return Width == dpi.Width &&
                   Height == dpi.Height &&
                   DpiX == dpi.DpiX &&
                   DpiY == dpi.DpiY;
        }
    }


    [DefaultEvent("PaintSurface")]
    [DefaultProperty("Name")]
    public class SKGLWpfControl : GLWpfControl, ISkiaCanvas
    {
        private const SKColorType colorType = SKColorType.Rgba8888;
        private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

        private bool designMode;
        private bool disposed;

        private GRContext grContext;
        private GRGlFramebufferInfo glInfo;
        private GRBackendRenderTarget renderTarget;
        private SKSurface surface;
        private SKCanvas canvas;

        private bool ignorePixelScaling;

        private SKSizeI lastSize;

        public SKGLWpfControl()
        {
            Initialize();
        }

        private void Initialize()
        {
            designMode = DesignerProperties.GetIsInDesignMode(this);

            var mainSettings = new GLWpfControlSettings { MajorVersion = 2, MinorVersion = 1 };

            this.Render += TkRender;

            this.Loaded += (s, e) =>
            {
                Window.GetWindow(this).Closing += (s1, e1) =>
                {
                    this.Dispose();
                };
            };

            Start(mainSettings);
        }

        [Bindable(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SKSize CanvasSize => lastSize;

        [Bindable(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GRContext GRContext => grContext;

        [Category("Appearance")]
        public event EventHandler<SKPaintGLSurfaceEventArgs> PaintSurfaceGL;

        // fake, for interface
        [Category("Appearance")]
        public event EventHandler<SKPaintSurfaceEventArgs> PaintSurface;

        public bool IgnorePixelScaling
        {
            get { return ignorePixelScaling; }
            set
            {
                ignorePixelScaling = value;
                InvalidateVisual();
            }
        }

        protected void TkRender(TimeSpan delta)
        {
            if (designMode || disposed)
                return;

            if (this.Visibility != Visibility.Visible)
                return;

            var size = CreateSize();
            if (size.Width <= 0 || size.Height <= 0)
                return;

            // create the contexts if not done already
            if (grContext == null)
            {
                var glInterface = GRGlInterface.Create();
                grContext = GRContext.CreateGl(glInterface);
            }

            // get the new surface size
            var newSize = new SKSizeI(size.Width, size.Height);

            // manage the drawing surface
            if (renderTarget == null || lastSize != newSize || !renderTarget.IsValid)
            {
                // create or update the dimensions
                lastSize = newSize;

                GL.GetInteger(GetPName.FramebufferBinding, out var framebuffer);
                GL.GetInteger(GetPName.StencilBits, out var stencil);
                GL.GetInteger(GetPName.Samples, out var samples);
                var maxSamples = grContext.GetMaxSurfaceSampleCount(colorType);
                if (samples > maxSamples)
                    samples = maxSamples;
                glInfo = new GRGlFramebufferInfo((uint)framebuffer, colorType.ToGlSizedFormat());

                // destroy the old surface
                surface?.Dispose();
                surface = null;
                canvas = null;

                // re-create the render target
                renderTarget?.Dispose();
                renderTarget = new GRBackendRenderTarget(newSize.Width, newSize.Height, samples, stencil, glInfo);
            }

            // create the surface
            if (surface == null)
            {
                surface = SKSurface.Create(grContext, renderTarget, surfaceOrigin, colorType);
                canvas = surface.Canvas;
            }

            using (new SKAutoCanvasRestore(canvas, true))
            {
                // start drawing
                OnPaintSurface(new SKPaintGLSurfaceEventArgs(surface, renderTarget, surfaceOrigin, colorType, glInfo));
            }

            // update the control
            canvas.Flush();
        }

        private SizeWithDpi CreateSize()
        {
            var w = ActualWidth;
            var h = ActualHeight;

            if (!IsPositive(w) || !IsPositive(h))
                return SizeWithDpi.Empty;

            if (IgnorePixelScaling)
                return new SizeWithDpi((int)w, (int)h);

            var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            return new SizeWithDpi((int)(w * m.M11), (int)(h * m.M22), 96.0 * m.M11, 96.0 * m.M22);

            bool IsPositive(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
            }
        }

        protected virtual void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            PaintSurfaceGL?.Invoke(this, e);
        }

        protected void Dispose()
        {
            // clean up
            canvas = null;
            surface?.Dispose();
            surface = null;
            renderTarget?.Dispose();
            renderTarget = null;
            grContext?.Dispose();
            grContext = null;

            disposed = true;
        }
    }
}
