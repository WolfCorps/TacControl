using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Sentry;
using Sentry.Protocol;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }



    public static class MainSentry
    {
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            int width = 0;
            try
            {
                
                while (width < 160)
                {
                    System.Console.SetWindowSize(++width, 1);
                }
            } catch (System.ArgumentOutOfRangeException)
            {
                width--;
            }

            try
            {
                int height = 0;
                while (height < 80)
                {
                    System.Console.SetWindowSize(width, ++height);
                }
            }
            catch (System.ArgumentOutOfRangeException)
            {

            }

            System.Console.SetBufferSize(width, Int16.MaxValue-1);

            //https://stackoverflow.com/questions/1600962/displaying-the-build-date
            
            var linkTimeLocal = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
            var username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;


#if DEBUG
            App.Main();
#else
            using (SentrySdk.Init((o) => {
                o.Dsn = "https://78e23a3aba34433a89f5a78e172dfcf8@o251526.ingest.sentry.io/5390642";
                o.Release = $"TacControl@{linkTimeLocal:yy-MM-dd_HH-mm}";
                o.Environment = username == "LAMBDA\\Dedmen" ? "Dev" : "Alpha";
            }))
            {
                SentrySdk.ConfigureScope((scope) => {
                    scope.User = new User
                    {
                        Username = username
                    };
                });

                App.Main();
            }

#endif

        }
    }


}
