using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Hani.Utilities
{
    internal static class ImageFactory
    {
        private static Guid guidItemUuid;

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int x, int y)
            {
                cx = x;
                cy = y;
            }
        }

        private enum SIGDN : uint
        {
            NORMALDISPLAY = 0,
            PARENTRELATIVEPARSING = 0x80018001,
            PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            PARENTRELATIVEEDITING = 0x80031001,
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            FILESYSPATH = 0x80058000,
            URL = 0x80068000
        }

        [Flags]
        public enum SIIGBF
        {
            RESIZETOFIT = 0x00000000,
            BIGGERSIZEOK = 0x00000001,
            MEMORYONLY = 0x00000002,
            ICONONLY = 0x00000004,
            THUMBNAILONLY = 0x00000008,
            INCACHEONLY = 0x00000010
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc,
                [MarshalAs(UnmanagedType.LPStruct)]Guid bhid,
                [MarshalAs(UnmanagedType.LPStruct)]Guid riid,
                out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImportAttribute()]
        [GuidAttribute("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(
            [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
            [In] SIIGBF flags,
            [Out] out IntPtr phbm);
        }

        #endregion Structures

        #region DllImports

        [SuppressUnmanagedCodeSecurityAttribute]
        private static class SafeNativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
            internal static extern void SHCreateItemFromParsingName(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In] IntPtr pbc,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteObject(IntPtr hObject);
        }

        #endregion DllImports

        static ImageFactory()
        {
            _set();
        }

        private static void _set()
        {
            guidItemUuid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
        }

        internal static void Get(ref BitmapSource icon, string source, SIIGBF flags)
        {
            try
            {
                IShellItem ppsi = null;
                SafeNativeMethods.SHCreateItemFromParsingName(source, IntPtr.Zero, guidItemUuid, out ppsi);

                IntPtr hbitmap = IntPtr.Zero;
                ((IShellItemImageFactory)ppsi).GetImage(new SIZE(48, 48), flags | SIIGBF.BIGGERSIZEOK, out hbitmap);
                icon = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                SafeNativeMethods.DeleteObject(hbitmap);

                if (icon != null) icon.Freeze();
            }
            catch { }
        }
    }
}