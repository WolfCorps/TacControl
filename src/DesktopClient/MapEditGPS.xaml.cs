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
using TacControl.Annotations;
using TacControl.Common;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for MapEditGPS.xaml
    /// </summary>
    public partial class MapEditGPS : UserControl, INotifyPropertyChanged
    {
        public MapEditGPS()
        {
            InitializeComponent();
        }

        public string DescriptionText
        {
            get;
            set;
        }

        public GPSTracker Tracker { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(Tracker))
            {
                DescriptionText = Tracker.displayName;
            }
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            GameState.Instance.gps.SetTrackerName(Tracker, DescriptionText);


            var parent = this.Parent;
            (parent as Popup).IsOpen = false;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent;
            (parent as Popup).IsOpen = false;
        }
    }
}
