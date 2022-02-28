using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

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

        //#TODO use enum for stereomode
        public int mainStereo { get; set; } // 0 center, 1 left, 2 right
        public int altStereo { get; set; } // 0 center, 1 left, 2 right
        public int volume { get; set; } //0-10
        public bool speaker { get; set; }



        [JsonIgnore]
        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = value; //Manually set text first to get rid of visual glitch
                SetDisplayName(value); //Then tell server to change it too
            }
        }

        [JsonIgnore]
        public bool HasAltChannel => currentAltChannel != -1;


        [JsonIgnore]
        public int CurrentChannel => currentChannel + 1;
        [JsonIgnore]
        public int CurrentAltChannel => currentAltChannel + 1;

        [JsonIgnore]
        public string CurrentMainFreq => currentChannel != -1 ? channels[currentChannel] : "";
        [JsonIgnore]
        public string CurrentAltFreq => currentAltChannel != -1 ? channels[currentAltChannel] : "";

        public ObservableCollection<string> channels { get; set; } = new ObservableCollection<string>();
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(channels))
            {
                OnPropertyChanged(nameof(CurrentMainFreq));
                OnPropertyChanged(nameof(CurrentAltFreq));
            }

        }


        TFARRadio()
        {
            channels.CollectionChanged += (a, b) => OnPropertyChanged(nameof(channels));
        }






        public void SetChannelFrequency(int channel, string text)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetFrequency""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""channel"": {channel},
                        ""freq"": {JsonConvert.ToString(text)}
                    }}
                }}"
            );
        }

        public void SetVolume(int eNewValue)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetVolume""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""volume"": {eNewValue}
                    }}
                }}"
            );
        }

        public void SetStereoMain(int eNewValue)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetStereo""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""main"": true,
                        ""mode"": {eNewValue}
                    }}
                }}"
            );
        }

        public void SetSteroAlt(int eNewValue)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetStereo""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""main"": false,
                        ""mode"": {eNewValue}
                    }}
                }}"
            );
        }

        public void SetSpeaker(bool enabled)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetSpeaker""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""enabled"": {enabled}
                    }}
                }}"
            );
        }

        public void SetMainChannel(int chan)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetChannel""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""main"": true,
                        ""channel"": {chan}
                    }}
                }}"
            );
        }

        public void SetAltChannel(int chan)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetChannel""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""main"": false,
                        ""channel"": {chan}
                    }}
                }}"
            );
        }

        public void SetDisplayName(string newName)
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Radio"", ""SetDisplayName""],
                    ""args"": {{
                        ""radioId"": ""{id}"",
                        ""name"": {JsonConvert.ToString(newName)}
                    }}
                }}"
            );
        }

    }

    public class ModuleRadio : INotifyPropertyChanged
    {
        public ObservableCollection<TFARRadio> radios { get; set; } = new ObservableCollection<TFARRadio>();

        public void RadioTransmit(TFARRadio radioRef, int channel, bool isTransmitting)
        {
            if (isTransmitting)
            {
                //Stop all other currently transmitting radios, because that wouldn't work.

                foreach (var tfarRadio in radios)
                {
                        if (tfarRadio.tx != -1)
                            RadioTransmit(tfarRadio, tfarRadio.tx, false);
                }
            }




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
