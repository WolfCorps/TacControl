using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fody;
using Marvin.JsonPatch;
using Marvin.JsonPatch.Converters;
using Marvin.JsonPatch.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TacControl.Common.Modules;
using WebSocket4Net;

namespace TacControl.Common.Modules
{
}

namespace TacControl.Common
{


    //class DenseVectorConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return typeof(Vector3).IsAssignableFrom(objectType);
    //    }
    //
    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        var arr = serializer.Deserialize<float[]>(reader);
    //        return new Vector3(arr[0], arr[1], arr[2]);
    //    }
    //
    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        var arr = new float[3];
    //        if (value is Vector3 vec)
    //        {
    //            arr[0] = vec.X;
    //            arr[1] = vec.Y;
    //            arr[2] = vec.Z;
    //        }
    //
    //        serializer.Serialize(writer, arr);
    //    }
    //}


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

    [ConfigureAwait(false)]
    public class Networking
    {
        public static Networking Instance = new Networking();

        public Func<Action, Task> MainThreadInvoke { get; set; }


        private WebSocket socket;
        public async Task Connect()
        {



            var Client = new UdpClient(8082, AddressFamily.InterNetworkV6);
            Client.EnableBroadcast = true;


            //var m_GrpAddr = IPAddress.Parse("FF01::1");

            // Use the overloaded JoinMulticastGroup method.
            // Refer to the ClientOriginator method to see how to use the other
            // methods.
            //Client.JoinMulticastGroup(m_GrpAddr);

 

            //Client.JoinMulticastGroup();
            var RequestData = Encoding.ASCII.GetBytes("R");

            Client.EnableBroadcast = true;
            Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Parse("10.0.1.1"), 8082)); //IPAddress.Broadcast


            //ConfigureAwait(false) is needed on android
            var ServerResponseData = await Client.ReceiveAsync().ConfigureAwait(false);

            //var ServerResponse = Encoding.ASCII.GetString(ServerResponseData.Buffer);
            //Console.WriteLine("Recived {0} from {1}", ServerResponse, ServerResponseData.RemoteEndPoint);

            Client.Close();


            socket = new WebSocket($"ws://{ServerResponseData.RemoteEndPoint}/");
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

        private async void OnMessage(Object _, MessageReceivedEventArgs args)
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
                    await MainThreadInvoke((Action)delegate
                    {
                        jsonSerializer.Populate(reader, GameState.Instance);
                    }).ConfigureAwait(false);
                    
                }
            }


            if (parsedMsg["cmd"].Value<string>() == "StateUpdate")
            {

                var patchDoc = DeserializeObject<JsonPatchDocument<GameState>>(parsedMsg["data"].Value<JArray>());
                //patchDoc.ContractResolver = new JsonContractResolver();
                await MainThreadInvoke((Action)delegate
                {
                    patchDoc.ApplyTo(GameState.Instance);
                }).ConfigureAwait(false);
                
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
