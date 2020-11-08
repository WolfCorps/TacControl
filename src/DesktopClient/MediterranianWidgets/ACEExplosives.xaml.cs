using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TacControl.Common;
using TacControl.Common.Modules;

namespace TacControl.MediterranianWidgets
{
    /// <summary>
    /// Interaction logic for RadioSettings.xaml
    /// </summary>
    public partial class ACEExplosives : UserControl
    {
        public ACEExplosives()
        {
            InitializeComponent();
        }

        public ModuleACE aceRef { get; } = GameState.Instance.ace;




    }
}
