using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Hani.Utilities
{
    internal static class AppLanguage
    {
        internal delegate void UpdatedHandler();
        internal static event UpdatedHandler OnUpdated;

        private static void Updated()
        {
            if (OnUpdated != null) new UpdatedHandler(OnUpdated)();
        }

        internal static CultureInfo AppCultureInfo { get; private set; }
        internal static string LanguageName { get; private set; }
        internal static char RLMARK { get; private set; }
        internal static int LanguageID { get; private set; }

        //internal static bool IsRTL { get; private set; } // NO NEED
        private static Dictionary<string, string> phraseCache;

        private static string languageCode;
        internal static string LanguageCode
        {
            get { return languageCode; }
            set { prepareLanguage(value); }
        }

        static AppLanguage()
        {
            _set();
        }

        internal static string Get(string name)
        {
            if (phraseCache.ContainsKey(name)) return phraseCache[name];
            string phrase = Application.Current.Resources.MergedDictionaries[0][name] as string;
            if (phrase == null) phrase = name + "-X";

            Task.Run(() =>
            {
                try { phraseCache.Add(name, phrase); }
                catch { }
            });

            return phrase;
        }

        internal static void SetCurrentThreadCulture(Thread thread)
        {
            thread.CurrentUICulture = AppLanguage.AppCultureInfo;
            thread.CurrentCulture = AppLanguage.AppCultureInfo;
        }

        internal static void Save()
        {
            AppSettings.Set("App", "Language", LanguageCode);
        }

        private static void _set()
        {
            phraseCache = new Dictionary<string, string>();

            string code = AppSettings.Get("App", "Language", string.Empty);
            if (code.NullEmpty())
            {
                LanguageCode = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
                Save();
            }
            else LanguageCode = code;

            setWindowCommands();
        }

        private static void setWindowCommands()
        {
            MahApps.Metro.Controls.WindowCommands.minimize = Get("LangMinimize");
            MahApps.Metro.Controls.WindowCommands.maximize = Get("LangMaximize");
            MahApps.Metro.Controls.WindowCommands.restore = Get("LangRestore");
            MahApps.Metro.Controls.WindowCommands.closeText = Get("LangClose");
        }

        private static void prepareLanguage(string code)
        {
            languageCode = code;

            setCultureInfo();
            setLanguageName();

            CultureInfo.DefaultThreadCurrentCulture = AppCultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = AppCultureInfo;
            Thread.CurrentThread.CurrentUICulture = AppCultureInfo;
            Thread.CurrentThread.CurrentCulture = AppCultureInfo;

            Uri lngUri = getLanguageUri();
            if (lngUri != Application.Current.Resources.MergedDictionaries[0].Source)
            {
                ResourceDictionary lngRes = new ResourceDictionary();
                lngRes.Source = lngUri;
                Application.Current.Resources.MergedDictionaries[0] = lngRes;
                phraseCache.Clear();
            }

            setDirection();

            Updated();
        }

        private static void setCultureInfo()
        {
            AppCultureInfo = CultureInfo.GetCultureInfoByIetfLanguageTag(languageCode);
        }

        private static void setLanguageName()
        {
            switch (languageCode)
            {
                case "ar": LanguageID = 0; LanguageName = "Arabic"; break;
                default: languageCode = "en"; LanguageID = 1; LanguageName = "English"; break;
            }
        }

        private static void setDirection()
        {
            switch (languageCode)
            {
                case "ar":
                    RLMARK = '\u200F';
                    //IsRTL = true;
                    break;

                default:
                    RLMARK = '\u200E';
                    //IsRTL = false;
                    break;
            }
        }

        private static Uri getLanguageUri()
        {
            return new Uri("pack://application:,,,/Languages/" + LanguageName + ".xaml");
        }
    }
}