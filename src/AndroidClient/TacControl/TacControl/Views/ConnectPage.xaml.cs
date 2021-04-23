using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TacControl.Common;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TacControl.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectPage : ContentPage
    {
        public Networking networking { get; } = Networking.Instance;

        private TacControlEndpoint CurrentEndpoint { get; set; } = null;

        public ConnectPage()
        {
            InitializeComponent();

            Networking.Instance.StartUDPSearch();
        }
        
        private void OnConnectClick(object sender, EventArgs e)
        {
            ConnectButton.IsEnabled = false;
            networking.Connect(CurrentEndpoint);
        }

        private void OnAddServerClick(object sender, EventArgs e)
        {
            var host = System.Net.Dns.GetHostEntry(DirectIPTextBox.Text);
            var addr = host.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            if (addr == null) return;

            //#TODO let user provide port with :
            networking.AvailableEndpoints.Add(new TacControlEndpoint { Address = new IPEndPoint(addr, 8082), ClientID = DirectIPTextBox.Text, LastActvity = DateTime.Now });
        }

        private void ListView_OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            CurrentEndpoint = e.SelectedItem as TacControlEndpoint;
            ConnectButton.IsEnabled = true;
        }
    }
}
