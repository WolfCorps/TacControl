using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using TacControl.Common;
using TacControl.Common.Modules;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TacControl.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {

        public GameState gsRef { get; set; } = GameState.Instance;

        public class TFARRadioChannel
        {
            public TFARRadio radio;
            public string Freq;
            public int channel;
        }

        public AboutViewModel()
        {
            Title = "About";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://aka.ms/xamain-quickstart"));
        }

        public ICommand OpenWebCommand { get; }
    }
}
