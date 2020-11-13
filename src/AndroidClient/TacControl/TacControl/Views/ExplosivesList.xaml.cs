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
    public partial class ExplosivesList : ContentPage
    {
        public ExplosivesList()
        {
            InitializeComponent();
        }


        public ModuleACE aceRef { get; set; } = GameState.Instance.ace;
    }
}
