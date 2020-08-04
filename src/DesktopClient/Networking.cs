using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marvin.JsonPatch;
using Marvin.JsonPatch.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TacControl.Annotations;
using WebSocket4Net;

namespace TacControl
{
    //struct TFARRadio
    //{
    //
    //    struct RadioChannel
    //    {
    //        std::string frequency;
    //    };
    //
    //
    //    std::string id;
    //    std::string displayName;
    //    int8_t currentChannel;
    //    int8_t currentAltChannel;
    //    //#TODO can receive on multiple channels..
    //    bool rx; //receiving right now
    //    //-1 == not sending
    //    int8_t tx; //sending right now
    //    std::vector<RadioChannel> channels;
    //
    //    void Serialize(JsonArchive& ar);
    //};

    //public class RadioChannel
    //{
    //    public string frequency { get; set; }
    //}
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

        [NotifyPropertyChangedInvocator]
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

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }



    public class GameState : INotifyPropertyChanged
    {
        public static GameState Instance = new GameState();

        public ModuleRadio radio { get; set; } = new ModuleRadio();


        public void test()
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class Networking
    {
        public static Networking Instance = new Networking();

        private WebSocket socket;
        public async void Connect()
        {

            socket = new WebSocket("ws://localhost:8082/");
            //socket.Opened += new EventHandler(websocket_Opened);
            //socket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            //socket.Closed += new EventHandler(websocket_Closed);
            socket.MessageReceived += OnMessage;
            socket.Open();
        }


        public static T DeserializeObject<T>(JToken value)
        {


            JsonPatchDocument<GameState> patchDoc = new JsonPatchDocument<GameState>();
            var jsonSerializer = new JsonSerializer();
            using (var reader = new JTokenReader(value))
            {
                return (T)jsonSerializer.Deserialize(reader, typeof(T));
            }

        }

        private void OnMessage(Object _, MessageReceivedEventArgs args)
        {
            var msg = args.Message;
            JObject parsedMsg = JObject.Parse(msg);




            if (parsedMsg["cmd"].Value<string>() == "StateFull")
            {
                var jsonSerializer = new JsonSerializer();
                using (var reader = new JTokenReader(parsedMsg["data"].Value<JToken>()))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        jsonSerializer.Populate(reader, GameState.Instance);
                    });
                    
                }
            }


            if (parsedMsg["cmd"].Value<string>() == "StateUpdate")
            {

                var patchDoc = DeserializeObject<JsonPatchDocument<GameState>>(parsedMsg["data"].Value<JArray>());

                patchDoc.ApplyTo(GameState.Instance);
            }
            GameState.Instance.test();
            GameState.Instance.radio.OnPropertyChanged("radios");
        }

        public async void SendMessage(string message)
        {
            socket.Send(message);
        }

    }
}
