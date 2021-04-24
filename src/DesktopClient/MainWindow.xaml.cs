using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AvalonDock.Layout.Serialization;
using Sentry.Protocol;
using TacControl.Common;
using TacControl.Common.Config;
using TacControl.Common.Modules;
using TacControl.Misc;
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
            this.DataContext = Workspace.This;
            //this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Unloaded += new RoutedEventHandler(MainWindow_Unloaded);

            Networking.Instance.MainThreadInvoke = (action) =>
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

                    if (App.Current == null) return Task.CompletedTask; //Exiting

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


            //#TODO allow user to choose, use options dialog and use a default value
            Networking.Instance.UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            Networking.Instance.StartUDPSearch();

            Networking.Instance.OnConnected += () =>
            {
                Networking.Instance.MainThreadInvoke(() =>
                {
                    Console.WriteLine($"MainWindow: Network connection done(?), loading window layout...");
                    WaitingForConnectionLabel.Visibility = Visibility.Hidden;
                    dockManager.Visibility = Visibility.Visible;
                    var parent = VisualTreeHelper.GetParent(WaitingForConnectionLabel) as Grid;
                    parent.Children.Remove(WaitingForConnectionLabel);
                    MainWindow_Loaded(this, null);
                    Networking.Instance.StopUDPSearch();
                });



            };

            var directory = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "maps"));
            if (!directory.Exists) directory.Create();

            //Compress(directory);
        }

        private void LoadLayout(string path)
        {

            var layoutSerializer = new XmlLayoutSerializer(dockManager);

            // Here I've implemented the LayoutSerializationCallback just to show
            //  a way to feed layout desarialization with content loaded at runtime
            // Actually I could in this case let AvalonDock to attach the contents
            // from current layout using the content ids
            // LayoutSerializationCallback should anyway be handled to attach contents
            // not currently loaded
            layoutSerializer.LayoutSerializationCallback += (s, e) =>
            {
                if (e.Model.ContentId == null) return;
                Type t = Type.GetType(e.Model.ContentId);
                e.Content = new UserControlViewModel(t);
            };
            layoutSerializer.Deserialize(path);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.config")))
                LoadLayout(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.config"));
            else
                LoadDefaultLayout();
        }

        private void LoadDefaultLayout()
        {
            File.WriteAllText(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.config"), @"<?xml version=""1.0"" encoding=""utf-8""?>
<LayoutRoot>
  <RootPanel Orientation=""Horizontal"">
    <LayoutAnchorablePane DockWidth=""1.54856115107914*"" FloatingWidth=""1128"" FloatingHeight=""657"" FloatingLeft=""205"" FloatingTop=""608"">
      <LayoutAnchorable AutoHideMinWidth=""100"" AutoHideMinHeight=""100"" Title=""TacControl.MapView"" IsSelected=""True"" ContentId=""TacControl.MapView"" FloatingLeft=""205"" FloatingTop=""608"" FloatingWidth=""1128"" FloatingHeight=""657"" CanClose=""True"" LastActivationTimeStamp=""04/24/2021 16:28:21"" PreviousContainerId=""8334c561-83ba-42e7-b6f0-d85ecdd928d3"" PreviousContainerIndex=""3"" />
    </LayoutAnchorablePane>
    <LayoutAnchorablePane Id=""8334c561-83ba-42e7-b6f0-d85ecdd928d3"" Name=""ToolsPane"" DockWidth=""0.451438848920863*"">
      <LayoutAnchorable AutoHideMinWidth=""100"" AutoHideMinHeight=""100"" Title=""TacControl.RadioWidget"" IsSelected=""True"" ContentId=""TacControl.RadioWidget"" CanClose=""True"" LastActivationTimeStamp=""04/24/2021 16:28:24"" />
      <LayoutAnchorable AutoHideMinWidth=""100"" AutoHideMinHeight=""100"" Title=""TacControl.BigWidgets.RadioSettingsList"" ContentId=""TacControl.BigWidgets.RadioSettingsList"" CanClose=""True"" />
      <LayoutAnchorable AutoHideMinWidth=""100"" AutoHideMinHeight=""100"" Title=""TacControl.MediterranianWidgets.ACEExplosives"" ContentId=""TacControl.MediterranianWidgets.ACEExplosives"" CanClose=""True"" />
    </LayoutAnchorablePane>
  </RootPanel>
  <TopSide />
  <RightSide />
  <LeftSide />
  <BottomSide />
  <FloatingWindows />
  <Hidden />
</LayoutRoot>"
            );

            LoadLayout(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.config"));
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            var serializer = new XmlLayoutSerializer(dockManager);
            serializer.Serialize(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.config"));
        }


        #region LoadLayoutCommand

        private RelayCommand _loadLayoutCommand = null;
        
        public ICommand LoadLayoutCommand
        {
            get
            {
                if (_loadLayoutCommand == null)
                {
                    _loadLayoutCommand = new RelayCommand((p) => OnLoadLayout(), (p) => CanLoadLayout());
                }
        
                return _loadLayoutCommand;
            }
        }

        private bool CanLoadLayout()
        {
            return File.Exists(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.Layout.config"));
        }

        private void OnLoadLayout()
        {
            LoadLayout(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.Layout.config"));
        }

        #endregion LoadLayoutCommand

        #region SaveLayoutCommand

        private RelayCommand _saveLayoutCommand = null;
        
        public ICommand SaveLayoutCommand
        {
            get
            {
                if (_saveLayoutCommand == null)
                {
                    _saveLayoutCommand = new RelayCommand((p) => OnSaveLayout(), (p) => CanSaveLayout());
                }
        
                return _saveLayoutCommand;
            }
        }

        private bool CanSaveLayout()
        {
            return true;
        }

        private void OnSaveLayout()
        {
            var layoutSerializer = new XmlLayoutSerializer(dockManager);
            layoutSerializer.Serialize(Path.Combine(AppConfig.Instance.ConfigDirectory, "AvalonDock.Layout.config"));
        }

        #endregion SaveLayoutCommand

        private void OnDumpToConsole(object sender, RoutedEventArgs e)
        {
            // Uncomment when TRACE is activated on AvalonDock project
            //dockManager.Layout.ConsoleDump(0);
        }










    }
}
