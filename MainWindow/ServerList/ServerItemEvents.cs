using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void ServerItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            if (ServerList.SelectedItems.Count > 1) e.Handled = true;
        }

        private void ServerItem_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.LeftButton != MouseButtonState.Pressed) || (ServerList.SelectedItems.Count == 0)) return;

            SmartItem[] items = ServerList.SelectedItems(ClientHelper.Items);
            if (items == null) return;
            CachedItems = items;

            e.Handled = true;
            draggingFrom = DraggingFrom.ServerList;

            if (DragWatcher.Start())
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, new string[] { DirectoryHelper.Temp + DragWatcher.Source }, true), DragDropEffects.Copy);
            //else DragDrop.DoDragDrop(this, new DataObject("ServerItems", cachedItems), DragDropEffects.Copy);
        }

        private void ServerItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            goToServerItem();
        }

        private void ServerItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (ServerList.SelectedItems.Count == 1)
            {
                if (e.Key == Key.Enter) goToServerItem();
                else if (e.Key == Key.F2) MenuItemServerRename_Click(sender, e);
            }
        }

        private void ServerItem_Selected(object sender, RoutedEventArgs e)
        {
            showServerItemsToolbar(e.Source as ListViewItem);
        }
    }
}