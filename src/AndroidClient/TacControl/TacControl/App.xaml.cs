using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TacControl.Common;
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
