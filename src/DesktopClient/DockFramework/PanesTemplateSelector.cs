using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Layout;

namespace TacControl
{
    class PanesTemplateSelector : DataTemplateSelector
    {

        public PanesTemplateSelector()
        {

        }
        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is UserControlViewModel vm)
            {
                FrameworkElementFactory factory = new FrameworkElementFactory(vm.type);
                DataTemplate dt = new DataTemplate();
                dt.VisualTree = factory;
                return dt;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
