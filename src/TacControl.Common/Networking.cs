using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
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
using Sentry;
using TacControl.Common.Annotations;
using TacControl.Common.Config;
using TacControl.Common.Modules;
using WebSocket4Net;
using DataReceivedEventArgs = WebSocket4Net.DataReceivedEventArgs;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

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
    public class TacControlEndpoint
    {
        public string ClientID { get; set; }
        public IPEndPoint Address { get; set; }

        [ConfigNoSerialize]
        public DateTime LastActvity { get; set; }
    }


    [ConfigureAwait(false)]
    public class Networking : INotifyPropertyChanged
    {
        public static Networking Instance { get; } = new Networking();

        public Func<Action, Task> MainThreadInvoke { get; set; }

        // Currently not able to take a Connect request as we are trying to connect somewhere else
        public bool Busy { get; set; } = false;
        public bool CanConnect => !Busy;


        public ObservableCollection<TacControlEndpoint> AvailableEndpoints { get; } = new ObservableCollection<TacControlEndpoint>();

        public delegate void OnConnectedHandler();

        public event OnConnectedHandler OnConnected;


        private WebSocket socket;
        /// <summary>
        /// Connect to a TacControl Server or Arma client
        /// </summary>
        /// <param name="IsServer"></param>
        /// <returns></returns>
        public async Task Connect(IPEndPoint targetEndpoint)
        {
            Busy = true;

            // connect to specific host
            SentrySdk.AddBreadcrumb($"Direct connecting to {targetEndpoint}");
            Console.WriteLine($"Networking: Direct connecting to {targetEndpoint}");

            socket = new WebSocket($"ws://{targetEndpoint}/", "", null, null, UserName); // UserAgent==UserName only for TacControl.Server
            
            //socket.Opened += new EventHandler(websocket_Opened);
            //socket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            //socket.Closed += new EventHandler(websocket_Closed);
            socket.MessageReceived += OnMessage;
            socket.Open();
            // Assuming specific host == TacControl.Server
            Busy = false;
        }


        public void Connect(TacControlEndpoint targetEndpoint)
        {
            SentrySdk.AddBreadcrumb($"Direct connecting to {targetEndpoint.Address}");
            Console.WriteLine($"Networking: Direct connecting to {targetEndpoint.Address}");
            Busy = true;

            socket = new WebSocket($"ws://{targetEndpoint.Address}/", "", null, new List<KeyValuePair<string,string>>{new KeyValuePair<string,string>("accept-encoding", "CBOR")}, UserName); // UserAgent==UserName only for TacControl.Server)

            //socket.Opened += new EventHandler(websocket_Opened);
            //socket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            //socket.Closed += new EventHandler(websocket_Closed);
            socket.MessageReceived += OnMessage;
            socket.DataReceived += OnBinaryMessage;
            socket.Open(); //#TODO Assert.True(await websocket.OpenAsync(), "Failed to connect");
            // Assuming specific host == TacControl.Server
            Busy = false;
        }

        public string UserName { get; set; }

        private UdpClient _udpClient;
        private Timer _beaconTimer;


        public void StartUDPSearch()
        {
            if (_udpClient != null)
                return;


            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // special handling for android, we need to listen on proper interface
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up);
                //var types = interfaces.Select(x => x.NetworkInterfaceType).ToArray();
                //var names = interfaces.Select(x => x.Name).ToArray();
                //var supports = interfaces.Select(x => x.Supports(NetworkInterfaceComponent.IPv4)).ToArray();


                var targetInterface = interfaces.FirstOrDefault(x =>
                    (x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || x.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    && x.Supports(NetworkInterfaceComponent.IPv4));

                if (targetInterface == null)
                    return;

                var targetAddress = targetInterface.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                if (targetAddress == null)
                    return;

                _udpClient = new UdpClient(new IPEndPoint(targetAddress.Address, 0));//(8082, AddressFamily.InterNetworkV6
            }
            else
            {

                _udpClient = new UdpClient(0);//(8082, AddressFamily.InterNetworkV6
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
            }
            _udpClient.EnableBroadcast = true;

            _beaconTimer = new Timer((x) =>
            {
                SendUDPBeacon();
            }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(5));

            _udpClient.ReceiveAsync().ContinueWith(UDPRecv, TaskScheduler.FromCurrentSynchronizationContext());

            foreach (var tacControlEndpoint in AppConfig.Instance.GetEntry<IEnumerable<TacControlEndpoint>>("Networking.DirectEndpoints"))
            {
                AvailableEndpoints.Add(tacControlEndpoint);
            }


        }

        private static IPEndPoint _beaconTarget = new IPEndPoint(IPAddress.Broadcast, 8082);

        public void SendUDPBeacon()
        {
            var RequestData = Encoding.ASCII.GetBytes("R");

            // For a device wwith multiple network adapters, a simple broadcast on _udpClient will only work on the network interface with the highest metric, not on others.
            //if (System.Environment.OSVersion.Platform == PlatformID.Unix) // android only has one device, so just let it do the old stuff
            //{
                _udpClient.Send(RequestData, RequestData.Length, _beaconTarget); //IPAddress.Broadcast IPAddress.Parse("10.0.0.10")
            //}
            //else
            //{
                var localPort = ((IPEndPoint)_udpClient.Client.LocalEndPoint).Port;

                // We have to specifically send a broadcast per network interface
                foreach (NetworkInterface adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Only select interfaces that are Ethernet type and support IPv4 (important to minimize waiting time)
                    if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet && adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                    if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                    if (!adapter.Supports(NetworkInterfaceComponent.IPv4)) continue;
                    try
                    {
                        IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                        foreach (var ua in adapterProperties.UnicastAddresses)
                        {
                            if (ua.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;

                            Socket bcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            bcSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                            IPEndPoint myLocalEndPoint = new IPEndPoint(ua.Address, localPort); // reusing our port, because we need reply to target correct port
                            bcSocket.Bind(myLocalEndPoint);
                            bcSocket.SendTo(RequestData, _beaconTarget);
                            bcSocket.Close();
                        }
                    }
                    catch { }
                }
            //}

            Console.WriteLine($"Networking: Sent broadcast Beacon");
        }


        public void StopUDPSearch()
        {
            //IPEndPoint end = _beaconTarget;
            //_udpClient.EndReceive(null, ref end);

            if (_beaconTimer != null)
            {
                _beaconTimer.Dispose();
                _beaconTimer = null;
            }

            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;
            }
            AvailableEndpoints.Clear();
        }

        private async void UDPRecv(Task<UdpReceiveResult> task)
        {
            if (_udpClient == null)
                return;
            Console.WriteLine($"Networking: Broadcast result from {task.Result.RemoteEndPoint}");

            var receivedData = Encoding.ASCII.GetString(task.Result.Buffer);

            if (receivedData.StartsWith("TC"))
            {
                var clientID = receivedData.Substring(2);

                var found = AvailableEndpoints.FirstOrDefault(x => x.ClientID == clientID);
                if (found != null)
                {
                    found.LastActvity = DateTime.Now;
                }
                else
                {
                    AvailableEndpoints.Add(new TacControlEndpoint{Address = task.Result.RemoteEndPoint, ClientID = clientID, LastActvity = DateTime.Now});
                }
            }

            // Special boilerplate so we don't get UnobservedTaskException
            _ = Task.Run(async () =>
            {
                try
                {
                    await _udpClient.ReceiveAsync().ContinueWith(UDPRecv, TaskScheduler.Current);
                }
                catch (System.ObjectDisposedException) { }
            });

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

        private async void OnMessage(JObject parsedMsg)
        {
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
                Console.WriteLine($"Networking: FullState received!");
                var jsonSerializer = new JsonSerializer();
                using (var reader = new JTokenReader(parsedMsg["data"].Value<JToken>()))
                {
                    await MainThreadInvoke((Action)delegate
                    {
                        jsonSerializer.Populate(reader, GameState.Instance);

                        // need to init later
                        GameState.Instance.gps.OnPropertyChanged(nameof(ModuleGPS.trackers));

                    }).ConfigureAwait(false);
                }

                OnConnected?.Invoke();
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

            //GameState.Instance.test();
            //GameState.Instance.radio.OnPropertyChanged("radios"); //#TODO remove
        }


        private async void OnMessage(Object _, MessageReceivedEventArgs args)
        {
            var msg = args.Message;
            if (msg == null)
            {
                return;
            }

            if (msg == "null") // Could happen if for example terrain SVG export fails, if it cannot read a file it'll return "null"
            {
                return;
            }
            
            try
            {
                JObject parsedMsg = JObject.Parse(msg);
                OnMessage(parsedMsg);
            }
            catch (Exception ex)
            {
                SentrySdk.AddBreadcrumb(msg);
                SentrySdk.CaptureException(ex);
                throw;
            }
        }


        private void OnBinaryMessage(object sender, DataReceivedEventArgs args)
        {
            var msg = args.Data;

            try
            {
                using (var stringReader = new MemoryStream(msg))
                using (var reader = new Newtonsoft.Json.Cbor.CborDataReader(stringReader))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var parsedMsg = serializer.Deserialize<JObject>(reader);
                    OnMessage(parsedMsg);
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }




        public async void SendMessage(string message)
        {
            Console.WriteLine($"Networking: MSG:\n{message}");

            socket?.Send(message);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
