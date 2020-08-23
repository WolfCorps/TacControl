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

            Networking.Instance.Connect();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
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
