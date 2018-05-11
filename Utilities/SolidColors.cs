using System.Windows.Media;

namespace Hani.Utilities
{
    internal static class SolidColors
    {
        internal static SolidColorBrush Black;
        internal static SolidColorBrush White;
        internal static SolidColorBrush Green;
        internal static SolidColorBrush DarkGreen;
        internal static SolidColorBrush Orange;
        internal static SolidColorBrush DarkOrange;
        internal static SolidColorBrush Red;
        internal static SolidColorBrush DarkRed;
        internal static SolidColorBrush DimGray;
        internal static SolidColorBrush DeepSkyBlue;
        internal static SolidColorBrush SolidBlue;
        internal static SolidColorBrush SolidGreen;

        static SolidColors()
        {
            _set();
        }

        private static void _set()
        {
            Black = new SolidColorBrush(Colors.Black);
            White = new SolidColorBrush(Colors.White);
            Green = new SolidColorBrush(Colors.Green);
            DarkGreen = new SolidColorBrush(Colors.DarkGreen);
            Orange = new SolidColorBrush(Colors.Orange);
            DarkOrange = new SolidColorBrush(Colors.DarkOrange);
            Red = new SolidColorBrush(Colors.Red);
            DarkRed = new SolidColorBrush(Colors.DarkRed);
            DimGray = new SolidColorBrush(Colors.DimGray);

            byte[] deepSkyBlue = new byte[] { 204, 17, 158, 218 };
            DeepSkyBlue = new SolidColorBrush(Color.FromArgb(deepSkyBlue[0], deepSkyBlue[1], deepSkyBlue[2], deepSkyBlue[3]));

            byte[] solidBlue = new byte[] { 255, 0, 88, 176 };
            SolidBlue = new SolidColorBrush(Color.FromArgb(solidBlue[0], solidBlue[1], solidBlue[2], solidBlue[3]));

            byte[] solidGreen = new byte[] { 255, 0, 120, 64 };
            SolidGreen = new SolidColorBrush(Color.FromArgb(solidGreen[0], solidGreen[1], solidGreen[2], solidGreen[3]));
        }
    }
}