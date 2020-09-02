using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using TacControl.Common.Modules;

namespace TacControl.BigWidgets
{
    /// <summary>
    /// Interaction logic for RadioSettings.xaml
    /// </summary>
    public partial class RadioSettings : UserControl
    {
        public RadioSettings()
        {
            InitializeComponent();
        }

        public TFARRadio RadioRef
        {
            get => (TFARRadio)GetValue(RadioRefProperty);
            set => SetValue(RadioRefProperty, value);
        }

        private void RadioPropChanged(object sender, PropertyChangedEventArgs e)
        {
            
        }

        public static readonly DependencyProperty RadioRefProperty = DependencyProperty.Register(nameof(RadioRef), typeof(TFARRadio), typeof(RadioSettings),
            new FrameworkPropertyMetadata(null, OnRadioPropertyChanged)
        );

        private static void OnRadioPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadioSettings self)
            {
                if (((TFARRadio)e.OldValue) != null) ((TFARRadio)e.OldValue).PropertyChanged -= self.RadioPropChanged;

                ((TFARRadio)e.NewValue).PropertyChanged += self.RadioPropChanged;
            }
        }

        //#TODO format filter on textbox
        private void Frequency_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            int channel = (int)(sender as FrameworkElement).Tag;
            string newFrequency = (sender as TextBox).Text;
            if (RadioRef.channels[channel] != newFrequency)
                RadioRef.SetChannelFrequency(channel, (sender as TextBox).Text);
        }

        private void Volume_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newVolume = (int)e.NewValue;
            if (newVolume != RadioRef.volume)
                RadioRef.SetVolume((int)e.NewValue);
        }

        private void StereoMain_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newStereo = (int)e.NewValue;
            if (newStereo != RadioRef.mainStereo)
                RadioRef.SetStereoMain((int)e.NewValue);
        }

        private void StereoAlt_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newStereo = (int)e.NewValue;
            if (newStereo != RadioRef.altStereo)
                RadioRef.SetSteroAlt((int)e.NewValue);
        }

        private void MainChannel_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newChannel = (int)e.NewValue;
            if (newChannel != RadioRef.currentChannel)
                RadioRef.SetMainChannel((int)e.NewValue);
        }

        private void AltChannel_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newChannel = (int) e.NewValue;
            if (newChannel != RadioRef.currentAltChannel)
                RadioRef.SetAltChannel((int)e.NewValue);
        }
    }
}
