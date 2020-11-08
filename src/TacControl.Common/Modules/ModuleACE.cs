using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace TacControl.Common.Modules
{

    public class Explosive
    {
        public string Id { get; set; }
        public UInt16 Code { get; set; }
        public string Detonator { get; set; }
    }


    public class ModuleACE : INotifyPropertyChanged
    {
        public ObservableCollection<Explosive> exp { get; set; } = new ObservableCollection<Explosive>();



        public void DetonateExplosive(string id)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""ACE"", ""Detonate""],
                    ""args"": {{
                        ""explosives"": [{JsonConvert.ToString(id)}]
                    }}
                }}"
            );
        }













        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
