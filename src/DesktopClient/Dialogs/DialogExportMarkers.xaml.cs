using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;
using TacControl.Annotations;
using TacControl.Common;
using TacControl.Common.Maps;
using TacControl.Common.Modules;

namespace TacControl.Dialogs
{
    public class CheckableMarkerListItem: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<CheckableMarkerListItem> Children { get; set; } = new ObservableCollection<CheckableMarkerListItem>();

        private bool _isChecked = true;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
                foreach (var child in Children)
                {
                    child.IsChecked = value;
                }
            }
        }
        public ActiveMarker Marker { get; set; }
        public string Value { get; set; }
    }


    public class MarkerVisibilityManager : IMarkerVisibilityManager
    {
        private HashSet<ActiveMarker> HiddenMarkers = new HashSet<ActiveMarker>();
        public MarkerVisibilityManager()
        {

        }

        public bool IsVisible(ActiveMarker marker)
        {
            if (HiddenMarkers.Contains(marker)) return false;

            return true;
        }

        public Action OnUpdated { get; set; }


        public void HideMarker(ActiveMarker marker)
        {
            HiddenMarkers.Add(marker);
            OnUpdated?.Invoke();
        }

        public void ShowMarker(ActiveMarker marker)
        {
            HiddenMarkers.Remove(marker);
            OnUpdated?.Invoke();
        }

    }

    public partial class DialogExportMarkers : Window
    {
        public ModuleMarker MarkerModule { get; set; } = new ModuleMarker();

        public ObservableCollection<CheckableMarkerListItem> MarkerList { get; set; } = new ObservableCollection<CheckableMarkerListItem>();

        private MarkerVisibilityManager _visibilityManager = new MarkerVisibilityManager();

        public DialogExportMarkers()
        {
            InitializeComponent();

            PreviewMap.VisibilityManager = _visibilityManager;
            foreach (var keyValuePair in GameState.Instance.marker.markers)
            {
                MarkerModule.markers.Add(keyValuePair.Key, keyValuePair.Value);
            }

            var xList = MarkerModule.markers.Values.GroupBy(x => x.channel).Select(grp => grp.ToList()).ToList();


            xList.ForEach(x =>
            {
                if (x.Count == 0) return;

                var channel = (MarkerChannel) x.First().channel;

                MarkerList.Add(new CheckableMarkerListItem
                {
                    Value = channel.ToString(),
                    Children = new ObservableCollection<CheckableMarkerListItem>(x.Select(y =>
                    {
                        var item = new CheckableMarkerListItem { Marker = y, Value = $"{y.text} ({y.id})" };
                        item.PropertyChanged += MarkerItemChanged;

                        return item;
                    }))
                });

            });





        }

        private void MarkerItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var markerItem = sender as CheckableMarkerListItem;
            if (markerItem.Marker == null) return;

            if (markerItem.IsChecked)
                _visibilityManager.ShowMarker(markerItem.Marker);
            else
                _visibilityManager.HideMarker(markerItem.Marker);
        }

        private void OnDoExport(object sender, RoutedEventArgs e)
        {
            var toRemove = MarkerModule.markers.Where(x => !_visibilityManager.IsVisible(x.Value))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                MarkerModule.markers.Remove(key);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog() {AddExtension = true, DefaultExt = "txt", FileName = "MarkerExport.txt"};
            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    MarkerModule.SerializeMarkers(writer);
                }
            }

            Close();
        }
    }
}
