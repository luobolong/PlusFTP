using System.Windows.Controls;
using System.Windows.Media;

namespace Hani.Utilities
{
    internal static class ProgressBarExtension
    {
        internal static Brush NormalColor;

        internal enum ProgressState : ushort
        {
            Normal = 0,
            Indeterminate = 1,
            Error = 2,
            Paused = 3,
        }

        internal static void SetStateColor(this ProgressBar progressBar, ProgressState state)
        {
            switch (state)
            {
                case ProgressState.Normal:
                    progressBar.Foreground = NormalColor;
                    break;
                case ProgressState.Indeterminate:
                    progressBar.Foreground = SolidColors.Green;
                    break;
                case ProgressState.Error:
                    progressBar.Foreground = SolidColors.Red;
                    break;
                case ProgressState.Paused:
                    progressBar.Foreground = SolidColors.Orange;
                    break;
            }
        }
    }
}