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
using TacControl.Common;

namespace TacControl.BigWidgets
{
    /// <summary>
    /// Interaction logic for RadioSettingsList.xaml
    /// </summary>
    public partial class RadioSettingsList : UserControl
    {
        public GameState gsRef { get; set; } = GameState.Instance;
        public RadioSettingsList()
        {
            InitializeComponent();
        }
    }
}
