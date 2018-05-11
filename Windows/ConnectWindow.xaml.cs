using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Hani.FTP;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class ConnectWindow : MahApps.Metro.Controls.MetroWindow
    {
        private UserViewModel users;

        private class UserViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void Notify(string propertyName)
            {
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            public ObservableCollection<UserInfo> Items { get; set; }

            private UserInfo _item = new UserInfo();
            public UserInfo Item { get { return _item; } set { _item = value; Notify("Item"); } }

            private int _selectedIndex; // static
            public int SelectedIndex { get { return _selectedIndex; } set { _selectedIndex = value; Notify("SelectedIndex"); } }

            public UserViewModel(List<UserInfo> items)
            {
                Items = new ObservableCollection<UserInfo>(items);

                //if (_selectedIndex == -1)
                //{
                int id = -1;
                if (Items.Count != 0)
                {
                    Parallel.For(0, Items.Count, (i, loopState) => { if (Items[i].Selected) { id = i; loopState.Stop(); } });
                    if (id == -1) id = 0;
                }
                SelectedIndex = id;
                //}
            }
        }

        internal static bool OK(Window owner)
        {
            return ((bool)new ConnectWindow(owner).ShowDialog());
        }

        private ConnectWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Users.Load();

            users = new UserViewModel(Users.Items);
            this.DataContext = users;

            if (users.SelectedIndex == -1) { ButtonDelete.IsEnabled = false; ButtonNew.IsEnabled = false; }

            CheckBoxSaveUser.IsChecked = AppSettings.Get("App", "SaveUserinfo", true);
            CheckBoxSaveUser.Click += new RoutedEventHandler(CheckBoxSaveUser_Click);
            CheckBoxSavePass.Click += new RoutedEventHandler(CheckBoxSavePass_Click);
            TextBoxHost.TextChanged += new TextChangedEventHandler(TextBoxHost_TextChanged);
            ComboBoxEncryption.SelectionChanged += new SelectionChangedEventHandler(ComboBoxEncryption_SelectionChanged);

            this.IsEnabled = true;

            ButtonConnect.Focus();
        }

        private void CheckBoxSaveUser_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Set("App", "SaveUserinfo", (bool)CheckBoxSaveUser.IsChecked);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string server = TextBoxHost.Text.Trim();
            if (server.NullEmpty())
            {
                DialogResult = false;
                return;
            }

            NetworkClient.Host = server;
            NetworkClient.UserName = TextBoxUserName.Text.Trim();

            string password = TextBoxPassword.Password;
            if (!password.NullEmpty())
            {
                if (!CryptoHashing.IsEncrypted(password))
                    password = CryptoHashing.Encrypt(password);
                else password = users.Item.Password;
            }

            NetworkClient.Password = password;
            NetworkClient.Port = TextBoxPort.Text.Int(21);
            _setOptions();
            DialogResult = true;

            await save();
        }

        private void ButtonNew_Click(object sender, RoutedEventArgs e)
        {
            if (users.Items.Count == 0) return;

            users.Items.Add(new UserInfo());
            users.SelectedIndex = users.Items.Count - 1;
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            int id = users.SelectedIndex;

            if ((id > -1) && (id < users.Items.Count))
            {
                users.Items[id] = new UserInfo();
                users.SelectedIndex = id;
            }
            else
            {
                users.Item = new UserInfo();
                TextBoxPassword.Password = string.Empty;
            }
        }

        private async void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (users.Items.Count == 0) return;

            if (MessageWindow.Show(this,
                AppLanguage.Get("LangMBRemoveAccount"),
                AppLanguage.Get("LangMBRemoveAccountTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

            if (users.Items.Count == 1) { ButtonDelete.IsEnabled = false; ButtonNew.IsEnabled = false; }

            int id = users.SelectedIndex;
            users.Items.RemoveAt(id);
            Users.Items.RemoveAt(id);

            if (id > 0) users.SelectedIndex = --id;
            else if (users.Items.Count > 0) users.SelectedIndex = id;

            await Users.Save();
        }

        private void ComboBoxUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserInfo _user;
            int id = users.SelectedIndex;

            if ((id > -1) && (id < users.Items.Count)) _user = users.Items[id];
            else _user = new UserInfo();

            users.Item = _user;

            bool hasPass = !_user.Password.NullEmpty();
            CheckBoxSavePass.IsChecked = hasPass;
            if (hasPass) TextBoxPassword.Password = CryptoHashing.PasswordChar;
            else TextBoxPassword.Password = string.Empty;
        }

        private void ComboBoxEncryption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxEncryption.SelectedIndex == 2) TextBoxPort.Text = 990.StringInv();
            else
            {
                if (users.Item.Port == 990) TextBoxPort.Text = 21.StringInv();
                else TextBoxPort.Text = users.Item.Port.StringInv();
            }

            if (ComboBoxEncryption.SelectedIndex == 0) ComboBoxProtocol.SelectedIndex = 0;
            else
            {
                if (users.Item.Protocol == 0) ComboBoxProtocol.SelectedIndex = 1;
                else ComboBoxProtocol.SelectedIndex = users.Item.Protocol;
            }
        }

        private void TextBoxHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            ButtonConnect.IsEnabled = (TextBoxHost.Text.Trim().Length > 0);
        }

        private async void CheckBoxSavePass_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)CheckBoxSavePass.IsChecked && !users.Item.Password.NullEmpty())
            {
                TextBoxPassword.Password = users.Item.Password = string.Empty;
                await Users.Save();
            }
        }

        private void _setOptions()
        {
            switch (ComboBoxEncryption.SelectedIndex)
            {
                case 1: FTPClient.Encryption.Type = EncryptionType.ExplicitType; break;
                case 2: FTPClient.Encryption.Type = EncryptionType.ImplicitType; break;
                default: FTPClient.Encryption.Type = EncryptionType.PlainType; break;
            }

            switch (ComboBoxProtocol.SelectedIndex)
            {
                case 1: FTPClient.Encryption.Protocol = FTPSslProtocol.TLS; break;
                case 2: FTPClient.Encryption.Protocol = FTPSslProtocol.SSL; break;
                default: FTPClient.Encryption.Protocol = FTPSslProtocol.None; break;
            }

            switch (ComboBoxUseUTF8.SelectedIndex)
            {
                case 1: NetworkClient.IsUTF8 = true; break;
                case 2: NetworkClient.IsUTF8 = false; break;
                default: NetworkClient.IsUTF8 = AppSettings.Get("FTP", "UTF8", true); break;
            }

            switch (ComboBoxUseMODEZ.SelectedIndex)
            {
                case 1: FTPClient.EnableMODEZ = true; break;
                case 2: FTPClient.EnableMODEZ = false; break;
                default: FTPClient.EnableMODEZ = AppSettings.Get("FTP", "MODEZ", false); break;
            }

            ProxyClient.Proxy = null;
            switch (ComboBoxUseProxy.SelectedIndex)
            {
                case 0: _setProxy(false); break;
                case 1: _setProxy(true); break;
            }

            switch (ComboBoxCacheFolders.SelectedIndex)
            {
                case 1: NetworkClient.CacheFolders = true; break;
                case 2: NetworkClient.CacheFolders = false; break;
                default: NetworkClient.CacheFolders = AppSettings.Get("FTP", "Cache", true); break;
            }
        }

        private static void _setProxy(bool force)
        {
            if (force || AppSettings.Get("Proxy", "Enable", false))
            {
                string server = AppSettings.Get("Proxy", "Server", "");
                if (!server.NullEmpty())
                {
                    string port = AppSettings.Get("Proxy", "Port", "8080");
                    if (!port.NullEmpty())
                        ProxyClient.Proxy = new ProxyClient.ProxyInfo(server, port, ProxyClient.ProxyProtocol.Http);
                }
            }
        }

        private async Task save()
        {
            if ((bool)CheckBoxSaveUser.IsChecked)
            {
                if (ComboBoxEncryption.SelectedIndex == 0) ComboBoxProtocol.SelectedIndex = 0;

                UserInfo userInfo = new UserInfo();
                userInfo.Host = NetworkClient.Host;
                userInfo.Port = NetworkClient.Port;
                userInfo.UserName = NetworkClient.UserName;
                userInfo.Password = (bool)CheckBoxSavePass.IsChecked ? NetworkClient.Password : string.Empty;
                userInfo.Selected = true;
                userInfo.Encryption = ComboBoxEncryption.SelectedIndex;
                userInfo.Protocol = ComboBoxProtocol.SelectedIndex;
                userInfo.UTF8 = ComboBoxUseUTF8.SelectedIndex;
                userInfo.MODEZ = ComboBoxUseMODEZ.SelectedIndex;
                userInfo.Proxy = ComboBoxUseProxy.SelectedIndex;
                userInfo.Cache = ComboBoxCacheFolders.SelectedIndex;
                Users.AddUser(userInfo);
            }
            else if (users.Items.Count != 0) Users.Items.RemoveAt(users.SelectedIndex);

            await Users.Save();
        }
    }
}