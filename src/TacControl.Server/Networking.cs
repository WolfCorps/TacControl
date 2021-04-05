using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Marvin.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace TacControl.Server
{
    public class Client
    {
        private JObject clientState;
        private System.Net.IPEndPoint myEndPoint;
        private readonly TcpClient _client;
        private System.Net.WebSockets.WebSocket socket;
        private Memory<byte> buffer = new(new byte[1024*1024]);

        public Client(TcpClient client, System.Net.WebSockets.WebSocket socket)
        {
            this.myEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            _client = client;
            this.socket = socket;

            socket.ReceiveAsync(buffer, CancellationToken.None).AsTask().ContinueWith(x => OnDataReceived(x.Result));
        }

        private void OnDataReceived(ValueWebSocketReceiveResult xResult)
        {
            try
            {
                using (var sReader = new System.IO.StringReader(Encoding.UTF8.GetString(buffer.Span)))
                using (var jReader = new JsonTextReader(sReader))
                {
                    var data = JObject.ReadFrom(jReader);
                    Networking.Instance.OnMsgReceived(this, data);
                }
            }
            catch( Exception ex)
            {
                Debugger.Break();
            }

            socket.ReceiveAsync(buffer, CancellationToken.None).AsTask().ContinueWith(x => OnDataReceived(x.Result));
        }

        public async Task SendFullState(JObject state)
        {
            JObject stateUpdate = new JObject();
            stateUpdate["cmd"] = "StateFull";
            stateUpdate["data"] = state;

            // special hacky workaround for sending GameInfo playerid
            state["GameInfo"]["playerID"] = (ulong)myEndPoint.GetHashCode();

            clientState = state.DeepClone() as JObject;

            var bytes = Encoding.UTF8.GetBytes(stateUpdate.ToString());

            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }



        public void UpdateState(JObject newState)
        {
            var oldState = clientState;
            clientState = newState.DeepClone() as JObject;

            // special hacky workaround for sending GameInfo playerid
            clientState["GameInfo"]["playerID"] = (ulong)myEndPoint.GetHashCode();


            JObject stateUpdate = new JObject();
            stateUpdate["cmd"] = "StateUpdate";

            var patch = new JsonPatchDocument();
            FillPatchForObject(oldState, clientState, patch, "/");
            patch.Operations.ForEach(x => x.path = x.path.Replace("~2", " ")); // Need to cheat because Marvin.JsonPatch had invalid path checking
            var patchobj = JToken.FromObject(patch);
            stateUpdate["data"] = patchobj;

            var str = stateUpdate.ToString();
            var bytes = Encoding.UTF8.GetBytes(str);

            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            //Microsoft.AspNetCore.JsonPatch

            if (!_client.Client.Connected)
            {
                Networking.Instance.OnClientDisconnected(this);
            }

           


        }

        static void FillPatchForObject(JObject orig, JObject mod, JsonPatchDocument patch, string path)
        {
            var origNames = orig.Properties().Select(x => x.Name).ToArray();
            var modNames = mod.Properties().Select(x => x.Name).ToArray();

            // Names removed in modified
            foreach (var k in origNames.Except(modNames))
            {
                var prop = orig.Property(k);
                patch.Remove(path + prop.Name
                        .Replace("~", "~0").Replace("/", "~1")
                        .Replace(" ", "~2") // Need to cheat because Marvin.JsonPatch had invalid path checking
                );
            }

            // Names added in modified
            foreach (var k in modNames.Except(origNames))
            {
                var prop = mod.Property(k);

                patch.Add(path + prop.Name
                    .Replace("~", "~0").Replace("/", "~1")
                    .Replace(" ", "~2") // Need to cheat because Marvin.JsonPatch had invalid path checking


                    , prop.Value);
            }

            // Present in both
            foreach (var k in origNames.Intersect(modNames))
            {
                var origProp = orig.Property(k);
                var modProp = mod.Property(k);

                if (origProp.Value.Type != modProp.Value.Type)
                {
                    patch.Replace(path + modProp.Name
                            .Replace("~", "~0").Replace("/", "~1")
                            .Replace(" ", "~2") // Need to cheat because Marvin.JsonPatch had invalid path checking
                        , modProp.Value);
                }
                else if (!string.Equals(
                    origProp.Value.ToString(Newtonsoft.Json.Formatting.None),
                    modProp.Value.ToString(Newtonsoft.Json.Formatting.None)))
                {
                    if (origProp.Value.Type == JTokenType.Object)
                    {
                        // Recurse into objects
                        FillPatchForObject(origProp.Value as JObject, modProp.Value as JObject, patch, path + modProp.Name
                                .Replace("~", "~0").Replace("/", "~1")
                                .Replace(" ", "~2") // Need to cheat because Marvin.JsonPatch had invalid path checking
                            + "/");
                    }
                    else
                    {
                        // Replace values directly
                        patch.Replace(path + modProp.Name
                                .Replace("~", "~0").Replace("/", "~1")
                                .Replace(" ", "~2") // Need to cheat because Marvin.JsonPatch had invalid path checking
                            , modProp.Value);
                    }
                }
            }
        }



        public void SendData(string message)
        {

            var bytes = Encoding.UTF8.GetBytes(message);

            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }




    public class Networking
    {
        public static Networking Instance = new();
        private TcpListener _socket;
        private UdpClient _udpSocket;

        // UDP Server listening on 8082 waits for broadcasts of clients requesting a server
        // TCP Websocket Server on 8082 handling client communication
        private List<Client> _clients = new();
        private JObject _lastState;


        Networking()
        {
            _socket = new TcpListener(IPAddress.Any, 8082);
            _udpSocket = new UdpClient(8082);
            _socket.Start();
            _socket.AcceptTcpClientAsync().ContinueWith(x => OnTCPConnection(x.Result));



                


            _udpSocket.ReceiveAsync().ContinueWith(x => OnUDPDatagram(x.Result));
        }

        private void OnTCPConnection(TcpClient client)
        {
            var stream = client.GetStream();
            while (!stream.DataAvailable) ;
            while (client.Available < 3) ; // match against "get"

            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, client.Available);
            string s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);


                string username = Regex.Match(s, "User-Agent: (.*)").Groups[1].Value.Trim();
                GameState.Instance.gameInfo.OnPlayerJoined((ulong)client.Client.RemoteEndPoint.GetHashCode(), username);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);

            }

            var newClient = new Client(client,
                System.Net.WebSockets.WebSocket.CreateFromStream(client.GetStream(), true, "HTTP",
                    TimeSpan.FromSeconds(10)));
            _clients.Add(newClient);
            _socket.AcceptTcpClientAsync().ContinueWith(x => OnTCPConnection(x.Result));
            newClient.SendFullState(_lastState);
        }

        private void OnUDPDatagram(UdpReceiveResult obj)
        {
            _udpSocket.Send(Encoding.ASCII.GetBytes("y"), 1, obj.RemoteEndPoint);
            _udpSocket.ReceiveAsync().ContinueWith(x => OnUDPDatagram(x.Result));
        }

        private void Run()
        {
     



        }

        public void Start()
        {


        }

        public void UpdateState(JObject newState)
        {
            _lastState = newState;
            foreach (var client in _clients)
            {
                client.UpdateState(newState);
            }
        }

        public void OnMsgReceived(Client cli, JToken data)
        {
            var cmd = data["cmd"].Value<JArray>().Select(x => x.Value<string>());
            GameState.Instance.TransferNetworkMessage(cmd, data["args"] as JObject, (string message) =>
            {
                cli.SendData(message);
            });
           

        }

        public void OnClientDisconnected(Client client)
        {
            //#TODO fix this, will crash in UpdateState due to modifying collection while iterating
            //_clients.Remove(client);
        }
    }
}
