using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Media;

namespace Hani.Utilities
{
    internal static class MouseUtilities
    {
        private static Win32Point win32Point;
        private static Point point;

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        };

        [SuppressUnmanagedCodeSecurityAttribute]
        private static class SafeNativeMethods
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCursorPos(ref Win32Point pt);
        }

        static MouseUtilities()
        {
            _set();
        }

        private static void _set()
        {
            point = new Point();
            win32Point = new Win32Point();
        }

        internal static Point GetPosition(Visual relativeTo)
        {
            SafeNativeMethods.GetCursorPos(ref win32Point);

            point.X = win32Point.X;
            point.Y = win32Point.Y;

            if (relativeTo == null) return point;

            try { return relativeTo.PointFromScreen(point); }
            catch { return point; }
        }
    }
}