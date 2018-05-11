using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Hani.Utilities
{
    internal static class VisualHelper
    {
        private static Dictionary<string, Visual> visualList;

        static VisualHelper()
        {
            _set();
        }

        private static void _set()
        {
            visualList = new Dictionary<string, Visual>();
        }

        internal static Visual GetVisual(this FrameworkElement Owner, string name)
        {
            if (visualList.ContainsKey(name)) return visualList[name];

            Visual visual = Owner.Resources[name] as Visual;

            Task.Run(() =>
            {
                try { visualList.Add(name, visual); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            });

            return visual;
        }
    }
}