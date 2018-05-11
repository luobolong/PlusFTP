using System;
using Hani.Utilities;

namespace Hani.FTP
{
    internal static class FTPCommandsHelper
    {
        internal static string[] GetFeat(string str)
        {
            string[] feat = new string[] { };

            if (str.NullEmpty()) return feat;
            string[] strs = str.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2) return feat;

            feat = new string[strs.Length - 2];

            int j = -1;
            for (int i = 1; i < strs.Length - 1; i++)
                feat[++j] = strs[i];

            return feat;
        }

        internal static long GetSize(string str)
        {
            if ((str == null) && !str.Contains(" ")) return 0;

            return str.Split(' ')[1].Long();
        }
    }
}