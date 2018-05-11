using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class MassExtensions
    {
        internal static string String(this DateTime date, string str)
        {
            return date.ToString(str, CultureInfo.CurrentCulture);
        }

        internal static string String(this int n)
        {
            return n.ToString(CultureInfo.CurrentCulture);
        }

        internal static string StringInv(this int n)
        {
            return n.ToString(CultureInfo.InvariantCulture);
        }

        internal static string String(this long l)
        {
            return l.ToString(CultureInfo.CurrentCulture);
        }

        internal static string String(this double d)
        {
            return d.ToString(CultureInfo.CurrentCulture);
        }

        internal static int CompareC(this string x, string y)
        {
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        internal static int Index(this string str, string arg)
        {
            return str.IndexOf(arg, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool Ends(this string str, string arg)
        {
            return str.EndsWith(arg, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool Starts(this string str, string arg)
        {
            return str.StartsWith(arg, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool Equal(this string str, string arg)
        {
            return str.Equals(arg, StringComparison.OrdinalIgnoreCase);
        }

        internal static string FormatC(this string str, object arg)
        {
            return string.Format(CultureInfo.CurrentCulture, str, arg);
        }

        internal static string FormatC(this string str, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, str, args);
        }

        internal static string Lower(this string str)
        {
            return str.ToLower(CultureInfo.CurrentCulture);
        }

        internal static bool True(this string str, bool value = false)
        {
            if (str != null) return (str.Lower() == "true");
            return value;
        }

        internal static bool NullEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        internal static double Double(this string str, double value = 0.0)
        {
            double nOut;
            if (double.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out nOut)) return nOut;
            return value;
        }

        internal static long Long(this string str, long value = 0)
        {
            long nOut;
            if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out nOut)) return nOut;
            return value;
        }

        internal static int Int(this string str, int value = 0)
        {
            int nOut;
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out nOut)) return nOut;
            return value;
        }

        internal static bool DateInvCulture(this string str, string arg, out DateTime date)
        {
            if (DateTime.TryParseExact(str, arg, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;
            return false;
        }

        internal static bool DateInvCulture(this string str, string[] args, out DateTime date)
        {
            if (DateTime.TryParseExact(str, args, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;
            return false;
        }

        internal static bool Contains(this string[] strs, string str)
        {
            if (strs.Length == 0) return false;

            bool exists = false;
            Parallel.For(0, strs.Length, (i, loopState) =>
            {
                if (strs[i].Contains(str))
                {
                    exists = true;
                    loopState.Stop();
                }
            });

            return exists;
        }
    }
}