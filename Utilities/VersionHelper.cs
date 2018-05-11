using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class VersionHelper
    {
        internal static Version AppVersion { get; private set; }
        internal static Version LatestVersion { get; private set; }
        internal static string AppUrlHomePage { get; set; }
        internal static string VersionUrl { get; set; }
        internal static string NewAppUrl { get; set; }
        internal static long CheckAfter { get; set; }  //6H: 216000000000, 1D: 864000000000, 10D: 8640000000000

        static VersionHelper()
        {
            _set();
        }

        private static void _set()
        {
            LatestVersion = AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
            CheckAfter = 8640000000000;
        }

        internal static void GetNewApp()
        {
            try { System.Diagnostics.Process.Start(VersionHelper.NewAppUrl); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
        }

        internal static async Task<bool?> Check(bool force = false)
        {
            long now = DateTime.UtcNow.Ticks;
            long LastChekedTicks = AppSettings.Get("App", "LastCheked", "0").Long();

            if (force || ((LastChekedTicks + CheckAfter) < now))
            {
                AppSettings.Set("App", "LastCheked", now.String());

                WebRequest wRequest = null;
                WebResponse vResponse = null;
                try
                {
                    wRequest = WebRequest.CreateHttp(VersionUrl);
                    wRequest.Method = "GET";
                    wRequest.Timeout = 5000;
                    vResponse = await wRequest.GetResponseAsync();

                    using (StreamReader sr = new StreamReader(vResponse.GetResponseStream(), System.Text.Encoding.UTF8))
                    {
                        string newVar = await sr.ReadToEndAsync();
                        if (!newVar.NullEmpty())
                        {
                            AppSettings.Set("App", "LatestVersion", newVar);

                            Version latestVar = null;
                            if (Version.TryParse(newVar, out latestVar))
                                LatestVersion = latestVar;

                            return (LatestVersion > AppVersion);
                        }
                    }
                }
                catch { return false; }
                finally { if (vResponse != null) vResponse.Dispose(); }
            }

            return false;
        }
    }
}