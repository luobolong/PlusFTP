using System;

namespace Hani.Utilities
{
    internal static class DaysWord
    {
        internal static string Today { get; private set; }
        internal static string TodayWord { get; set; } //Today
        internal static string Yesterday { get; private set; }
        internal static string YesterdayWord { get; set; } //Yesterday
        internal static string DShortDateFormat { get; set; } //MMM dd yyyy

        static DaysWord()
        {
            __set();
        }

        private static void __set()
        {
            DShortDateFormat = DateFormatHelper.SysShortDateFormat;
        }

        internal static void _set()
        {
            DateTime today = DateTime.Today;
            Today = today.String(DShortDateFormat);
            Yesterday = today.AddDays(-1).String(DShortDateFormat);
        }

        internal static string Parse(string date)
        {
            return date.NullEmpty() ? string.Empty : date.Replace(Today, TodayWord).Replace(Yesterday, YesterdayWord);
        }
    }
}