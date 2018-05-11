using System.Windows;
using MahApps.Metro.Controls;

namespace Hani.Utilities
{
    internal static class ProgressRingExtension
    {
        internal static void Roll(this ProgressRing pr, bool value)
        {
            if (value == true)
            {
                pr.Visibility = Visibility.Visible;
                pr.IsActive = true;
            }
            else
            {
                pr.Visibility = Visibility.Collapsed;
                pr.IsActive = false;
            }
        }
    }
}