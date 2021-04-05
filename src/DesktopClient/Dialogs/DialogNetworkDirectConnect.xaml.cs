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
using System.Windows.Shapes;
using TacControl.Common;

namespace TacControl.Dialogs
{
    /// <summary>
    /// Interaction logic for DialogNetworkDirectConnect.xaml
    /// </summary>
    public partial class DialogNetworkDirectConnect : Window
    {
        public DialogNetworkDirectConnect()
        {
            InitializeComponent();
        }

        private void CoolButton_Click(object sender, RoutedEventArgs e)
        {


            var host = System.Net.Dns.GetHostEntry(IPAddrInput.Text);
            var addr = host.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork );
            if (addr == null) return;

            Networking.Instance.Connect(new IPEndPoint(addr, 8082));
            Close();
        }
    }
}
