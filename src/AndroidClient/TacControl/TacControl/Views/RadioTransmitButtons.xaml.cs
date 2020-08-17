using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TacControl.Common;
using TacControl.Common.Modules;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TacControl.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RadioTransmitButtons : ContentView, INotifyPropertyChanged, IDisposable
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


        public static readonly BindableProperty RadioRefProperty =
            BindableProperty.Create(nameof(RadioRef), typeof(TFARRadio), typeof(RadioTransmitButtons), null, BindingMode.OneWay, null, OnRadioPropertyChanged);


        private static void OnRadioPropertyChanged(BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is RadioTransmitButtons self)
            {
                if (((TFARRadio)oldValue) != null) ((TFARRadio)oldValue).PropertyChanged -= self.RadioPropChanged;

                if (newValue != null) ((TFARRadio)newValue).PropertyChanged += self.RadioPropChanged;
            }
        
        }


        public int Channel
        {
            get => (int)GetValue(ChannelProperty);
            set => SetValue(ChannelProperty, value);
        }

        public static readonly BindableProperty ChannelProperty =
            BindableProperty.Create(nameof(Channel), typeof(int), typeof(RadioTransmitButtons), null);

        public bool IsTransmitting => RadioRef != null && RadioRef.tx == Channel;

        public bool IsLatched => IsTransmitting && WantLatch;

        public Xamarin.Forms.Color LatchColor => IsLatched && IsTransmitting ? Xamarin.Forms.Color.Orange : Xamarin.Forms.Color.DarkGoldenrod;
        public Xamarin.Forms.Color TXColor => IsTransmitting ? Xamarin.Forms.Color.LimeGreen : Xamarin.Forms.Color.DarkGreen;
        public Xamarin.Forms.Color StopColor => IsTransmitting ? Xamarin.Forms.Color.Red : Xamarin.Forms.Color.DarkRed;

        //#TODO propertyChanged EH from RadioRef, if radioRef LOOSES transmit while we are latched, unlatch
        private bool WantLatch { get; set; } = false;

        public RadioTransmitButtons()
        {

            //BindingContext = this;
            InitializeComponent();
        }

        public void Dispose()
        {
            RadioRef.PropertyChanged -= RadioPropChanged;
            RadioRef = null;

        }

        private void TransmitStop_OnPressed(object sender, EventArgs e)
        {
            WantLatch = false;
            if (IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, false);
        }

        private void TransmitLatch_OnPressed(object sender, EventArgs e)
        {
            WantLatch = true;
            if (!IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, true);
        }

        private void TransmitSoft_OnPressed(object sender, EventArgs e)
        {
            WantLatch = false;
            if (!IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, true);
        }

        private void TransmitSoft_OnReleased(object sender, EventArgs e)
        {
            WantLatch = false;
            if (IsTransmitting)
                GameState.Instance.radio.RadioTransmit(RadioRef, Channel, false);
        }
    }
}
