using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Hani.Utilities;
using Microsoft.Win32;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void DetailList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            bool enabled = (DetailList.Items.Count > 0);

            MenuItemDetailSelectAll.IsEnabled = enabled;
            MenuItemDetailCopy.IsEnabled = enabled;
            MenuItemDetailSaveAs.IsEnabled = enabled;
            MenuItemDetailClear.IsEnabled = enabled;
        }

        private void MenuItemDetailCopy_Click(object sender, RoutedEventArgs e)
        {
            MessageItem[] items = DetailList.SelectedMessages();
            if (items == null) return;

            StringBuilder sb = new StringBuilder();
            try
            {
                for (int i = 0; i < items.Length; i++)
                    sb.Append(items[i].MText + Environment.NewLine);

                Clipboard.SetText(sb.ToString());
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            sb.Clear();
        }

        private async void MenuItemDetailSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (DetailList.Items.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = AppSettings.Get("Path", "LastSaved", DirectoryHelper.DesktopDirectory);
            sfd.CheckPathExists = true;
            sfd.AddExtension = true;
            sfd.DefaultExt = ".txt";
            sfd.FileName = AppLanguage.Get("LangPlusFTPLog") + " - " + DateFormatHelper.GetShortDateTimeSafe();
            sfd.Filter = AppLanguage.Get("LangTextAllextensions");

            if (!(bool)sfd.ShowDialog() || (sfd.FileName.Trim().Length == 0)) return;

            await Task.Run(() =>
            {
                AppSettings.Set("Path", "LastSaved", DirectoryHelper.GetPath(sfd.FileName));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < AppMessage.Count; i++)
                    try { sb.Append(((sb.Length > 0) ? Environment.NewLine : string.Empty) + AppMessage.Items[i].MText + " - " + AppMessage.Items[i].MDate); }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }

                FileHelper.WriteAll(sfd.FileName, sb.ToString());
                sb.Clear();
            });
        }

        private void MenuItemDetailSelectAll_Click(object sender, RoutedEventArgs e)
        {
            DetailList.SelectAll();
        }

        private void MenuItemDetailClear_Click(object sender, RoutedEventArgs e)
        {
            DetailList.UnselectAll();
            AppMessage.Clear();
        }
    }
}