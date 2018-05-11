using System;
using Microsoft.Win32;

namespace Hani.Utilities
{
    internal static class DateFormatHelper
    {
        internal static string SysShortDateFormat;
        internal static string SysTimeFormat;
        internal static string SysShortTimeFormat;
        internal static string SysDateTimeFormat;

        static DateFormatHelper()
        {
            _set();
        }

        private static void _set()
        {
            try { SysShortDateFormat = Registry.CurrentUser.OpenSubKey(@"Control Panel\International", false).GetValue("sShortDate").ToString(); }
            catch (Exception exp) { SysShortDateFormat = "d/M/yyyy"; ExceptionHelper.Log(exp); }

            try { SysTimeFormat = Registry.CurrentUser.OpenSubKey(@"Control Panel\International", false).GetValue("sTimeFormat").ToString(); }
            catch (Exception exp) { SysTimeFormat = "h:mm:ss tt"; ExceptionHelper.Log(exp); }

            try { SysShortTimeFormat = Registry.CurrentUser.OpenSubKey(@"Control Panel\International", false).GetValue("sShortTime").ToString(); }
            catch (Exception exp) { SysShortTimeFormat = "h:mm tt"; ExceptionHelper.Log(exp); }
            SysDateTimeFormat = SysShortDateFormat + " " + SysTimeFormat;
        }

        internal static string GetShortDateTimeSafe()
        {
            return GetShortDateTime().Replace(@"/", "-").Replace(":", "-");
        }

        internal static string GetShortDateTime()
        {
            return DateTime.Now.String(SysShortDateFormat + " " + SysShortTimeFormat);
        }
    }
}