using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void ServerList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!ServerList.IsMouseOver) HideServerToolbar();
        }

        private void ServerList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1: serverGoBack(); break;
                case MouseButton.XButton2: serverGoForward(); break;
            }
        }

        private void ServerList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ServerList.SelectedItems.Clear();
            ServerList.Focus();
        }

        private void ServerList_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5: refreshServerItems(); break;
                case Key.Delete: deleteServerItems(); break;
                case Key.Escape: ServerList.UnselectAll(); break;
            }
        }

        private void ServerList_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.V) && (e.KeyboardDevice.Modifiers == ModifierKeys.Control))
                ServerList_PasteItems();
        }

        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerList.SelectedItems.Count == 0) { HideServerToolbar(); return; }
            SLTRenameItem.IsEnabled = (ServerList.SelectedItems.Count == 1);
        }

        private void ServerList_DragEnter(object sender, DragEventArgs e)
        {
            if (!DragThumb.IsVisible && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                showDragThumb(e.Data);
                e.Handled = true;
            }
        }

        private void ServerList_Drop(object sender, DragEventArgs e)
        {
            if ((draggingFrom != DraggingFrom.ServerList) && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                ClientHelper.TransferItemsAsync(e.Data.GetData(DataFormats.FileDrop), ClientHelper.CurrentPath, true);
                e.Handled = true;
            }
        }

        #region Connect Events

        private void Connecting()
        {
            connecting = true;
            ButtonServerConnect.ToolTip = AppLanguage.Get("LangButtonCancel");
            VisualBrushConnect.Visual = this.GetVisual("appbar_monitor_starting");

            this.Title = AppLanguage.Get("LangTitlePlusFTP") + ' ' + AppLanguage.Get("LangTitleConnectingTo") + ' ' + AppLanguage.RLMARK + "(" + ClientHelper.Server + ")";
            ButtonServerConnect.IsEnabled = true;
        }

        private void Connected()
        {
            connecting = false;
            ButtonServerConnect.ToolTip = AppLanguage.Get("LangButtonDisconnect");
            VisualBrushConnect.Visual = this.GetVisual("appbar_monitor_on");
            this.Title = AppLanguage.Get("LangTitlePlusFTP") + " - " + AppLanguage.RLMARK + "(" + ClientHelper.Server + ")";

            ClientHelper.Counts.Time = new DateTime(0);
        }

        private void Disconnected()
        {
            connecting = false;
            ButtonServerConnect.ToolTip = AppLanguage.Get("LangButtonConnect");
            VisualBrushConnect.Visual = this.GetVisual("appbar_monitor_off");

            this.Title = AppLanguage.Get("LangTitlePlusFTP");
            ClientHelper.Counts.Items = string.Empty;
            Lock();

            SLProgress.Roll(false);
            ButtonServerConnect.IsEnabled = true;
            ButtonServerConnect.Focus();

            ClientHelper.Counts.Time = null;
        }

        #endregion Connect Events
    }
}