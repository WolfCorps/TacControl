using System;
using System.Collections.Generic;
using System.Linq;
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
using TacControl.Common.Maps;
using TacControl.Common.Modules;
using TacControl.TinyWidgets;

namespace TacControl.BigWidgets.Map
{
    /// <summary>
    /// Interaction logic for MarkerVisibilityList.xaml
    /// </summary>
    public partial class MarkerVisibilityList : UserControl
    {
        private MarkerVisibilityManager _manager;

        public Action<MarkerChannel> OnCurrentChannelSelected;


        public MarkerVisibilityList()
        {
            InitializeComponent();
        }

        public void Initialize(MarkerVisibilityManager manager)
        {
            _manager = manager;

            Items.Children.Clear();

            foreach (var (channelid, name) in new (int,string)[]{
                (0, "Global"),
                (1, "Side"),
                (2, "Command"),
                (3, "Group"),
                (4, "Vehicle"),
                (5, "Direct") //#TODO custom channels
            })
            {
                var item = new MarkerVisibilityElement { Title = name };

                if (channelid == 0)
                    item.IsCurrent = true;

                item.OnSoloChanged += () =>
                {
                    _manager.SetChannelSolo(channelid, item.IsSolo);
                };
                item.OnIgnoreChanged += () =>
                {
                    _manager.SetChannelIgnore(channelid, item.IsIgnore);
                };

                item.OnCurrentChanged += () =>
                {
                    if (item.IsCurrent)
                    {
                        OnCurrentChannelSelected?.Invoke((MarkerChannel)channelid);
                    }
                    foreach (MarkerVisibilityElement itemsChild in Items.Children)
                    {
                        if (itemsChild != item)
                            itemsChild.IsCurrent = false;
                    }
                };


                Items.Children.Add(item);
            }
        }






    }
}
