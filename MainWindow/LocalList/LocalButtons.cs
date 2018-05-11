using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private async void TextBoxLocalPath_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) && (TextBoxLocalPath.Text != LocalHelper.CurrentPath))
                await setLocalList(TextBoxLocalPath.Text);
        }

        private async void ButtonLocalRefresh_Click(object sender, RoutedEventArgs e)
        {
            await setLocalList(LocalHelper.CurrentPath);
        }

        private async void ButtonLocalHome_Click(object sender, RoutedEventArgs e)
        {
            await setLocalList(LocalHelper.Home);
        }

        private void ButtonLocalBack_Click(object sender, RoutedEventArgs e)
        {
            localGoBack();
        }

        private void ButtonLocalForward_Click(object sender, RoutedEventArgs e)
        {
            localGoForward();
        }

        private async void ButtonLocalUp_Click(object sender, RoutedEventArgs e)
        {
            await setLocalList(LocalHelper.ParentPath);
        }

        private void ButtonLocalBackward_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            string peek = LocalBackForthStack.PeekBack();
            if (!peek.NullEmpty()) ButtonLocalBack.ToolTip = AppLanguage.Get("LangTextBackToX").FormatC(peek);
            else ButtonLocalBack.ToolTip = AppLanguage.Get("LangTextBack");
        }

        private void ButtonLocalForward_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            string peek = LocalBackForthStack.PeekForth();
            if (!peek.NullEmpty()) ButtonLocalForward.ToolTip = AppLanguage.Get("LangTextForwardToX").FormatC(peek);
            else ButtonLocalForward.ToolTip = AppLanguage.Get("LangTextForward");
        }
    }
}