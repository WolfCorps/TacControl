using System;
using System.Collections.Generic;
using TacControl.ViewModels;
using TacControl.Views;
using Xamarin.Forms;

namespace TacControl
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public bool Connected { get; set; } = false;
        public bool NotConnected => !Connected;


        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
