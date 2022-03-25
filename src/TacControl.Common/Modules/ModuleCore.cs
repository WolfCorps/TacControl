using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace TacControl.Common.Modules
{
    public class ModuleCore : INotifyPropertyChanged
    {
        public ObservableCollection<string> ActiveInterests { get; set; } = new ObservableCollection<string>();


        [NonSerialized]
        private Dictionary<string, int> internalInterests = new Dictionary<string, int>();

        public void UpdateInterest(string interest, bool interestedNow)
        {
            if (!internalInterests.ContainsKey(interest))
                internalInterests.Add(interest, 0);

            if (interestedNow)
            {
                var newCount = ++internalInterests[interest];
                if (newCount == 1)
                    Networking.Instance.SendMessage(
                        $@"{{
                        ""cmd"": [""Core"", ""Register""],
                        ""args"": {{
                            ""interests"": [{JsonConvert.ToString(interest)}]
                        }}
                    }}"
                );
            }
            else
            {
                var newCount = --internalInterests[interest];
                if (newCount == 0)
                    Networking.Instance.SendMessage(
                        $@"{{
                        ""cmd"": [""Core"", ""Unregister""],
                        ""args"": {{
                            ""interests"": [{JsonConvert.ToString(interest)}]
                        }}
                    }}"
                    );
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
