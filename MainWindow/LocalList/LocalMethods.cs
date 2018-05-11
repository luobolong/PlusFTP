using System;
using System.Threading.Tasks;
using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private bool isLocalListUpdating = false;

        private async Task setLocalList(string path)
        {
            LocalList.IsEnabled = false;

            if (await LocalHelper.SetItemsAsync(path))
            {
                if (LocalHelper.CurrentPath != LocalHelper.LastPath)
                {
                    TextBoxLocalPath.Text = LocalHelper.CurrentPath;
                    LocalBackForthStack.Save(LocalHelper.LastPath, LocalHelper.CurrentPath);
                }

                ButtonLocalBack.IsEnabled = LocalBackForthStack.CanBack;
                ButtonLocalForward.IsEnabled = LocalBackForthStack.CanForth;
                ButtonLocalHome.IsEnabled = (path != LocalHelper.Home);
                ButtonLocalUp.IsEnabled = !LocalHelper.ParentPath.NullEmpty();

                LocalList.UnselectAll();
            }

            LocalList.Focus();
            LocalList.ItemsSource = LocalHelper.Items;
            if (LocalHelper.Items.Count > 0)
            {
                MenuItemLocalSelectAll.IsEnabled = true;
                LocalList.ScrollIntoView(LocalHelper.Items[0]);
            }

            LocalList.UnselectAll();
            LocalList.IsEnabled = true;

            GC.Collect();
        }

        private async void goToLocalItem()
        {
            SmartItem item = LocalList.SelectedItem();

            if (item == null) return;

            if (item.IsFile)
            {
                if (item.Extension == "lnk")
                {
                    string path = ShellLinkHelper.GetPath(item.FullName);
                    if ((path != null) && DirectoryHelper.Exists(path)) await setLocalList(path);
                    else
                    {
                        try { System.Diagnostics.Process.Start(item.FullName); }
                        catch (Exception exp) { ExceptionHelper.Log(exp); }
                    }
                }
            }
            else await setLocalList(item.FullName);
        }

        private async void localListChanged(object sender, EventArgs e)
        {
            if (isLocalListUpdating) while (isLocalListUpdating) { await Task.Delay(200); }

            isLocalListUpdating = true;
            await Dispatcher.Invoke(async delegate { await setLocalList(LocalHelper.CurrentPath); isLocalListUpdating = false; });
        }

        private void deleteLocalItems()
        {
            if (LocalList.SelectedItems.Count == 0) return;

            if (MessageWindow.Show(this,
                AppLanguage.Get("LangMBDelete"),
                AppLanguage.Get("LangMBDeleteTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

            SmartItem[] items = LocalList.SelectedItems();
            if (items == null) return;

            LocalHelper.Delete(items);
        }

        private async void localGoBack()
        {
            await setLocalList(LocalBackForthStack.Back());
        }

        private async void localGoForward()
        {
            await setLocalList(LocalBackForthStack.Forth());
        }
    }
}