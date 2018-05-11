using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void TextBoxHostPath_KeyUp(object sender, KeyEventArgs e)
        {
            if (!TextBoxHostPath.Text.Trim().NullEmpty() && (TextBoxHostPath.Text != ClientHelper.CurrentPath))
            {
                ButtonServerPathGo.Visibility = Visibility.Visible;
                if (e.Key == Key.Enter) goToServerPath(TextBoxHostPath.Text);
            }
            else ButtonServerPathGo.Visibility = Visibility.Collapsed;
        }

        private void TextBoxHostPath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string path = TextBoxHostPath.SelectedItem as string;
            if ((path != null) && (path != ClientHelper.CurrentPath))
                ButtonServerPathGo.Visibility = Visibility.Visible;
            else ButtonServerPathGo.Visibility = Visibility.Collapsed;
        }

        private void ButtonServerPathGo_Click(object sender, RoutedEventArgs e)
        {
            goToServerPath(TextBoxHostPath.Text);
        }

        private async void ButtonServerConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ClientHelper.IsConnected || connecting)
            {
                if (!connecting && MessageWindow.Show(this,
                AppLanguage.Get("LangMBDisconnect").FormatC(ClientHelper.Server),
                AppLanguage.Get("LangMBDisconnectTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

                ButtonServerConnect.IsEnabled = false;
                TextBoxHostPath.Text = string.Empty;
                TextBoxHostPath.Items.Clear();
                ServerBackForthStack.Clear();

                await ClientHelper.DisconnectAsync(true);
            }
            else if (ConnectWindow.OK(this))
            {
                ButtonServerConnect.IsEnabled = false;
                AppMessage.Clear();

                if (await ClientHelper.ConnectAsync())
                {
                    await setServerList(ClientHelper.Home);
                    SLTChangePermission.IsEnabled = ClientHelper.Client.IsUnix.HasValue && ClientHelper.Client.IsUnix.Value;
                    ClientHelper.SetSecondaryClient();
                }
            }
        }

        private void ButtonServerRefresh_Click(object sender, RoutedEventArgs e)
        {
            refreshServerItems();
        }

        private async void ButtonServerHome_Click(object sender, RoutedEventArgs e)
        {
            await setServerList(ClientHelper.Home);
        }

        private void ButtonServerBack_Click(object sender, RoutedEventArgs e)
        {
            serverGoBack();
        }

        private void ButtonServerForward_Click(object sender, RoutedEventArgs e)
        {
            serverGoForward();
        }

        private async void ButtonServerUp_Click(object sender, RoutedEventArgs e)
        {
            await setServerList(ClientHelper.Up());
        }

        private void ButtonServerHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow.Initialize(this);
        }

        private void TextBoxHostSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchServerItems();
        }

        private void ButtonServerBack_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            string peek = ServerBackForthStack.PeekBack();
            if (!peek.NullEmpty()) ButtonServerBack.ToolTip = AppLanguage.Get("LangTextBackToX").FormatC(peek);
            else ButtonServerBack.ToolTip = AppLanguage.Get("LangTextBack");
        }

        private void ButtonServerForward_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            string peek = ServerBackForthStack.PeekForth();
            if (!peek.NullEmpty()) ButtonServerForward.ToolTip = AppLanguage.Get("LangTextForwardToX").FormatC(peek);
            else ButtonServerForward.ToolTip = AppLanguage.Get("LangTextForward");
        }
    }
}