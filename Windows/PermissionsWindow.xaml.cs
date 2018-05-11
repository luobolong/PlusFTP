using System.Windows;
using System.Windows.Controls;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class PermissionsWindow : MahApps.Metro.Controls.MetroWindow
    {
        private bool IsAutoChange;
        private SmartItem[] Items;

        internal static void Initialize(Window owner, SmartItem[] items)
        {
            if ((items != null) && (items.Length > 0) && items[0].Permissions.NullEmpty()) return;
            new PermissionsWindow(owner, items).ShowDialog();
        }

        private PermissionsWindow(Window owner, SmartItem[] items)
        {
            this.Owner = owner;
            InitializeComponent();

            this.Items = items;

            TextBoxPath.Text = ClientHelper.CurrentPath;
            LabelPermissionDgit.Text = items[0].Permissions;
            TextBoxNewPermission.Text = items[0].Permissions;

            LabelPermission.Text = PermParser.GetLetters(items[0].Permissions);

            for (int i = 0; i < items.Length; i++)
            {
                if (!TextBoxFiles.Text.NullEmpty()) TextBoxFiles.Text += AppLanguage.Get("LangTextSpaceComma");
                TextBoxFiles.Text += items[i].ItemName;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RoutedEventHandler permissionClick = new RoutedEventHandler(CheckBoxPermission_Click);

            CheckBoxRO.Click += permissionClick;
            CheckBoxWO.Click += permissionClick;
            CheckBoxEO.Click += permissionClick;
            CheckBoxRG.Click += permissionClick;
            CheckBoxWG.Click += permissionClick;
            CheckBoxEG.Click += permissionClick;
            CheckBoxRE.Click += permissionClick;
            CheckBoxWE.Click += permissionClick;
            CheckBoxEE.Click += permissionClick;
        }

        private void CheckBoxPermission_Click(object sender, RoutedEventArgs e)
        {
            if (IsAutoChange) return;

            ApplyButton.IsEnabled = false;

            IsAutoChange = true;
            TextBoxNewPermission.Text = CheckBoxsGetPermissionDgit();
            IsAutoChange = false;

            if (TextBoxNewPermission.Text != LabelPermissionDgit.Text)
                ApplyButton.IsEnabled = true;
        }

        private void TextBoxNewPermission_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsAutoChange) return;

            ApplyButton.IsEnabled = false;

            string NewPermission = TextBoxNewPermission.Text.Trim();
            if (NewPermission.Length != 3) return;

            IsAutoChange = true;
            CheckBoxsSetPermission(NewPermission);
            IsAutoChange = false;

            if (TextBoxNewPermission.Text != LabelPermissionDgit.Text)
                ApplyButton.IsEnabled = true;
        }

        private async void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyButton.IsEnabled = false;

            await ClientHelper.ChangePermAsync(Items, TextBoxNewPermission.Text);

            LabelPermissionDgit.Text = TextBoxNewPermission.Text;
            LabelPermission.Text = CheckBoxsGetPermissionString();
        }

        private async void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            if (ApplyButton.IsEnabled == true)
            {
                ApplyButton.IsEnabled = false;
                await ClientHelper.ChangePermAsync(Items, TextBoxNewPermission.Text);
            }
            this.Close();
        }

        private string CheckBoxsGetPermissionDgit()
        {
            int perm = 0;
            string permissions = string.Empty;

            if ((bool)CheckBoxRO.IsChecked) perm += 4;
            if ((bool)CheckBoxWO.IsChecked) perm += 2;
            if ((bool)CheckBoxEO.IsChecked) perm += 1;
            permissions += perm.String();
            perm = 0;

            if ((bool)CheckBoxRG.IsChecked) perm += 4;
            if ((bool)CheckBoxWG.IsChecked) perm += 2;
            if ((bool)CheckBoxEG.IsChecked) perm += 1;
            permissions += perm.String();
            perm = 0;

            if ((bool)CheckBoxRE.IsChecked) perm += 4;
            if ((bool)CheckBoxWE.IsChecked) perm += 2;
            if ((bool)CheckBoxEE.IsChecked) perm += 1;
            permissions += perm.String();

            return permissions;
        }

        private string CheckBoxsGetPermissionString()
        {
            string permission = string.Empty;

            permission += ((bool)CheckBoxRO.IsChecked) ? 'r' : '-';
            permission += ((bool)CheckBoxWO.IsChecked) ? 'w' : '-';
            permission += ((bool)CheckBoxEO.IsChecked) ? 'x' : '-';

            permission += ((bool)CheckBoxRG.IsChecked) ? 'r' : '-';
            permission += ((bool)CheckBoxWG.IsChecked) ? 'w' : '-';
            permission += ((bool)CheckBoxEG.IsChecked) ? 'x' : '-';

            permission += ((bool)CheckBoxRE.IsChecked) ? 'r' : '-';
            permission += ((bool)CheckBoxWE.IsChecked) ? 'w' : '-';
            permission += ((bool)CheckBoxEE.IsChecked) ? 'x' : '-';

            return permission;
        }

        private void CheckBoxsSetPermission(string text)
        {
            int[] numbers = new int[] { (int)text[0], (int)text[1], (int)text[2] };

            CheckBoxRO.IsChecked = ((numbers[0] & 4) == 4);
            CheckBoxWO.IsChecked = ((numbers[0] & 2) == 2);
            CheckBoxEO.IsChecked = ((numbers[0] & 1) == 1);

            CheckBoxRG.IsChecked = ((numbers[1] & 4) == 4);
            CheckBoxWG.IsChecked = ((numbers[1] & 2) == 2);
            CheckBoxEG.IsChecked = ((numbers[1] & 1) == 1);

            CheckBoxRE.IsChecked = ((numbers[2] & 4) == 4);
            CheckBoxWE.IsChecked = ((numbers[2] & 2) == 2);
            CheckBoxEE.IsChecked = ((numbers[2] & 1) == 1);
        }
    }
}