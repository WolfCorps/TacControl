using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using TacControl.Common;
using TacControl.Common.Modules;
using Path = System.IO.Path;
using PixelFormat = System.Windows.Media.PixelFormat;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace TacControl
{
    public partial class MainWindow : Window
    {
        public GameState gsRef { get; set; } = GameState.Instance;

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
                ? Application.Current.Windows.OfType<T>().Any()
                : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        public static T GetWindow<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
                ? Application.Current.Windows.OfType<T>().First()
                : Application.Current.Windows.OfType<T>().First(w => w.Name.Equals(name));
        }



        public static void Compress(DirectoryInfo directorySelected)
        {
            foreach (FileInfo fileToCompress in directorySelected.GetFiles())
            {
                using (FileStream originalFileStream = fileToCompress.OpenRead())
                {
                    if ((File.GetAttributes(fileToCompress.FullName) &
                         FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".svgz")
                    {
                        using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + "z"))
                        {
                            using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                                CompressionMode.Compress))
                            {
                                originalFileStream.CopyTo(compressionStream);
                            }
                        }
                        FileInfo info = new FileInfo(fileToCompress.FullName + "z");
                        Console.WriteLine($"Compressed {fileToCompress.Name} from {fileToCompress.Length.ToString()} to {info.Length.ToString()} bytes.");
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Networking.Instance.MainThreadInvoke = (action) =>
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

                    return App.Current.Dispatcher.InvokeAsync(action).Task;

                    //Xamarin
                    //Device.BeginInvokeOnMainThread(() =>
                    //{
                    //    try
                    //    {
                    //        action();
                    //        tcs.SetResult(null);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        tcs.SetException(ex);
                    //    }
                    //
                    //}); return tcs.Task;
                };

            Networking.Instance.Connect();
            
            Compress(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "maps")));
        }

        private void OpenTacRadio_Click(object sender, RoutedEventArgs e)
        {
            if (IsWindowOpen<RadioWindow>())
            {
                GetWindow<RadioWindow>().Focus();
                return;
            }
                

            var newWindow = new RadioWindow();
            newWindow.Show();
        }

        private void OpenTacMap_Click(object sender, RoutedEventArgs e)
        {
            if (IsWindowOpen<MapViewWindow>())
            {
                GetWindow<MapViewWindow>().Focus();
                return;
            }
              

            var newWindow = new MapViewWindow();
            newWindow.Show();
        }
    }
}
