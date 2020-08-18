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
using SkiaSharp;
using SkiaSharp.Views.Android;
using TacControl.Common.Modules;
using Xamarin.Essentials;

namespace TacControl.Droid
{


    public class Bitmap : ImageDirectory.IImage
    {
        public SKImage bmp;
        public object GetImage()
        {
            return bmp;
        }
    }

    public class BitmapFromDataWindows : IBitmapFromData
    {
        public ImageDirectory.IImage GetBitmapFrom(byte[] dataBytes, int width)
        {
            //ARGB -> BGRA
            for (int i = 0; i < dataBytes.Length; i += 4)
            {
                var A = dataBytes[i];
                var B = dataBytes[i + 1];
                var G = dataBytes[i + 2];
                var R = dataBytes[i + 3];
            
                dataBytes[i] = B;
                dataBytes[i + 1] = G;
                dataBytes[i + 2] = R;
                dataBytes[i + 3] = A;
            }

            var res = new Bitmap { bmp= SKImage.FromPixelCopy(new SKImageInfo(width, width, SKColorType.Bgra8888), dataBytes) }; //#TODO use this in common
            return res;
        }
    }










    [Activity(Label = "TacControl", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private WifiManager.MulticastLock castLock;
        protected override void OnCreate(Bundle savedInstanceState)
        {

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
            }, new BitmapFromDataWindows()));


            /*
                           ;
                //var bmp = new Bitmap {bmp = new System.Drawing.Bitmap(width, width, PixelFormat.Format32bppArgb)};
                var bmp = new Bitmap {bmp = new Android.Graphics.Bitmap(width, width, PixelFormat.Format32bppArgb)};

                //ARGB -> BGRA
                for (int i = 0; i < dataBytes.Length; i+=4)
                {
                    var A = dataBytes[i];
                    var B = dataBytes[i + 1];
                    var G = dataBytes[i + 2];
                    var R = dataBytes[i + 3];

                    dataBytes[i] = B;
                    dataBytes[i+1] = G;
                    dataBytes[i+2] = R;
                    dataBytes[i+3] = A;
                }


                BitmapData bmpData = bmp.bmp.LockBits(new Rectangle(0, 0,
                        bmp.bmp.Width,
                        bmp.bmp.Height),
                    ImageLockMode.WriteOnly,
                    bmp.bmp.PixelFormat);

                IntPtr pNative = bmpData.Scan0;
                Marshal.Copy(dataBytes, 0, pNative, dataBytes.Length);

                //var output = new FileStream("P:/test2", FileMode.CreateNew);
                //output.Write(dataBytes, 0, dataBytes.Length);
                //output.Close();


                bmp.bmp.UnlockBits(bmpData);
             *
             */










        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
