using System;
using System.Collections.Generic;
using System.ComponentModel;
using PropertyChanged;
using TacControl.Common;
using TacControl.ViewModels;
using TacControl.Views;
using Xamarin.Forms;

namespace TacControl
{
    public partial class AppShell : Xamarin.Forms.Shell, INotifyPropertyChanged
    {
        public bool Connected { get; set; } = false;
        [DependsOn(nameof(Connected))]
        public bool NotConnected => !Connected;


        public AppShell()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<App>(this, message: "connected", (sender) =>
            {
                Connected = true;
            });
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
