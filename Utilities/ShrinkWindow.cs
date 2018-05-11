using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace Hani.Utilities
{
    internal class ShrinkWindow
    {
        internal bool Shrinked { get; private set; }

        private Window Owner;
        private MetroWindow Window;
        private UIElement ToHide;
        private UIElement ToShow;

        private double minWidth;
        private double minHeight;
        private double actualWidth;
        private double actualHeight;
        private SolidColorBrush glowBrush;
        private Brush background;
        private WindowStyle windowStyle;
        private ResizeMode resizeMode;
        private SizeToContent sizeToContent;
        private bool showCloseButton;
        private bool showIconOnTitleBar;
        private bool showMaxRestoreButton;
        private bool showMinButton;
        private bool showTitleBar;
        private bool topmost;

        internal ShrinkWindow(MetroWindow window, UIElement hide, UIElement show)
        {
            Window = window;
            ToHide = hide;
            ToShow = show;
        }

        internal void Do()
        {
            store();

            Window.Owner = null;

            Window.MinHeight = 0;
            Window.MinWidth = 0;
            Window.GlowBrush = null;
            Window.Background = Brushes.Transparent;
            Window.WindowStyle = WindowStyle.None;
            Window.ResizeMode = ResizeMode.NoResize;
            Window.ShowCloseButton = false;
            Window.ShowIconOnTitleBar = false;
            Window.ShowMaxRestoreButton = false;
            Window.ShowMinButton = false;
            Window.ShowTitleBar = false;
            Window.Topmost = true;

            ToHide.Visibility = Visibility.Collapsed;
            ToShow.Visibility = Visibility.Visible;
            Window.SizeToContent = SizeToContent.WidthAndHeight;

            Window.Left = (Window.Left + (actualWidth / 2)) - (Window.ActualWidth / 2);
            Window.Top = (Window.Top + (actualHeight / 2)) - (Window.ActualHeight / 2);

            Shrinked = true;
        }

        internal void Undo()
        {
            double left = (Window.Left - (actualWidth / 2)) + Window.ActualWidth / 2;
            double top = (Window.Top - (actualHeight / 2)) + Window.ActualHeight / 2;

            if (left < 0) left = 0;
            else if ((left + Window.ActualWidth) > SystemParameters.WorkArea.Width)
                left = SystemParameters.WorkArea.Width - Window.ActualWidth;

            if (top < 0) top = 0;
            else if ((top + Window.ActualHeight) > SystemParameters.WorkArea.Height)
                top = SystemParameters.WorkArea.Height - Window.ActualHeight;

            Window.Left = left;
            Window.Top = top;

            Window.Owner = Owner;

            ToHide.Visibility = Visibility.Visible;
            ToShow.Visibility = Visibility.Collapsed;

            Window.GlowBrush = glowBrush;
            Window.Background = background;
            Window.WindowStyle = windowStyle;
            Window.ResizeMode = resizeMode;
            Window.SizeToContent = sizeToContent;
            Window.ShowCloseButton = showCloseButton;
            Window.ShowIconOnTitleBar = showIconOnTitleBar;
            Window.ShowMaxRestoreButton = showMaxRestoreButton;
            Window.ShowMinButton = showMinButton;
            Window.ShowTitleBar = showTitleBar;

            Window.Topmost = topmost;

            Window.MinHeight = minHeight;
            Window.MinWidth = minWidth;
            Window.Height = actualHeight;
            Window.Width = actualWidth;

            Shrinked = false;
        }

        private void store()
        {
            Owner = Window.Owner;

            actualWidth = Window.ActualWidth;
            actualHeight = Window.ActualHeight;
            minWidth = Window.MinWidth;
            minHeight = Window.MinHeight;
            glowBrush = Window.GlowBrush;
            background = Window.Background;
            windowStyle = Window.WindowStyle;
            resizeMode = Window.ResizeMode;
            sizeToContent = Window.SizeToContent;
            showCloseButton = Window.ShowCloseButton;
            showIconOnTitleBar = Window.ShowIconOnTitleBar;
            showMaxRestoreButton = Window.ShowMaxRestoreButton;
            showMinButton = Window.ShowMinButton;
            showTitleBar = Window.ShowTitleBar;
            topmost = Window.Topmost;
        }
    }
}