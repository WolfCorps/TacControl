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
using Mapsui.Widgets;

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
            List<LayerListItem> toRemove = new List<LayerListItem>();
            foreach (var source in Items.Children)
            {
                if (source is LayerListItem x && x.Widget == null)
                    toRemove.Add(x);

            }

            foreach (var source in toRemove)
                Items.Children.Remove(source);

            foreach (var layer in layers)
            {
                var item = new LayerListItem { LayerName = layer.Name };
                item.Enabled = layer.Enabled;
                //item.LayerOpacity = layer.Opacity;
                item.Layer = layer;
                Items.Children.Add(item);
            }
        }

        public void AddWidget(string name, IWidget widget)
        {
            var item = new LayerListItem { LayerName = name };
            item.Enabled = widget.Enabled;
            item.Widget = widget;
            Items.Children.Add(item);
            
        }

    }
}
