using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace Hani.Utilities
{
    internal static class TaskbarHelper
    {
        internal static TaskbarItemInfo MainTaskbar { get; set; }

        private static IList<ProgressBar> taskbars;

        static TaskbarHelper()
        {
            _set();
        }

        private static void _set()
        {
            taskbars = new List<ProgressBar>(5);
        }

        internal static void Add(ProgressBar taskbar)
        {
            taskbars.Add(taskbar);
            subscribe(taskbar);
        }

        internal static void Remove(ProgressBar taskbar)
        {
            taskbars.Remove(taskbar);
            if (taskbars.Count == 0) MainTaskbar.ProgressValue = 0.0;
        }

        private static void subscribe(ProgressBar taskbar)
        {
            taskbar.ValueChanged += changed;
        }

        private static void changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = 0.0;

            for (int i = 0; i < taskbars.Count; i++)
            {
                if (taskbars[i] == null) Remove(taskbars[i]);
                else value += taskbars[i].Value / taskbars[i].Maximum;
            }

            MainTaskbar.ProgressValue = value / taskbars.Count;
        }
    }
}