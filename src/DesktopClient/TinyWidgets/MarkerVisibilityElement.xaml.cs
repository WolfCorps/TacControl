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

namespace TacControl.TinyWidgets
{
    public partial class MarkerVisibilityElement : UserControl, INotifyPropertyChanged
    {
        public MarkerVisibilityElement()
        {
            InitializeComponent();
        }

        public Action OnSoloChanged { get; set; }
        public Action OnIgnoreChanged { get; set; }

        public string Title { get; set; }
        public bool IsSolo { get; set; }
        public bool IsIgnore { get; set; }

        public Brush SoloColor => IsSolo ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.DarkGreen);
        public Brush IgnoreColor => IsIgnore ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.DarkRed);


        private void SoloButton_OnClick(object sender, RoutedEventArgs e)
        {
            IsSolo = !IsSolo;
            OnSoloChanged?.Invoke();
            OnPropertyChanged(nameof(SoloColor));
            if (IsSolo && IsIgnore)
            {
                IsIgnore = false;
                OnIgnoreChanged?.Invoke();
                OnPropertyChanged(nameof(IgnoreColor));
            }
        }

        private void IgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            IsIgnore = !IsIgnore;
            OnIgnoreChanged?.Invoke();
            OnPropertyChanged(nameof(IgnoreColor));

            if (IsSolo && IsIgnore)
            {
                IsSolo = false;
                OnSoloChanged?.Invoke();
                OnPropertyChanged(nameof(SoloColor));
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
