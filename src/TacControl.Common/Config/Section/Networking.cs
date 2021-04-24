using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace TacControl.Common.Config.Section
{
    public class Networking : ConfigSectionBase
    {
        [ConfigEntry]
        public ObservableCollection<TacControlEndpoint> DirectEndpoints { get; set; } = new ObservableCollection<TacControlEndpoint>();



        public Networking()
        {
            DirectEndpoints.CollectionChanged +=
                (x, y) =>
                    OnPropertyChanged(nameof(DirectEndpoints)
                );
        }
    }
}
