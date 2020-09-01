using System.Windows.Controls;
using System.Windows;

namespace TacControl
{
    class PanesStyleSelector : StyleSelector
    {
        public Style ToolStyle
        {
            get;
            set;
        }

        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
        {
            if (item is UserControlViewModel)
                return ToolStyle;

            return base.SelectStyle(item, container);
        }
    }
}
