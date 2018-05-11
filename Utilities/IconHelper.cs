using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hani.Utilities
{
    internal static class IconHelper
    {
        private static Dictionary<string, BitmapSource> iconsList;
        //Local Only
        private const string NoExtCache = "exe, lnk, mp4, ico, gif, jpg, jpe, png, psd, pdf, bmp, tif, url, website";

        static IconHelper()
        {
            _set();
        }

        private static void _set()
        {
            iconsList = new Dictionary<string, BitmapSource>();
            //DImage = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Indexed1, BitmapPalettes.BlackAndWhite, new byte[] { 0, 0, 0, 0 }, 1);
        }

        internal static BitmapSource Get(int source)
        {
            string name = source.String();
            if (iconsList.ContainsKey(name)) return iconsList[name];

            BitmapSource icon = null;
            ImageList.Get(ref icon, source, ImageList.SHFlags.ICON | ImageList.SHFlags.DISPLAYNAME | ImageList.SHFlags.SYSICONINDEX | ImageList.SHFlags.PIDL);

            Task.Run(() =>
            {
                try { iconsList.Add(name, icon); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            });

            return icon;
        }

        internal static BitmapSource Get(string source, bool isFile, string extension, bool isLink)
        {
            bool Local = !source.Starts("/");
            bool iconOnly = (!Local || !extension.NullEmpty() && !NoExtCache.Contains(extension));
            extension = (extension.NullEmpty() ? "dummy" : extension);
            string name = iconOnly ? (isFile ? extension : "folder") : source;

            if (iconsList.ContainsKey(name)) return iconsList[name];

            BitmapSource icon = null;
            if (!Local)
            {
                if (isFile) ImageList.Get(ref icon, "." + extension, ImageList.SHFlags.ICON | ImageList.SHFlags.SYSICONINDEX | ImageList.SHFlags.USEFILEATTRIBUTES);
                else
                {
                    source = DirectoryHelper.Temp;
                    Local = true;
                }
            }

            if (Local) ImageFactory.Get(ref icon, source, (iconOnly ? ImageFactory.SIIGBF.ICONONLY : 0) | ImageFactory.SIIGBF.RESIZETOFIT);

            Task.Run(() =>
            {
                try { iconsList.Add(name, icon); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            });

            return icon;
        }
    }
}