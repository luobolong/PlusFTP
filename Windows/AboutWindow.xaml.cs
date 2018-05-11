using System;
using System.Diagnostics;
using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class AboutWindow : MahApps.Metro.Controls.MetroWindow
    {
        internal static void Initialize(Window owner)
        {
            new AboutWindow(owner).ShowDialog();
        }

        private AboutWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
            LabelVersion.Content = AppLanguage.Get("LangLabelVersion").FormatC(AppLanguage.Get("LangTitlePlusFTP"), VersionHelper.AppVersion.ToString());
            HyperlinkHomePage.Inlines.Add(VersionHelper.AppUrlHomePage);
            HyperlinkHomePage.NavigateUri = new Uri(VersionHelper.AppUrlHomePage);
            LabelCopyrightX.Content = DateTime.Now.Year.String();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            setLastVersion();
        }

        private void HyperlinkHomePage_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private async void ButtonCheckForNewVersion_Click(object sender, RoutedEventArgs e)
        {
            ButtonCheckForNewVersion.IsEnabled = false;
            Cursor = System.Windows.Input.Cursors.Wait;
            LabelLatest.Visibility = Visibility.Collapsed;
            LinkDownload.Visibility = Visibility.Collapsed;

            bool? hasChecked = await VersionHelper.Check(true);
            if (hasChecked.HasValue) { setLastVersion(true); }
            else
            {
                MessageWindow.Show(this,
                   AppLanguage.Get("LangMBUpdateFails"),
                   AppLanguage.Get("LangMBCheckingForUpdatesTitle"),
                   MessageBoxButton.OK,
                   MessageBoxImage.Error,
                   MessageBoxResult.OK);
            }

            Cursor = System.Windows.Input.Cursors.Arrow;
            ButtonCheckForNewVersion.IsEnabled = true;
        }

        private void setLastVersion(bool displayVar = false)
        {
            Version latestVar = null;
            if (!Version.TryParse(AppSettings.Get("App", "LatestVersion", VersionHelper.AppVersion.ToString()), out latestVar))
                latestVar = VersionHelper.AppVersion;

            bool newer = (latestVar > VersionHelper.AppVersion);
            if (displayVar || newer)
            {
                LabelLatest.Content = latestVar.ToString();
                LabelLatest.Visibility = Visibility.Visible;

                if (newer)
                {
                    LabelVersion.Foreground = SolidColors.DarkRed;
                    LabelLatest.FontWeight = FontWeights.SemiBold;
                    LinkDownload.Visibility = Visibility.Visible;
                }
                else
                {
                    LabelVersion.Foreground = SolidColors.Black;
                    LabelLatest.FontWeight = FontWeights.Normal;
                    LinkDownload.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void HyperlinkDownload_Click(object sender, RoutedEventArgs e)
        {
            VersionHelper.GetNewApp();
        }
    }
}