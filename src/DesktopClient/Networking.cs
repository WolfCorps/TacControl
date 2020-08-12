using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marvin.JsonPatch;
using Marvin.JsonPatch.Converters;
using Marvin.JsonPatch.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TacControl.Annotations;
using TacControl.Properties;
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

    class DenseVectorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Vector3).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var arr = serializer.Deserialize<float[]>(reader);
            return new Vector3(arr[0], arr[1], arr[2]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var arr = new float[3];
            if (value is Vector3 vec)
            {
                arr[0] = vec.X;
                arr[1] = vec.Y;
                arr[2] = vec.Z;
            }
   
            serializer.Serialize(writer, arr);
        }
    }


    public class GPSTracker : INotifyPropertyChanged
    {
      

        public string id { get; set; }
        //[JsonConverter(typeof(DenseVectorConverter))]
        //public Vector3 pos { get; set; }
        //[JsonConverter(typeof(DenseVectorConverter))]
        //public Vector3 vel { get; set; }
        public string displayName { get; set; }

        public ObservableCollection<float> pos { get; set; } = new ObservableCollection<float> {};
        public ObservableCollection<float> vel { get; set; } = new ObservableCollection<float> {};

        GPSTracker()
        {
            pos.CollectionChanged += (a, e) =>
            {
                if (pos.Count > 3) //#TODO this is stupid, need Vector3 support
                    pos.RemoveAt(0);
                OnPropertyChanged("pos");
            };
            vel.CollectionChanged += (a, e) =>
            {
                if (vel.Count > 3)
                    vel.RemoveAt(0);
                OnPropertyChanged("vel");
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ModuleGPS : INotifyPropertyChanged
    {
        public ObservableDictionary<string, GPSTracker> trackers { get; set; } = new ObservableDictionary<string, GPSTracker>();


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImageDirectory
    {
        public static ImageDirectory Instance = new ImageDirectory();

        public interface IImage
        {

        }

        public class Bitmap : IImage
        {
            public System.Drawing.Bitmap bmp;
        }



        public Dictionary<string, IImage> imageCache = new Dictionary<string, IImage>(StringComparer.InvariantCultureIgnoreCase);


        private class ImageRequest
        {
            public string path;
            public TaskCompletionSource<IImage> completionSource;
        }


        private Dictionary<string, ImageRequest> pendingRequests = new Dictionary<string, ImageRequest>(StringComparer.InvariantCultureIgnoreCase);

        public Task<IImage> GetImage(string path)
        {
            lock (imageCache)
            {
                if (imageCache.ContainsKey(path))
                    return Task.FromResult(imageCache[path]);

                if (pendingRequests.ContainsKey(path))
                    return pendingRequests[path].completionSource.Task;

                var request = new ImageRequest { path = path, completionSource = new TaskCompletionSource<IImage>() };
                pendingRequests[path] = request;


                Networking.Instance.SendMessage(
                    $@"{{
                        ""cmd"": [""ImgDir"", ""RequestTexture""],
                        ""args"": {{
                            ""path"": ""{path.Replace("\\","\\\\")}""
                        }}
                    }}"
                );


                return request.completionSource.Task;
            }
        }

        public void OnNetworkMessage(IEnumerable<string> cmd, JObject args)
        {
            if (cmd.First() == "TextureFile")
            {
                var path = args["path"].Value<string>();
                var data = args["data"].Value<string>();

                ImageRequest request;

                lock (imageCache)
                {
                    if (!pendingRequests.ContainsKey(path)) return; //#TODO log/handle

                    request = pendingRequests[path];
                }

                var dataBytes = Convert.FromBase64String(data);
             
                int width = (int) Math.Sqrt(dataBytes.Length/4);
                var bmp = new Bitmap {bmp = new System.Drawing.Bitmap(width, width, PixelFormat.Format32bppArgb)};

                //ARGB -> BGRA
                for (int i = 0; i < dataBytes.Length; i+=4)
                {
                    var A = dataBytes[i];
                    var B = dataBytes[i + 1];
                    var G = dataBytes[i + 2];
                    var R = dataBytes[i + 3];

                    dataBytes[i] = B;
                    dataBytes[i+1] = G;
                    dataBytes[i+2] = R;
                    dataBytes[i+3] = A;
                }


                BitmapData bmpData = bmp.bmp.LockBits(new Rectangle(0, 0,
                        bmp.bmp.Width,
                        bmp.bmp.Height),
                    ImageLockMode.WriteOnly,
                    bmp.bmp.PixelFormat);

                IntPtr pNative = bmpData.Scan0;
                Marshal.Copy(dataBytes, 0, pNative, dataBytes.Length);

                //var output = new FileStream("P:/test2", FileMode.CreateNew);
                //output.Write(dataBytes, 0, dataBytes.Length);
                //output.Close();


                bmp.bmp.UnlockBits(bmpData);

                request.completionSource.SetResult(bmp);

                lock (imageCache)
                {
                    imageCache[path] = bmp;
                    pendingRequests.Remove(path);
                }
            }
        }

    }


    struct MarkerType
    {
        std::string name;
        std::string color;
        uint32_t size;
        bool shadow;
        std::string icon;

        void Serialize(JsonArchive& ar);
    };

    std::map<std::string, MarkerType, std::less<>> markerTypes;

    struct MarkerColor
    {
        std::string name;
        std::string color;

        void Serialize(JsonArchive& ar);
    };

    std::map<std::string, MarkerColor, std::less<>> markerColors;

    struct MarkerBrush
    {
        std::string name;
        std::string texture;
        bool drawBorder;

        void Serialize(JsonArchive& ar);
    };




    public class ModuleMarker : INotifyPropertyChanged
    {
        public class MarkerType
        {
            public string name { get; set; }
            public string color { get; set; }
            public UInt32 size { get; set; }
            public bool shadow { get; set; }
            public string icon { get; set; }
        }

        public class MarkerColor
        {
            public string name { get; set; }
            public string color { get; set; }
        }

        public class MarkerBrush
        {
            public string name { get; set; }
            public string texture { get; set; }
            public bool drawBorder { get; set; }
        }

        public ObservableDictionary<string, MarkerType> markerTypes { get; set; } = new ObservableDictionary<string, MarkerType>();
        public ObservableDictionary<string, MarkerColor> markerColors { get; set; } = new ObservableDictionary<string, MarkerColor>();
        public ObservableDictionary<string, MarkerBrush> markerBrushes { get; set; } = new ObservableDictionary<string, MarkerBrush>();


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class GameState : INotifyPropertyChanged
    {
        public static GameState Instance = new GameState();

        public ModuleRadio radio { get; set; } = new ModuleRadio();
        public ModuleGPS gps { get; set; } = new ModuleGPS();

        public ModuleMarker marker { get; set; } = new ModuleMarker();

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

    /*
    public class Vector3Adapter : IAdapter
    {
        //https://github.com/KevinDockx/JsonPatch/blob/adb1ee749f24db67fd3425700e10f46b3fffa590/src/Marvin.JsonPatch/Internal/ListAdapter.cs
        public bool TryAdd(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage) {


            errorMessage = null;
            return true;
        }

        public bool TryGet(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {

            if (!TryGetPositionInfo(segment, OperationType.Get, out var positionInfo, out errorMessage))
            {
                value = null;
                return false;
            }

            value = 0;

            errorMessage = null;
            return true;
        }

        public bool TryRemove(
            object target,
            string segment,
            IContractResolver contractResolver,
            out string errorMessage)
        {


            if (!TryGetPositionInfo(segment, OperationType.Remove, out var positionInfo, out errorMessage))
            {
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool TryReplace(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {

            if (!TryGetPositionInfo(segment, OperationType.Replace, out var positionInfo, out errorMessage))
            {
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool TryTest(
           object target,
           string segment,
           IContractResolver contractResolver,
           object value,
           out string errorMessage)
        {
            var vec = (Vector3)target;


            if (!TryGetPositionInfo(segment, OperationType.Replace, out var positionInfo, out errorMessage))
            {
                return false;
            }

           //if (!TryConvertValue(value, typeArgument, segment, out var convertedValue, out errorMessage))
           //{
           //    return false;
           //}
           //
           //var currentValue = vec[positionInfo.Index];
           //if (!JToken.DeepEquals(JsonConvert.SerializeObject(currentValue), JsonConvert.SerializeObject(convertedValue)))
           //{
           //    errorMessage ="FormatValueAtListPositionNotEqualToTestValue(currentValue, value, positionInfo.Index)";
           //    return false;
           //}
           //else
            {
                errorMessage = null;
                return true;
            }
        }

        public bool TryTraverse(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {
            value = null;

            errorMessage = null;
            return true;
        }
        
        private bool TryGetPositionInfo(
            string segment,
            OperationType operationType,
            out PositionInfo positionInfo,
            out string errorMessage)
        {
            if (segment == "-")
            {
                positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                errorMessage = null;
                return true;
            }

            var position = -1;
            if (int.TryParse(segment, out position))
            {
                if (position >= 0 && position < 3)
                {
                    positionInfo = new PositionInfo(PositionType.Index, position);
                    errorMessage = null;
                    return true;
                }
                // As per JSON Patch spec, for Add operation the index value representing the number of elements is valid,
                // where as for other operations like Remove, Replace, Move and Copy the target index MUST exist.
                else if (position == 3 && operationType == OperationType.Add)
                {
                    positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                    errorMessage = null;
                    return true;
                }
                else
                {
                    positionInfo = new PositionInfo(PositionType.OutOfBounds, position);
                    errorMessage = "Out of bounds";
                    return false;
                }
            }
            else
            {
                positionInfo = new PositionInfo(PositionType.Invalid, -1);
                errorMessage = "Invalid Index";
                return false;
            }
        }

        private struct PositionInfo
        {
            public PositionInfo(PositionType type, int index)
            {
                Type = type;
                Index = index;
            }

            public PositionType Type { get; }
            public int Index { get; }
        }

        private enum PositionType
        {
            Index, // valid index
            EndOfList, // '-'
            Invalid, // Ex: not an integer
            OutOfBounds
        }

        private enum OperationType
        {
            Add,
            Remove,
            Get,
            Replace
        }
    }



    public class JsonContractResolver : DefaultContractResolver
    {
        public override JsonContract ResolveContract(Type type)
        {
            if (type == typeof(Vector3))
            {
                return new Vector3Adapter();
            }



            return base.ResolveContract(type);
        }
    }

    */

    //#TODO ^ to use Vector3 and potentially more other types later with JsonPatch
    //Need to get https://github.com/KevinDockx/JsonPatch/blob/adb1ee749f24db67fd3425700e10f46b3fffa590/src/Marvin.JsonPatch/Internal/ObjectVisitor.cs#L48 to return custom adapter


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

            if (parsedMsg["cmd"].Type == JTokenType.Array)
            {
                var cmd = parsedMsg["cmd"].Value<JArray>().Select(x => x.Value<string>());
                
                var cmdFirst = cmd.First();
                if (cmdFirst == "ImgDir")
                {
                    ImageDirectory.Instance.OnNetworkMessage(cmd.Skip(1), parsedMsg["args"].Value<JObject>());
                }

                return;
            }


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
                //patchDoc.ContractResolver = new JsonContractResolver();
                patchDoc.ApplyTo(GameState.Instance);
            }

            GameState.Instance.test();
            GameState.Instance.radio.OnPropertyChanged("radios"); //#TODO remove
        }

        public async void SendMessage(string message)
        {
            socket.Send(message);
        }

    }
}
