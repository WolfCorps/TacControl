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

namespace TacControl
{
    /// <summary>
    /// Interaction logic for RadioWidget.xaml
    /// </summary>
    public partial class RadioWidget : UserControl
    {
        public GameState gsRef { get; set; } = GameState.Instance;
        public RadioWidget()
        {
            InitializeComponent();
        }
    }
}
