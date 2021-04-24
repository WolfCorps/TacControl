using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TacControl.Common;
using TacControl.Common.Config;

namespace TacControl.MediterranianWidgets
{
    /// <summary>
    /// Interaction logic for NetworkConnectWidget.xaml
    /// </summary>
    public partial class NetworkConnectWidget : UserControl
    {
        public Networking networking { get; } = Networking.Instance;

        private TacControlEndpoint CurrentEndpoint { get; set; } = null;

        public NetworkConnectWidget()
        {
            InitializeComponent();
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            networking.Connect(CurrentEndpoint);
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentEndpoint = e.AddedItems[0] as TacControlEndpoint;
            ConnectButton.IsEnabled = true;
        }

        private void OnConnectDirectClick(object sender, RoutedEventArgs e)
        {


        }

        private void OnAddServerClick(object sender, RoutedEventArgs e)
        {
            var hostName = DirectIPTextBox.Text;
            DirectIPTextBox.Text = "";

            try
            {
                var host = System.Net.Dns.GetHostEntry(hostName);
                var addr = host.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                if (addr == null) return;

                //#TODO let user provide port with :
                //#TODO only add if doesn't exist yet
                var newEndpoint = new TacControlEndpoint { Address = new IPEndPoint(addr, 8082), ClientID = hostName, LastActvity = DateTime.Now };
                networking.AvailableEndpoints.Add(newEndpoint);
            }
            catch (System.Net.Sockets.SocketException)
            {
                // throws exception if "could not resolve host"
                return;
            }
            

        }
    }
}
