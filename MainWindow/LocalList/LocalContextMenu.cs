using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void LocalList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            bool enabled = (LocalList.SelectedItems.Count > 0);

            MenuItemLocalDelete.IsEnabled = enabled;
            MenuItemLocalUpload.IsEnabled = enabled && ClientHelper.IsConnected;
            MenuItemLocalRename.IsEnabled = enabled && (LocalList.SelectedItems.Count == 1);
        }

        private void MenuItemLocalUpload_Click(object sender, RoutedEventArgs e)
        {
            SmartItem[] FTPItems = LocalList.SelectedItems();
            if (FTPItems == null) return;

            string[] items = new string[FTPItems.Length];
            Parallel.For(0, FTPItems.Length, i => { items[i] = FTPItems[i].FullName; });
            FTPItems = null;

            ClientHelper.TransferItemsAsync(items, ClientHelper.CurrentPath, true);
        }

        private async void MenuItemLocalRefresh_Click(object sender, RoutedEventArgs e)
        {
            await setLocalList(LocalHelper.CurrentPath);
        }

        private async void MenuItemLocalNewFolder_Click(object sender, RoutedEventArgs e)
        {
            await NewFolderWindow.New(this, true);
        }

        private void MenuItemLocalRename_Click(object sender, RoutedEventArgs e)
        {
            SmartItem item = LocalList.SelectedItem();
            if (item == null) return;

            RenameWindow.Rename(this, item, true);
        }

        private void MenuItemLocalDelete_Click(object sender, RoutedEventArgs e)
        {
            deleteLocalItems();
        }

        private void MenuItemLocalSelectAll_Click(object sender, RoutedEventArgs e)
        {
            LocalList.SelectAll();
        }
    }
}