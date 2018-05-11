using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private static readonly GridLength DLTAniTo = new GridLength(21);
        private static readonly GridLength DLAniFrom = new GridLength(160);
        private static GridLengthAnimation DetailListAnimation = new GridLengthAnimation();

        private void ButtonHideDetailList_Click(object sender, RoutedEventArgs e)
        {
            DetailListAnimation.Duration = timeSpan3;

            ThicknessAnimation HideButtonMargin = new ThicknessAnimation();
            HideButtonMargin.Duration = timeSpan3;

            if (AppMessage.IsVisible)
            {
                DetailListAnimation.From = DLAniFrom;
                DetailListAnimation.To = DLTAniTo;

                VisualBrushHideDetailList.Visual = this.GetVisual("appbar_chevron_up");
                ButtonHideDetailList.ToolTip = AppLanguage.Get("LangButtonShowDetailList");
                AppMessage.IsVisible = false;
            }
            else
            {
                DetailListAnimation.From = DLTAniTo;
                DetailListAnimation.To = DLAniFrom;
                VisualBrushHideDetailList.Visual = this.GetVisual("appbar_chevron_down");
                ButtonHideDetailList.ToolTip = AppLanguage.Get("LangButtonHideDetailList");
                AppMessage.IsVisible = true;
            }
            AppSettings.Set("DetailList", "Minimized", !AppMessage.IsVisible);

            DetailListRow.BeginAnimation(RowDefinition.HeightProperty, DetailListAnimation);
            ButtonHideDetailList.BeginAnimation(Button.MarginProperty, HideButtonMargin);
            //VisualBrushHideDetailList
        }
    }
}