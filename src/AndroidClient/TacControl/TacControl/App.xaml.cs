using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TacControl.Common;
using TacControl.Common.Modules;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TacControl.Services;
using TacControl.Views;
using Xamarin.Essentials;
using Xamarin.Forms.Internals;
using System.Net;

namespace TacControl
{
    public partial class App : Application
    {

        public App(Func<Action, Task> MainThreadInvoke)
        {

            //#TODO https://stackoverflow.com/questions/43687689/keeping-screen-turned-on-for-certain-pages

            Xamarin.Forms.Internals.Log.Listeners.Add(new DelegateLogListener((arg1, arg2) => Debug.WriteLine(arg2)));

            Networking.Instance.MainThreadInvoke = MainThreadInvoke;

            InitializeComponent();

            //Networking.Instance.Connect(new IPEndPoint(IPAddress.Parse("10.0.0.10"),8082));

            DependencyService.Register<MockDataStore>();

            MainPage = new AppShell();
            Routing.RegisterRoute("//ConnectPage", typeof(ConnectPage));
            Shell.Current.GoToAsync("//ConnectPage");

            Networking.Instance.OnConnected += () => MainThreadInvoke(() =>
            {
                Networking.Instance.StopUDPSearch();
                Shell.Current.GoToAsync("//MapPage", true);
            });

        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
