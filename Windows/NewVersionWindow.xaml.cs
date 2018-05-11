using System;
using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class NewVersionWindow : MahApps.Metro.Controls.MetroWindow
    {
        private static string currentVersion;

        static NewVersionWindow()
        {
            _set();
        }

        private static void _set()
        {
            currentVersion = AppLanguage.Get("LangTextBlockCurrentVersion_x").FormatC(VersionHelper.AppVersion);
        }

        internal static void Initialize(Window owner)
        {
            new NewVersionWindow(owner).ShowDialog();
        }

        private NewVersionWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
            TextBlockCurrentVersion.Text = currentVersion;
            TextBlockNewVersion.Text = AppLanguage.Get("LangTextBlockNewVersion_x").FormatC(VersionHelper.LatestVersion.ToString());
            CheckBoxCheckVersion.IsChecked = AppSettings.Get("App", "CheckVersion", true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBoxCheckVersion.Click += new EventHandler<RoutedEventArgs>(CheckBoxCheckVersion_Click);
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            VersionHelper.GetNewApp();
            this.Close();
        }

        private void CheckBoxCheckVersion_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Set("App", "CheckVersion", (bool)CheckBoxCheckVersion.IsChecked);
        }
    }
}