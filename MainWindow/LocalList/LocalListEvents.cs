using System.Windows;
using System.Windows.Input;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void LocalList_GotFocus(object sender, RoutedEventArgs e)
        {
            LocalList.SelectedItems.Clear();
            LocalList.Focus();
        }

        private void LocalList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1: localGoBack(); break;
                case MouseButton.XButton2: localGoForward(); break;
            }
        }

        private async void LocalList_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5: await setLocalList(LocalHelper.CurrentPath); break;
                case Key.Delete: deleteLocalItems(); break;
                case Key.Escape: LocalList.UnselectAll(); break;
            }
        }

        private void LocalList_DragEnter(object sender, DragEventArgs e)
        {
            if (!DragThumb.IsVisible && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                showDragThumb(e.Data);
                e.Handled = true;
            }
        }

        private void LocalList_Drop(object sender, DragEventArgs e)
        {
            if (draggingFrom == DraggingFrom.LocalList) return;

            if (e.Data.GetDataPresent("ServerItems"))
                ClientHelper.TransferItemsAsync(e.Data.GetData("ServerItems"), LocalHelper.CurrentPath, false);
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] items = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (items[0].Ends(DragWatcher.Source))
                {
                    ClientHelper.TransferItemsAsync(CachedItems, LocalHelper.CurrentPath, false);
                    e.Handled = true;
                }

                /*for (int i = 0; i < items.Length; i++)
                    if (FileHelper.Exists(items[i])) File.Copy(items[i], DirectoryHelper.CurrentPath + @"\" + (new FileInfo(items[i])).Name);*/
            }
        }
    }
}