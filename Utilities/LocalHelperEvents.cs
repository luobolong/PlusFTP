using System;

namespace Hani.Utilities
{
    internal static partial class LocalHelper
    {
        internal delegate void DirectoryHandler(object sender, EventArgs e);
        internal static event DirectoryHandler OnListChanged;

        internal static void ListChanged(object sender, EventArgs e)
        {
            if (OnListChanged != null) new DirectoryHandler(OnListChanged)(sender, e);
        }
    }
}