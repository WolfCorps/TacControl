using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using TacControl.Annotations;
using TacControl.Common;
using TacControl.Common.Modules;

namespace TacControl
{
    //https://www.wpf-tutorial.com/data-binding/value-conversion-with-ivalueconverter/


    //System.Windows.Media.ImageSource
    public class SKImageToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            return ((SKImage)value).ToWriteableBitmap();
        }
    
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    //System.Windows.Media.ImageSource
    public class MarkerTypeToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MarkerType type)
            {

                if (type.iconImage == null) return null;

                var image = type.iconImage;
                if (type.color != "Default" && type.Color != SKColors.White)
                {

                    image = image.ApplyImageFilter(
                        SkiaSharp.SKImageFilter.CreateColorFilter(SkiaSharp.SKColorFilter.CreateLighting(type.Color, new SKColor(0, 0, 0))),
                        new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                        new SkiaSharp.SKRectI(0, 0, image.Width, image.Height),
                        out var outSUbs,
                        out SKPoint outoffs
                    );
                }


                return image.ToWriteableBitmap();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    public class SKColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var color = ((SKColor)value).ToColor();
            return new SolidColorBrush(color);
        }
    
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Color col)
            {
                return col.ToSKColor();
            }
            if (value is SolidColorBrush br)
            {
                return br.Color.ToSKColor();
            }
            return SKColor.Empty;
        }
    }

    public class MarkerTypeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string typeName)
                return GameState.Instance.marker.markerTypes[typeName];
            if (value is MarkerType type)
                return GameState.Instance.marker.markerTypes.First(x => x.Value == type).Key; //#TODO store id in markerType

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value,targetType,parameter,culture);
        }
    }

    public class MarkerColorStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string typeName)
                return GameState.Instance.marker.markerColors[typeName];
            if (value is MarkerColor type)
                return GameState.Instance.marker.markerColors.First(x => x.Value == type).Key; //#TODO store id in markerType

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }


    public partial class MapCreateMarker : UserControl, INotifyPropertyChanged
    {
        public ActiveMarker MarkerRef { get; set; }
        
        public MapCreateMarker()
        {
            InitializeComponent();
           
            cmbColors.ItemsSource = GameState.Instance.marker.markerColors.Values;
            DirSlider.ApplyTemplate();
            var t1 = DirSlider.Template.FindName("PART_Track", DirSlider);
            thumb = (t1 as Track).Thumb;
            //Magic needed to get slider drag while clicking on one of the outer buttons
            (t1 as Track).DecreaseRepeatButton.AddHandler(
                RepeatButton.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(UIElement_OnMouseDown),
                true
            );
            (t1 as Track).IncreaseRepeatButton.AddHandler(
                RepeatButton.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(UIElement_OnMouseDown),
                true
            );


            // Create and initialize timer
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Tick += timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 50); // 50 millisecond interval

        }

        private Thumb thumb;

        System.Windows.Threading.DispatcherTimer _timer;
        //#TODO stuttery slider https://social.msdn.microsoft.com/Forums/en-US/ff4cc6dd-b280-4ed8-a3a3-eee2524fe33f/serious-bug-in-slider?forum=wpf




        private void AlphaSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            MarkerRef.dir = (float)Math.Round(DirSlider.Value); //#TODO its Direction, not alpha
            // Put your original Value Changed Code here.
        }


        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                args.RoutedEvent = MouseLeftButtonDownEvent;
                thumb.RaiseEvent(args);


                MouseEventArgs args2 = new MouseEventArgs(e.MouseDevice, e.Timestamp);
                args2.RoutedEvent = MouseEnterEvent;
                thumb.RaiseEvent(args);
            }
        }



        public void Init()
        {
            cmbTypes.ItemsSource = GameState.Instance.marker.markerTypes.Values.Where(x => x.iconImage != null);
        }

        public delegate void ChannelChanged(string oldId);

        public event ChannelChanged OnChannelChanged;


        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            GameState.Instance.marker.CreateMarker(MarkerRef);


            var parent = this.Parent;
            (parent as Popup).IsOpen = false;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent;
            (parent as Popup).IsOpen = false;
        }


        private void CmbColors_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

 
    }
}
