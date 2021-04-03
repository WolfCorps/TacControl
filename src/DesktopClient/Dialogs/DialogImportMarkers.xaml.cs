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
    public partial class DialogImportMarkers : Window
    {
        public ModuleMarker MarkerModule { get; set; } = new ModuleMarker();

        public ObservableCollection<CheckableMarkerListItem> MarkerList { get; set; } = new ObservableCollection<CheckableMarkerListItem>();

        private MarkerVisibilityManager _visibilityManager = new MarkerVisibilityManager();

        public DialogImportMarkers()
        {
            InitializeComponent();

            PreviewMap.VisibilityManager = _visibilityManager;


            // Load saved markers file


            var openFileDialog = new OpenFileDialog() { AddExtension = true, DefaultExt = "txt", FileName = "MarkerExport.txt" };
            if (openFileDialog.ShowDialog() == true)
            {
                using (var sw = new StreamReader(openFileDialog.FileName))
                using (var writer = new JsonTextReader(sw))
                {
                    MarkerModule.DeserializeMarkers(writer);
                }
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

        private void OnDoImport(object sender, RoutedEventArgs e)
        {
            var toRemove = MarkerModule.markers.Where(x => !_visibilityManager.IsVisible(x.Value) || GameState.Instance.marker.markers.ContainsKey(x.Key))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                MarkerModule.markers.Remove(key);
            }

            foreach (var keyValuePair in MarkerModule.markers)
            {
                GameState.Instance.marker.CreateMarker(keyValuePair.Value);
            }
            
            Close();
        }
    }
}
