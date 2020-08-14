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
using Mapsui.Layers;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for LayerList.xaml
    /// </summary>
    public partial class LayerList : UserControl
    {
        public LayerList()
        {
            InitializeComponent();
        }


        public void Initialize(LayerCollection layers)
        {
            Items.Children.Clear();

            foreach (var layer in layers)
            {
                var item = new LayerListItem { LayerName = layer.Name };
                item.Enabled = layer.Enabled;
                //item.LayerOpacity = layer.Opacity;
                item.Layer = layer;
                Items.Children.Add(item);
            }
        }
    }
}
