using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using TacControl.Common.Maps;
using Point = Mapsui.Geometries.Point;

namespace TacControl.TinyWidgets
{
    /// <summary>
    /// Map Cursor information that follows cursor, information about markers and whatever is under cursor currently
    /// </summary>
    public partial class MapCursor : UserControl, INotifyPropertyChanged
    {
        public Mapsui.UI.MapInfo UnderCursor { get; set; }



        public string MarkerOwner { get; set; }
        public string GridCoordinates { get; set; }

        public MapCursor()
        {
            InitializeComponent();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private (uint, uint) WorldPosToGrid(Point worldPosition)
        {

            var Xgrid10KM = (uint)(worldPosition.X / 10000) % 10;
            var Xgrid1KM = (uint)(worldPosition.X / 1000) % 10;
            var Xgrid100m = (uint)(worldPosition.X / 100) % 10;
            var Xgrid10m = (uint)(worldPosition.X / 10) % 10;


            var Ygrid10KM = (uint)(worldPosition.Y / 10000) % 10;
            var Ygrid1KM = (uint)(worldPosition.Y / 1000) % 10;
            var Ygrid100m = (uint)(worldPosition.Y / 100) % 10;
            var Ygrid10m = (uint)(worldPosition.Y / 10) % 10;

            return (
                Xgrid10KM * 1000 + Xgrid1KM * 100 + Xgrid100m * 10 + Xgrid10m,
                Ygrid10KM * 1000 + Ygrid1KM * 100 + Ygrid100m * 10 + Ygrid10m
            );
        }


        // Called by Fody.PropertyChanged
        private void OnUnderCursorChanged()
        {
            MarkerOwner = "";

            var worldGrid = WorldPosToGrid(UnderCursor.WorldPosition);
            GridCoordinates = $"{worldGrid.Item1:0000} {worldGrid.Item2:0000}";


            if (UnderCursor.Feature != null)
            {
                if (UnderCursor.Feature is MarkerFeature marker)
                {
                    UInt64 dpid = marker.marker.GetDPID();
                    if (GameState.Instance.gameInfo.players.TryGetValue(dpid, out var markerOwnerName))
                        MarkerOwner = markerOwnerName;
                }
                if (UnderCursor.Feature is GPSTrackerFeature tracker)
                {
                    var speed = tracker.Tracker.GetSpeedInKMH();





                }



                //Debugger.Break();



            }


        }











    }
}
