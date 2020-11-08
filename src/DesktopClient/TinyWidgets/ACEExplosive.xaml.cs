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
using TacControl.Common.Modules;

namespace TacControl.TinyWidgets
{
    /// <summary>
    /// Interaction logic for ACEExplosive.xaml
    /// </summary>
    public partial class ACEExplosive : UserControl
    {
        public ACEExplosive()
        {
            InitializeComponent();
        }


        public Explosive ExpRef
        {
            get => (Explosive)GetValue(ExpRefProperty);
            set => SetValue(ExpRefProperty, value);
        }

        public static readonly DependencyProperty ExpRefProperty = DependencyProperty.Register(nameof(ExpRef), typeof(Explosive), typeof(ACEExplosive));


        private void Detonate_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

            GameState.Instance.ace.DetonateExplosive(ExpRef.Id);


        }
    }
}
