using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Newtonsoft.Json;
using TacControl.Common.Annotations;

namespace TacControl.Common.Modules
{
    public class Note : INotifyPropertyChanged
    {
        public int id { get; set; }
        public string text { get; set; }
        public string gpsTracker { get; set; }
        public string radioFrequency { get; set; }

        public string GPSTrackerDisplayName => GameState.Instance.gps.trackers.ContainsKey(gpsTracker)
            ? GameState.Instance.gps.trackers[gpsTracker].displayName
            : "";

        public string Text
        {
            get => text;
            set
            {
                text = value; //Manually set text first to get rid of visual glitch
                SetText(value); //Then tell server to change it too
            }
        }


        public void SetText(string newText)
        {
            //#TODO send base64 encoded
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Note"", ""SetText""],
                    ""args"": {{
                        ""id"": {id},
                        ""text"": {JsonConvert.ToString(newText)}
                    }}
                }}"
            );
        }

        public void SetGPS(string newText)
        {
            //#TODO send base64 encoded
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Note"", ""SetGPS""],
                    ""args"": {{
                        ""id"": {id},
                        ""text"": {JsonConvert.ToString(newText)}
                    }}
                }}"
            );
        }

        public void SetRadio(string newText)
        {
            //#TODO send base64 encoded
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Note"", ""SetRadio""],
                    ""args"": {{
                        ""id"": {id},
                        ""text"": {JsonConvert.ToString(newText)}
                    }}
                }}"
            );
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class ModuleNote : INotifyPropertyChanged
    {
      

        public ObservableDictionary<string, Note> notes { get; set; } = new ObservableDictionary<string, Note>();

        public void CreateNewNote()
        {
            Networking.Instance.SendMessage(
                $@"{{
                    ""cmd"": [""Note"", ""Create""],
                    ""args"": {{
                        
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
