using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Hani.Utilities
{
    internal static class ImageList
    {
        private static IImageList iImageList;

        #region Structures

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        internal enum SpecialFolderCSIDL : int
        {
            DESKTOP = 0x0000,    // Desktop
            PERSONAL = 0x0005,   // Documents
            DRIVES = 0x0011,     // My PC
        }

        [Flags]
        internal enum SHFlags : uint
        {
            ICON = 0x100,                // get icon
            DISPLAYNAME = 0x200,         // get display name
            TYPENAME = 0x400,            // get type name
            ATTRIBUTES = 0x800,          // get attributes
            ICONLOCATION = 0x1000,       // get icon location
            EXETYPE = 0x2000,            // return exe type
            SYSICONINDEX = 0x4000,       // get system icon index
            LINKOVERLAY = 0x8000,        // put a link overlay on icon
            SELECTED = 0x10000,          // show icon in selected state
            ATTR_SPECIFIED = 0x20000,    // get only specified attributes
            LARGEICON = 0x0,             // get large icon
            SMALLICON = 0x1,             // get small icon
            OPENICON = 0x2,              // get open icon
            SHELLICONSIZE = 0x4,         // get shell size
            PIDL = 0x8,                  // pszPath is a pidl
            USEFILEATTRIBUTES = 0x10,    // use passed dwFileAttribute
            ADDOVERLAYS = 0x000000020,   // apply the appropriate overlays
            OVERLAYINDEX = 0x000000040   // Get the index of the overlay
        }

        public enum ImageListIconSize : int
        {
            Large = 0x0,
            Small = 0x1,
            ExtraLarge = 0x2,
            Jumbo = 0x4
        }

        [Flags]
        public enum ILD_FLAGS : int
        {
            NORMAL = 0x00000000,
            TRANSPARENT = 0x00000001,
            BLEND25 = 0x00000002,
            FOCUS = 0x00000002,
            BLEND50 = 0x00000004,
            SELECTED = 0x00000004,
            BLEND = 0x00000004,
            MASK = 0x00000010,
            IMAGE = 0x00000020,
            ROP = 0x00000040,
            OVERLAYMASK = 0x00000F00,
            PRESERVEALPHA = 0x00001000,
            SCALE = 0x00002000,
            DPISCALE = 0x00004000,
            ASYNC = 0x00008000
        }

        #endregion Structures

        #region DllImports

        [SuppressUnmanagedCodeSecurityAttribute]
        private static class SafeNativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern IntPtr SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("shell32.dll", EntryPoint = "#727")]
            internal extern static int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

            [DllImport("comctl32.dll", SetLastError = true)]
            internal static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern int SHGetFolderLocation(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwReserved, out IntPtr ppidl);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DestroyIcon(IntPtr hIcon);
        }

        #endregion DllImports

        #region IImageList

        [ComImportAttribute()]
        [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int GetIcon(int i, int flags, ref IntPtr picon);
        };

        #endregion IImageList

        static ImageList()
        {
            _set();
        }

        private static void _set()
        {
            try
            {
                Guid imageListGuid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                int r = SafeNativeMethods.SHGetImageList((int)ImageListIconSize.ExtraLarge, ref imageListGuid, out iImageList);
                if (r != 0) iImageList = null;
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
        }

        internal static void Get(ref BitmapSource icon, string source, SHFlags flags)
        {
            if (iImageList == null) return;

            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hIml = SafeNativeMethods.SHGetFileInfo(source, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(flags));
                getIcon(ref icon, ref shinfo, hIml);
            }
            catch { }
        }

        internal static void Get(ref BitmapSource icon, int source, SHFlags flags)
        {
            try
            {
                IntPtr ppidl = IntPtr.Zero;
                int r = SafeNativeMethods.SHGetFolderLocation(IntPtr.Zero, source, IntPtr.Zero, 0, out ppidl);
                if (r != 0) return;

                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hIml = SafeNativeMethods.SHGetFileInfo(ppidl, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(flags));

                getIcon(ref icon, ref shinfo, hIml);
            }
            catch { }
        }

        private static void getIcon(ref BitmapSource icon, ref SHFILEINFO shinfo, IntPtr hIml)
        {
            //IntPtr hIcon = IntPtr.Zero;
            //iImageList.GetIcon(shinfo.iIcon, 0, ref hIcon);

            IntPtr hIcon = SafeNativeMethods.ImageList_GetIcon(hIml, shinfo.iIcon, 0);
            SafeNativeMethods.DestroyIcon(shinfo.hIcon);

            icon = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (icon != null) icon.Freeze();
            SafeNativeMethods.DestroyIcon(hIcon);
        }
    }
}