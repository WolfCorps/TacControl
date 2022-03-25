using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TacControl.Common.Modules
{
    public class ModuleGameInfo : INotifyPropertyChanged
    {
        public string worldName { get; set; }
        // Local players Direct Play ID
        public string playerID { get; set; }

        // Player direct play ID and their name
        public ObservableDictionary<UInt64, string> players { get; set; } = new ObservableDictionary<UInt64, string>();



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
