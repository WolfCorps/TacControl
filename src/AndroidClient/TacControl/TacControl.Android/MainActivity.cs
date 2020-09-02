using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Java.Nio;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Distribute;
using SkiaSharp;
using SkiaSharp.Views.Android;
using TacControl.Common.Modules;
using Xamarin.Essentials;

namespace TacControl.Droid
{
    [Activity(Label = "TacControl", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private WifiManager.MulticastLock castLock;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            AppCenter.Start("b17f9c9d-e90c-488f-8c4b-92ef3e305c0d", typeof(Analytics), typeof(Distribute));


            WifiManager wifiMgr = (WifiManager) ApplicationContext.GetSystemService(Context.WifiService);
            castLock = wifiMgr.CreateMulticastLock("TacControl-udp");
            castLock.SetReferenceCounted(true);
            castLock.Acquire();


            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App((action) =>
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                bool isMain = MainThread.IsMainThread;

                RunOnUiThread(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }

                });

                return tcs.Task;
            }));
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
