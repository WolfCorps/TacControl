using System;
using System.Collections.Generic;
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
using TacControl.Common;
using TacControl.Common.Modules;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for NoteWidget.xaml
    /// </summary>
    public partial class NoteWidget : UserControl
    {
        public static ModuleGPS gpsRef { get; } = GameState.Instance.gps;

        public Note NoteRef
        {
            get => (Note)GetValue(NoteRefProperty);
            set => SetValue(NoteRefProperty, value);
        }


        public static readonly DependencyProperty NoteRefProperty = DependencyProperty.Register(nameof(NoteRef), typeof(Note), typeof(NoteWidget));

        public NoteWidget()
        {
            InitializeComponent();
        }

        private void BtnJumpToGPS_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NoteRef.gpsTracker)) return;

            var tracker = gpsRef.trackers[NoteRef.gpsTracker];
            EventSystem.CenterMapOn(new Mapsui.MPoint(tracker.pos[0], tracker.pos[1]));

        }


        private void JumpToGPS_Selected(object sender, SelectionChangedEventArgs e)
        {

            if (e.AddedItems[0] is KeyValuePair<string, GPSTracker> track)
                NoteRef.SetGPS(track.Value.id);
            JumpToGPSButton.IsOpen = false;
        }
    }
}
