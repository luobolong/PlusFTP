using System.Threading.Tasks;
using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class NewFolderWindow : MahApps.Metro.Controls.MetroWindow
    {
        internal string newFolder = string.Empty;
        private bool local;

        internal static async Task<string> New(Window owner, bool isLocal)
        {
            NewFolderWindow NF = new NewFolderWindow(owner, isLocal);

            if ((bool)NF.ShowDialog())
            {
                if (isLocal)
                {
                    if (DirectoryHelper.Create(LocalHelper.CurrentPath + @"\" + NF.newFolder))
                        return NF.newFolder;
                }
                else if (await ClientHelper.NewFolder(NF.newFolder, ClientHelper.CurrentPath))
                    return NF.newFolder;
            }

            return null;
        }

        internal NewFolderWindow(Window owner, bool isLocal)
        {
            this.Owner = owner;
            InitializeComponent();
            local = isLocal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxNewFolder.Focus();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (newFolder.NullEmpty()) return;

            if (local ? LocalHelper.NameExists(newFolder) : ClientHelper.NameExists(newFolder)) TextBlockNewFolderinfo.Text = AppLanguage.Get("LangTextBlockSameNameExists");
            else DialogResult = true;
        }

        private void TextBoxNewFolder_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            newFolder = TextBoxNewFolder.Text.Trim();
            ButtonOK.IsEnabled = (newFolder.Length > 0);
        }
    }
}