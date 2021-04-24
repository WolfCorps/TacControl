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
using System.Windows.Shapes;
using TacControl.Common.Config;
using TacControl.Common.Config.Section;

namespace TacControl.Dialogs
{
    /// <summary>
    /// Interaction logic for DialogOptions.xaml
    /// </summary>
    public partial class DialogOptions : Window
    {

        public Map MapSettings { get; set; } = AppConfig.Instance.GetSection<Map>("Map");


        public DialogOptions()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
