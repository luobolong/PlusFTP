namespace Hani.Utilities
{
    internal static class SizeUnit
    {
        internal static string SizeB;
        internal static string SizeKB;
        internal static string SizeMB;
        internal static string SizeGB;
        internal static string SizeTB;

        static SizeUnit()
        {
            _set();
        }

        private static void _set()
        {
            SizeB = " Bytes";
            SizeKB = "KB";
            SizeMB = "MB";
            SizeGB = "GB";
            SizeTB = "TB";
        }

        internal static string Parse(long bytes)
        {
            if (bytes < 1024) return bytes + " " + SizeB;
            else if (bytes < 1048576) return ParseUnit(bytes, 1024) + SizeKB;
            else if (bytes < 1073741824) return ParseUnit(bytes, 1048576) + SizeMB;
            else if (bytes < 1099511627776) return ParseUnit(bytes, 1073741824) + SizeGB;
            else return ParseUnit(bytes, 1099511627776) + SizeTB;
        }

        private static string ParseUnit(double bytes, long unit)
        {
            return "{0:#0.00} ".FormatC(bytes / unit);//Math.Ceiling(bytes / unit)
        }
    }
}