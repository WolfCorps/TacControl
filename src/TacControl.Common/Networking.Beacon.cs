using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TacControl.Common.Config;
using System.Collections.ObjectModel;

namespace TacControl.Common
{
    public partial class Networking
    {
        public ObservableCollection<TacControlEndpoint> AvailableEndpoints { get; } = new ObservableCollection<TacControlEndpoint>();

        private UdpClient _udpClient;
        private Timer _beaconTimer;

        private SynchronizationContext _syncContext;
        public void StartUDPSearch()
        {
            if (_udpClient != null)
                return;
            _syncContext = SynchronizationContext.Current;

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
                    _syncContext.Post(o =>
                    {
                        AvailableEndpoints.Add(new TacControlEndpoint { Address = task.Result.RemoteEndPoint, ClientID = clientID, LastActvity = DateTime.Now });
                    }, null);
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



    }
}
