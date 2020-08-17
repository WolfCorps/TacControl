// /*
// * Copyright (C) Dedmen Miller @ R4P3 - All Rights Reserved
// * Unauthorized copying of this file, via any medium is strictly prohibited
// * Proprietary and confidential
// * Written by Dedmen Miller <dedmen@dedmen.de>, 08 2016
// */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TacControl.Common.Modules
{
    public class TFARRadio : INotifyPropertyChanged
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public int currentChannel { get; set; }
        public int currentAltChannel { get; set; }
        public bool rx { get; set; }
        public int tx { get; set; }

        public ObservableCollection<string> channels { get; set; } = new ObservableCollection<string>();
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ModuleRadio : INotifyPropertyChanged
    {
        public ObservableCollection<TFARRadio> radios { get; set; } = new ObservableCollection<TFARRadio>();

        public void RadioTransmit(TFARRadio radioRef, int channel, bool isTransmitting)
        {
            Networking.Instance.SendMessage(

                $@"{{
                    ""cmd"": [""Radio"", ""Transmit""],
                    ""args"": {{
                        ""radioId"": ""{radioRef.id}"",
                        ""channel"": {channel},
                        ""tx"": {(isTransmitting ? "true" : "false")}
                    }}
                }}"
            );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
