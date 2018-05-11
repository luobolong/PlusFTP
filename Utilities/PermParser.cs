using System.Text.RegularExpressions;

namespace Hani.Utilities
{
    internal static class PermParser
    {
        internal static Regex permRegex;

        static PermParser()
        {
            _set();
        }

        private static void _set()
        {
            permRegex = new Regex(@"[\-wrx]{9}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        internal static string ParseText(string perm)
        {
            if (perm.NullEmpty()) return string.Empty;

            switch (perm)
            {
                case "rw-r--r--": { return "644"; }
                case "rwxr-xr-x": { return "755"; }
            }

            if (permRegex.IsMatch(perm))
                return _parseOGE(perm.Substring(0, 3)) +
                       _parseOGE(perm.Substring(3, 3)) +
                       _parseOGE(perm.Substring(6, 3));

            return string.Empty;
        }

        internal static string GetLetters(string text)
        {
            string perm = string.Empty;
            int[] numbers = new int[] { (int)text[0], (int)text[1], (int)text[2] };

            perm += ((numbers[0] & 4) == 4) ? 'r' : '-';
            perm += ((numbers[0] & 2) == 2) ? 'w' : '-';
            perm += ((numbers[0] & 1) == 1) ? 'x' : '-';

            perm += ((numbers[1] & 4) == 4) ? 'r' : '-';
            perm += ((numbers[1] & 2) == 2) ? 'w' : '-';
            perm += ((numbers[1] & 1) == 1) ? 'x' : '-';

            perm += ((numbers[2] & 4) == 4) ? 'r' : '-';
            perm += ((numbers[2] & 2) == 2) ? 'w' : '-';
            perm += ((numbers[2] & 1) == 1) ? 'x' : '-';

            return perm;
        }

        private static string _parseOGE(string permissions)
        {
            return ((permissions[0] == 'r' ? 4 : 0) +
                    (permissions[1] == 'w' ? 2 : 0) +
                    (permissions[2] == 'x' ? 1 : 0)).String();
        }
    }
}