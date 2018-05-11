using System;
using System.Windows;
using System.Windows.Controls;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class OptionsWindow : MahApps.Metro.Controls.MetroWindow
    {
        internal static void Initialize(Window owner)
        {
            new OptionsWindow(owner).ShowDialog();
        }

        private OptionsWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBoxLanguage.SelectedIndex = AppLanguage.LanguageID;
            CheckBoxSaveSameLocation.IsChecked = AppSettings.SameAppPath;
            CheckBoxMultiConnection.IsChecked = ClientHelper.MultiConnection;
            CheckBoxUTF8.IsChecked = AppSettings.Get("FTP", "UTF8", true);
            CheckBoxMODEZ.IsChecked = AppSettings.Get("FTP", "MODEZ", true);
            CheckBoxCache.IsChecked = AppSettings.Get("FTP", "Cache", true);
            CheckBoxCheckVersion.IsChecked = AppSettings.Get("App", "CheckVersion", true);
            CheckBoxEnableProxy.IsChecked = AppSettings.Get("Proxy", "Enable", false);

            ComboBoxDefaultTransferBehaved.SelectedIndex = AppSettings.Get("FTP", "UploadBehavior", 0);
            ComboBoxDefaultFishedBehaved.SelectedIndex = AppSettings.Get("FTP", "FishedBehavior", 0);

            TextBoxProxyServer.Text = AppSettings.Get("Proxy", "Server", "");
            TextBoxProxyPort.Text = AppSettings.Get("Proxy", "Port", "8080");

            ComboBoxLanguage.SelectionChanged += new SelectionChangedEventHandler(ComboBoxLanguage_SelectionChanged);
            ComboBoxDefaultTransferBehaved.SelectionChanged += new SelectionChangedEventHandler(ComboBoxDefaultTransferBehaved_SelectionChanged);
            ComboBoxDefaultFishedBehaved.SelectionChanged += new SelectionChangedEventHandler(ComboBoxDefaultFishedBehaved_SelectionChanged);

            CheckBoxMultiConnection.Click += delegate { AppSettings.Set("App", "MultiConnection", (bool)CheckBoxMultiConnection.IsChecked); ClientHelper.MultiConnection = (bool)CheckBoxMultiConnection.IsChecked; };
            CheckBoxUTF8.Click += delegate { AppSettings.Set("FTP", "UTF8", (bool)CheckBoxUTF8.IsChecked); };
            CheckBoxMODEZ.Click += delegate { AppSettings.Set("FTP", "MODEZ", (bool)CheckBoxMODEZ.IsChecked); };
            CheckBoxCheckVersion.Click += delegate { AppSettings.Set("App", "CheckVersion", (bool)CheckBoxCheckVersion.IsChecked); };
            CheckBoxCache.Click += delegate { AppSettings.Set("FTP", "Cache", (bool)CheckBoxCache.IsChecked); NetworkClient.CacheFolders = (bool)CheckBoxCache.IsChecked; };

            CheckBoxEnableProxy.Click += new EventHandler<RoutedEventArgs>(CheckBoxEnableProxy_Click);

            CheckBoxSaveSameLocation.Checked += new EventHandler<RoutedEventArgs>(CheckBoxSaveSameLocation_Checked);
            CheckBoxSaveSameLocation.Unchecked += new EventHandler<RoutedEventArgs>(CheckBoxSaveSameLocation_Unchecked);
        }

        private async void Window_Closed(object sender, System.EventArgs e)
        {
            if ((bool)CheckBoxEnableProxy.IsChecked)
            {
                AppSettings.Set("Proxy", "Server", TextBoxProxyServer.Text);
                AppSettings.Set("Proxy", "Port", TextBoxProxyPort.Text);
            }

            await AppSettings.Save();
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppLanguage.LanguageCode = (ComboBoxLanguage.SelectedItem as ListBoxItem).Tag.ToString();
            AppLanguage.SetCurrentThreadCulture(System.Threading.Thread.CurrentThread);
            AppLanguage.Save();

            Close();

            Initialize(this.Owner);
        }

        private void ComboBoxDefaultTransferBehaved_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppSettings.Set("FTP", "UploadBehavior", ComboBoxDefaultTransferBehaved.SelectedIndex);
            TransferEvents.TillCloseAction = TransferAction.Unknown;
        }

        private void ComboBoxDefaultFishedBehaved_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppSettings.Set("FTP", "FishedBehavior", ComboBoxDefaultFishedBehaved.SelectedIndex);
        }

        private void CheckBoxSaveSameLocation_Checked(object sender, RoutedEventArgs e)
        {
            if (DirectoryHelper.Exists(AppSettings.RoamingPath))
            {
                string[] appPaths = new string[] { AppSettings.FileAppPath, Users.FileAppPath, AppHistory.FileAppPath };
                string[] roamingPaths = new string[] { AppSettings.FileRoamingPath, Users.FileRoamingPath, AppHistory.FileRoamingPath };

                for (int i = 0; i < roamingPaths.Length; i++) FileHelper.Move(roamingPaths[i], appPaths[i]);

                DirectoryHelper.Delete(AppSettings.RoamingPath);
            }

            AppSettings.FilePath = AppSettings.FileAppPath;
            Users.FilePath = Users.FileAppPath;
            AppHistory.FilePath = AppHistory.FileAppPath;
        }

        private void CheckBoxSaveSameLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            DirectoryHelper.Create(AppSettings.RoamingPath);

            string[] appPaths = new string[] { AppSettings.FileAppPath, Users.FileAppPath, AppHistory.FileAppPath };
            string[] roamingPaths = new string[] { AppSettings.FileRoamingPath, Users.FileRoamingPath, AppHistory.FileRoamingPath };

            for (int i = 0; i < appPaths.Length; i++) FileHelper.Move(appPaths[i], roamingPaths[i]);

            AppSettings.FilePath = AppSettings.FileRoamingPath;
            Users.FilePath = Users.FileRoamingPath;
            AppHistory.FilePath = AppHistory.FileRoamingPath;
        }

        private void CheckBoxEnableProxy_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)CheckBoxEnableProxy.IsChecked;
            StackPanelServer.IsEnabled = isChecked;
            StackPanelProxyType.IsEnabled = isChecked;
            AppSettings.Set("Proxy", "Enable", isChecked);
        }
    }
}