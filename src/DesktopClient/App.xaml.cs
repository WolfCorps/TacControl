using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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

            var linkTimeLocal = Assembly.GetExecutingAssembly().GetLinkerTime(TimeZoneInfo.Utc);
            var username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            using (SentrySdk.Init((o) => {
                o.Dsn = new Dsn("https://78e23a3aba34433a89f5a78e172dfcf8@o251526.ingest.sentry.io/5390642");
                o.Release = $"TacControl@{linkTimeLocal:yy-MM-dd_HH-mm}";
                o.Environment = username == "Dedmen-PC\\dedmen" ? "Dev" : "Alpha";
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

        }


        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

    }


}
