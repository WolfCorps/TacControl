using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacControl.Common;
using TacControl.Common.Modules;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TacControl.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExplosiveButtons : ContentView
    {
        public ExplosiveButtons()
        {
            InitializeComponent();
        }


        public Explosive ExpRef
        {
            get => (Explosive)GetValue(ExpRefProperty);
            set => SetValue(ExpRefProperty, value);
        }

        public static readonly BindableProperty ExpRefProperty =
            BindableProperty.Create(nameof(ExpRef), typeof(Explosive), typeof(ExplosiveButtons), null, BindingMode.OneWay, null);





        private void Explode_OnPressed(object sender, EventArgs e)
        {
           
        }

        private void Explode_OnReleased(object sender, EventArgs e)
        {
            GameState.Instance.ace.DetonateExplosive(ExpRef.Id);
        }
    }
}
