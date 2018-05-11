using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private static ICollectionView serverListView;

        private bool connecting = false, Locked = true;

        private async Task setServerList(string path)
        {
            if (!ClientHelper.IsConnected || path.NullEmpty()) return;
            Lock();

            if (await ClientHelper.SetItemsAsync(path))
            {
                if (ClientHelper.CurrentPath != ClientHelper.LastPath)
                {
                    ServerBackForthStack.Save(ClientHelper.LastPath, LocalHelper.CurrentPath);
                    ButtonServerPathGo.Visibility = Visibility.Collapsed;
                }

                if (!TextBoxHostPath.Items.Contains(ClientHelper.CurrentPath))
                    TextBoxHostPath.Items.Add(ClientHelper.CurrentPath);
                TextBoxHostPath.SelectedItem = ClientHelper.CurrentPath;

                ServerList.UnselectAll();

                if (ClientHelper.Items.Count > 0)
                {
                    ServerList.ScrollIntoView(ClientHelper.Items[0]);
                    MenuItemServerSelectAll.IsEnabled = true;
                    MenuItemServerToBrowser.IsEnabled = SLTToBrowser.IsEnabled = (ClientHelper.CurrentPath.Contains("public_html") || ClientHelper.CurrentPath.Contains("www"));
                }
                else MenuItemServerToBrowser.IsEnabled = MenuItemServerSelectAll.IsEnabled = false;
            }

            ServerList.Focus();
            ServerList.ItemsSource = ClientHelper.Items;
            serverListView = CollectionViewSource.GetDefaultView(ServerList.ItemsSource);
            if (TextBoxHostSearch.Text.Trim().Length != 0) searchServerItems();
            UnLock();
            ServerList.UnselectAll();
            GC.Collect();
        }

        private void Lock()
        {
            if (Locked) return; // Look at Locked && ClientWrapper.Connected
            Locked = true;

            if (ClientHelper.IsConnected) Task.Run(async delegate
            {
                await Task.Delay(1500);
                if (Locked && ClientHelper.IsConnected) Dispatcher.Invoke((Action)(() => { SLProgress.Roll(!ServerList.IsEnabled); }));
            });

            GroupBoxServerFiles.Cursor = ClientHelper.IsConnected ? Cursors.Wait : Cursors.Arrow;
            ServerList.IsEnabled = false;
            ButtonServerUp.IsEnabled = false;
            ButtonServerBack.IsEnabled = false;
            ButtonServerForward.IsEnabled = false;
            ButtonServerHome.IsEnabled = false;
        }

        internal void UnLock()
        {
            if (!ClientHelper.IsConnected || !Locked) return;

            GroupBoxServerFiles.Cursor = Cursors.Arrow;
            ServerList.IsEnabled = true;
            SLProgress.Roll(false);

            ButtonServerBack.IsEnabled = ServerBackForthStack.CanBack;
            ButtonServerForward.IsEnabled = ServerBackForthStack.CanForth;
            ButtonServerHome.IsEnabled = ButtonServerUp.IsEnabled = (ClientHelper.Home != ClientHelper.CurrentPath);

            Locked = false;
        }

        private void searchServerItems()
        {
            serverListView.Filter = new Predicate<object>(searchServerItemName);
        }

        private void goToServerItem()
        {
            SmartItem item = ServerList.SelectedItem();
            if ((item != null) && !item.IsFile) goToServerPath(ClientHelper.CurrentPath + item.ItemName);
        }

        private async void goToServerPath(string path)
        {
            await setServerList(path);
        }

        private static void ServerList_PasteItems()
        {
            if (Clipboard.ContainsData(DataFormats.FileDrop))
                ClientHelper.TransferItemsAsync(Clipboard.GetData(DataFormats.FileDrop), ClientHelper.CurrentPath, true);
        }

        private async void refreshServerItems()
        {
            if (!ClientHelper.IsConnected) return;

            ClientHelper.ClearCachedPath(ClientHelper.CurrentPath);
            await setServerList(ClientHelper.CurrentPath);
        }

        private void deleteServerItems()
        {
            SmartItem[] items = ServerList.SelectedItems(ClientHelper.Items);
            if (items == null) return;

            if (MessageWindow.Show(this,
                AppLanguage.Get("LangMBDelete"),
                AppLanguage.Get("LangMBDeleteTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

            ClientHelper.DeleteAsync(items);
        }

        private bool searchServerItemName(object obj)
        {
            string search = TextBoxHostSearch.Text.Trim().Lower();
            if (search.Length == 0) return true; // the filter is empty - pass all items

            SmartItem item = obj as SmartItem;
            if (item == null) return true;

            if (item.ItemName.Lower().Contains(search)) return true;
            return false;
        }

        private async void serverGoBack()
        {
            ButtonServerBack.IsEnabled = false;
            await setServerList(ServerBackForthStack.Back());
        }

        private async void serverGoForward()
        {
            ButtonServerForward.IsEnabled = false;
            await setServerList(ServerBackForthStack.Forth());
        }
    }
}