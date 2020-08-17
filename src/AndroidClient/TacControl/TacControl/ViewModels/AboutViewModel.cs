using System;
using System.Windows.Input;
using TacControl.Common;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TacControl.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {

        public GameState gsRef { get; set; } = GameState.Instance;

        public AboutViewModel()
        {
            Title = "About";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://aka.ms/xamain-quickstart"));
        }

        public ICommand OpenWebCommand { get; }
    }
}
