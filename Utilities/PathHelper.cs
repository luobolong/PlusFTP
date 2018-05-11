namespace Hani.Utilities
{
    internal static class PathHelper
    {
        internal static void AddEndningSlash(ref string path)
        {
            if (!path.Ends("/")) path += "/";
        }
    }
}