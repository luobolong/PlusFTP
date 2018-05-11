using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private static readonly Duration timeSpan3 = new Duration(TimeSpan.FromSeconds(0.3));
        private static readonly Duration timeSpan2 = new Duration(TimeSpan.FromSeconds(0.2));
        private static readonly DoubleAnimation hideServerToolbarAnimation = new DoubleAnimation(0.0, timeSpan2);
        private static readonly DoubleAnimation ServerItemsToolbarAnimation = new DoubleAnimation(0.1, timeSpan3);

        private async void showServerItemsToolbar(ListViewItem item)
        {
            await Task.Delay(200);
            if (!ServerList.IsEnabled) { HideServerToolbar(); return; }
            if ((item == null) || !item.IsSelected || !item.IsLoaded) { HideServerToolbar(); return; }

            if (columnNameWidth == 0.0) columnNameWidth = (ServerList.View as GridView).Columns[0].ActualWidth + 5;
            Thickness newPosition = new Thickness(columnNameWidth, item.TranslatePoint(new Point(0, 0), ServerList).Y + 4.5, 0, 0);

            if (ServerItemsToolbar.Visibility != Visibility.Visible)
            {
                ServerItemsToolbar.BeginAnimation(Border.MarginProperty, new ThicknessAnimation(newPosition, new Duration(TimeSpan.Zero)));
                if (ServerItemsToolbar.Opacity == 0) ServerItemsToolbar.BeginAnimation(Border.OpacityProperty, new DoubleAnimation(1, timeSpan2));
                ServerItemsToolbar.Visibility = Visibility.Visible;
            }
            else
                ServerItemsToolbar.BeginAnimation(Border.MarginProperty, new ThicknessAnimation(newPosition, timeSpan2));

            if (ServerItemsToolbar.Opacity == 0) ServerItemsToolbar.BeginAnimation(Border.OpacityProperty, new DoubleAnimation(1, timeSpan3));
        }

        private void WidthPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActualWidth")
            {
                columnNameWidth = (ServerList.View as GridView).Columns[0].ActualWidth + 5;
                Thickness newPosition = new Thickness(columnNameWidth, ServerItemsToolbar.Margin.Top, 0, 0);

                if (ServerItemsToolbar.Visibility != Visibility.Visible)
                    ServerItemsToolbar.BeginAnimation(Border.MarginProperty, new ThicknessAnimation(newPosition, new Duration(TimeSpan.Zero)));
                else ServerItemsToolbar.BeginAnimation(Border.MarginProperty, new ThicknessAnimation(newPosition, timeSpan2));
            }
        }

        private void HideServerToolbar()
        {
            if (ServerItemsToolbar.Opacity != 0) ServerItemsToolbar.BeginAnimation(Border.OpacityProperty, hideServerToolbarAnimation);
        }

        private void hideServerToolbarAnimationCompleted(object sender, EventArgs e)
        {
            ServerItemsToolbar.Visibility = Visibility.Collapsed;
        }
    }
}