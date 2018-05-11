using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Hani.Utilities;
using Microsoft.Win32;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void ServerList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            HideServerToolbar();
            bool enabled = (ServerList.SelectedItems.Count > 0);

            MenuItemServerDownload.IsEnabled = enabled;
            MenuItemServerPaste.IsEnabled = Clipboard.ContainsData(DataFormats.FileDrop);
            MenuItemServerRename.IsEnabled = enabled && SLTRenameItem.IsEnabled;
            MenuItemServerCopyPath.IsEnabled = enabled;
            MenuItemServerPermission.IsEnabled = enabled && SLTChangePermission.IsEnabled;
            MenuItemServerMove.IsEnabled = enabled;
            MenuItemServerDelete.IsEnabled = enabled;
        }

        private void MenuItemServerDownload_Click(object sender, RoutedEventArgs e)
        {
            HideServerToolbar();
            SmartItem[] ritems = ServerList.SelectedItems(ClientHelper.Items);
            if (ritems == null) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = AppSettings.Get("Path", "LastDownload", DirectoryHelper.DesktopDirectory);
            sfd.CheckFileExists = false;
            sfd.OverwritePrompt = false;
            sfd.FileName = ritems[0].ItemName;

            if ((bool)sfd.ShowDialog())
            {
                string path = DirectoryHelper.GetPath(sfd.FileName);
                AppSettings.Set("Path", "LastDownload", path);
                if (!path.NullEmpty()) ClientHelper.TransferItemsAsync(ritems, path, false);
            }
        }

        private void MenuItemServerRefresh_Click(object sender, RoutedEventArgs e)
        {
            refreshServerItems();
        }

        private void MenuItemServerPaste_Click(object sender, RoutedEventArgs e)
        {
            ServerList_PasteItems();
        }

        private async void MenuItemServerNewFolder_Click(object sender, RoutedEventArgs e)
        {
            ServerList.UnselectAll();
            string newFolder = await NewFolderWindow.New(this, false);

            if (!newFolder.NullEmpty())
            {
                SmartItem item = ClientHelper.GetItem(newFolder);
                if (item != null)
                {
                    ServerList.Focus();
                    ServerList.SelectedItem = item;
                }
            }
        }

        private void MenuItemServerRename_Click(object sender, RoutedEventArgs e)
        {
            SmartItem item = ServerList.SelectedItem();
            if (item == null) return;

            RenameWindow.Rename(this, item, false);
        }

        private void MenuItemServerToBrowser_Click(object sender, RoutedEventArgs e)
        {
            SmartItem[] items = ServerList.SelectedItems(ClientHelper.Items);
            if (items == null) return;

            string uRL = null, host = null;
            try { host = "http://" + Regex.Replace(ClientHelper.Server, @"^(www|ftp)\.", "", RegexOptions.Compiled); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            try { uRL = Regex.Replace(ClientHelper.CurrentPath, "^/(public_html|www)/(.*)", host + @"/$2", RegexOptions.IgnoreCase | RegexOptions.Compiled); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            if ((host == null) || (uRL == null)) return;

            for (int i = 0; i < items.Length; i++)
            {
                try
                {
                    string url = uRL + items[i].ItemName.Replace(" ", "%20");
                    System.Diagnostics.Process.Start(url);
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }
        }

        private void MenuItemServerCopyPath_Click(object sender, RoutedEventArgs e)
        {
            SmartItem[] items = ServerList.SelectedItems(ClientHelper.Items);
            if (items == null) return;

            string paths = string.Empty;
            for (int i = 0; i < items.Length; i++)
                paths += ClientHelper.CurrentPath + items[i].ItemName + Environment.NewLine;

            Clipboard.SetText(paths.Trim());
        }

        private void MenuItemServerPermission_Click(object sender, RoutedEventArgs e)
        {
            SmartItem[] items = ServerList.SelectedItems(ClientHelper.Items);
            if (items == null) return;

            PermissionsWindow.Initialize(this, items);
        }

        private void MenuItemServerMove_Click(object sender, RoutedEventArgs e)
        {
            SmartItem[] items = ServerList.SelectedItems(ClientHelper.Items);
            if (items == null) return;
            BrowseServerWindow.Initialize(this, items);
        }

        private void MenuItemServerDelete_Click(object sender, RoutedEventArgs e)
        {
            deleteServerItems();
        }

        private void MenuItemServerSelectAll_Click(object sender, RoutedEventArgs e)
        {
            ServerList.SelectAll();
        }
    }
}