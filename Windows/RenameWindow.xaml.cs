using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class RenameWindow : MahApps.Metro.Controls.MetroWindow
    {
        private string newName = string.Empty;
        private bool local;
        private string oldName;

        internal static void Rename(Window owner, SmartItem item, bool isLocal)
        {
            RenameWindow RW = new RenameWindow(owner, item.ItemName, item.Extension, isLocal);

            if ((bool)RW.ShowDialog())
            {
                if (isLocal)
                {
                    string newFullname = LocalHelper.CurrentPath + @"\" + RW.newName;

                    if (item.IsFile) FileHelper.Rename(item.FullName, newFullname);
                    else DirectoryHelper.Rename(item.FullName, newFullname);
                }
                else ClientHelper.RenameAsync(item, RW.newName);
            }
        }

        private RenameWindow(Window owner, string name, string extension, bool isLocal)
        {
            this.Owner = owner;
            InitializeComponent();

            this.TextBoxNewName.Text = oldName = name;
            local = isLocal;
            if (extension.NullEmpty()) TextBoxNewName.SelectAll();
            else TextBoxNewName.Select(0, TextBoxNewName.Text.Length - (extension.Length + 1));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxNewName.Focus();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (newName.NullEmpty() || (oldName == TextBoxNewName.Text)) return;

            if (local ? LocalHelper.NameExists(newName) : ClientHelper.NameExists(newName)) TextBlockNewNameinfo.Text = AppLanguage.Get("LangTextBlockSameNameExists");
            else DialogResult = true;
        }

        private void TextBoxNewName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            newName = TextBoxNewName.Text.Trim();
            ButtonOK.IsEnabled = (newName.Length > 0);
        }
    }
}