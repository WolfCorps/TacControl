using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using TacControl.Annotations;
using TacControl.Common;
using TacControl.Common.Modules;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for RadioTransmitButtons.xaml
    /// </summary>
    public partial class RadioTransmitButtons : UserControl, INotifyPropertyChanged, IDisposable
    {

        public TFARRadio RadioRef
        {
            get => (TFARRadio)GetValue(RadioRefProperty);
            set => SetValue(RadioRefProperty, value);
        }

        private void RadioPropChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "tx")
            {
                OnPropertyChanged(nameof(IsTransmitting));
                OnPropertyChanged(nameof(LatchColor));
                OnPropertyChanged(nameof(TXColor));
                OnPropertyChanged(nameof(StopColor));
            }
        }

        public static readonly DependencyProperty RadioRefProperty = DependencyProperty.Register(nameof(RadioRef), typeof(TFARRadio), typeof(RadioTransmitButtons),
            new FrameworkPropertyMetadata(null, OnRadioPropertyChanged)
            );

        private static void OnRadioPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadioTransmitButtons self)
            {
                if (((TFARRadio)e.OldValue) != null) ((TFARRadio)e.OldValue).PropertyChanged -= self.RadioPropChanged;

                ((TFARRadio)e.NewValue).PropertyChanged += self.RadioPropChanged;
            }
        }


        public int Channel
        {
            get => (int)GetValue(ChannelProperty);
            set => SetValue(ChannelProperty, value);
        }

        public static readonly DependencyProperty ChannelProperty = DependencyProperty.Register(nameof(Channel), typeof(int), typeof(RadioTransmitButtons),
            new FrameworkPropertyMetadata(-1, OnChannelPropertyChanged));

        private static void OnChannelPropertyChanged(DependencyObject bindable, DependencyPropertyChangedEventArgs e)
        {
            if (bindable is RadioTransmitButtons self)
            {
                self.OnPropertyChanged(nameof(TXColor));
                self.OnPropertyChanged(nameof(LatchColor));
                self.OnPropertyChanged(nameof(StopColor));
            }
        }


        public bool IsTransmitting => RadioRef != null && RadioRef.tx == Channel;

        public bool IsLatched => IsTransmitting && WantLatch;

        public Brush LatchColor => IsLatched && IsTransmitting ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.DarkGoldenrod);
        public Brush TXColor => IsTransmitting ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.DarkGreen);
        public Brush StopColor => IsTransmitting ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.DarkRed);
        
        //#TODO propertyChanged EH from RadioRef, if radioRef LOOSES transmit while we are latched, unlatch
        private bool WantLatch { get; set; } = false;

        public RadioTransmitButtons()
        {
            InitializeComponent();
        }

        private void TransmitSoft_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WantLatch = false;
            if (!IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, true);
        }

        private void TransmitSoft_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WantLatch = false;
            if (IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, false);
        }

        private void TransmitSoft_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsTransmitting && !WantLatch)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, false);
        }

        private void TransmitLatch_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            WantLatch = true;
            if (!IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, true);
        }

        private void TransmitStop_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            WantLatch = false;
            if (IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            RadioRef.PropertyChanged -= RadioPropChanged;
            RadioRef = null;

        }
    }
}
