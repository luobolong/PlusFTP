using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class BrowseServerWindow : MahApps.Metro.Controls.MetroWindow
    {
        private SmartItem[] selectedItems;
        private static BitmapSource dummyIcon;

        internal class TreeSmartItem : SmartItem
        {
            internal TreeSmartItem Parent = null;
            internal bool HasDummy;

            private bool selected { get; set; }
            public bool Selected
            {
                get { return selected; }
                set { selected = value; FirePropertyChanged("Selected"); }
            }

            private bool expanded { get; set; }
            public bool Expanded
            {
                get { return expanded; }
                set { expanded = value; FirePropertyChanged("Expanded"); }
            }

            private ObservableCollection<TreeSmartItem> items = new ObservableCollection<TreeSmartItem>();
            public ObservableCollection<TreeSmartItem> Items
            {
                get { return items; }
                set { items = value; FirePropertyChanged("Items"); }
            }

            internal TreeSmartItem(string realName, string path)
                : base(realName, path)
            {
                items.Add(new TreeSmartItem(AppLanguage.Get("LangTextLoading"), dummyIcon));
            }

            internal TreeSmartItem(string name, BitmapSource icon)
            {
                FullName = ItemName = name;
                ItemIcon = icon;
            }
        }

        static BrowseServerWindow()
        {
            _set();
        }

        private static void _set()
        {
            dummyIcon = null;
        }

        internal static void Initialize(Window owner, SmartItem[] items)
        {
            new BrowseServerWindow(owner, items).ShowDialog();
        }

        private BrowseServerWindow(Window owner, SmartItem[] items)
        {
            this.Owner = owner;
            InitializeComponent();
            selectedItems = items;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TreeSmartItem item = new TreeSmartItem(ClientHelper.Home, string.Empty);
            item.Selected = true;
            item.Expanded = true;

            await addFolders(item, item.FullName);
            TreeViewFolders.Items.Add(item);
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            TreeSmartItem sItem = TreeViewFolders.SelectedItem as TreeSmartItem;
            if ((sItem == null) || sItem.HasError) return;

            TreeSmartItem item;
            if (sItem.Parent != null) item = sItem.Parent;
            else item = sItem;

            string toPath = item.FullName;
            PathHelper.AddEndningSlash(ref toPath);

            if (MessageWindow.Show(this,
                AppLanguage.Get("LangMBMove").FormatC(System.Environment.NewLine + toPath),
                AppLanguage.Get("LangMBMoveTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

            ClientHelper.MoveAsync(selectedItems, toPath);

            this.Close();
        }

        private async void ButtonNewFolder_Click(object sender, RoutedEventArgs e)
        {
            TreeSmartItem sItem = TreeViewFolders.SelectedItem as TreeSmartItem;
            if ((sItem == null) || sItem.HasError) return;

            NewFolderWindow NFW = new NewFolderWindow(this, false);
            if (!(bool)NFW.ShowDialog() || NFW.newFolder.NullEmpty()) return;

            //item.Expanded = true;
            TreeSmartItem item;
            if (sItem.Parent != null) item = sItem.Parent;
            else item = sItem;

            if (item.HasDummy)
            {
                item.Items.Clear();
                item.HasDummy = false;
            }

            string path = item.FullName;
            PathHelper.AddEndningSlash(ref path);
            TreeSmartItem folder = new TreeSmartItem(NFW.newFolder, path);

            item.Items.Add(folder);

            folder.OptColor = SolidColors.DarkGreen;
            folder.Operation = AppLanguage.Get("LangOperationCreating");

            if (await ClientHelper.NewFolder(NFW.newFolder, path))
            {
                folder.Selected = true;
                folder.OptColor = SolidColors.SolidBlue;
                folder.Operation = AppLanguage.Get("LangOperationCreated");
            }
            else
            {
                folder.Items.Clear();
                folder.HasError = true;
                folder.OptColor = SolidColors.DarkRed;
                folder.Operation = AppLanguage.Get("LangOperationCreateError");
                folder.Items.Add(new TreeSmartItem(AppLanguage.Get("LangTextNoFoldersInside"), dummyIcon));
            }
        }

        private async void TreeViewItemFolder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeItem = e.OriginalSource as TreeViewItem;
            if (treeItem == null) return;

            TreeSmartItem item = treeItem.Header as TreeSmartItem;
            if ((item == null) || item.HasError) return;

            await addFolders(item, item.FullName);
        }

        private void TreeViewItemFolder_Collapsed(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeItem = e.OriginalSource as TreeViewItem;
            if (treeItem == null) return;

            TreeSmartItem item = treeItem.Header as TreeSmartItem;
            if ((item == null) || item.HasError) return;

            item.Items.Clear();
            item.Items.Add(new TreeSmartItem(AppLanguage.Get("LangTextLoading"), dummyIcon));
        }

        private async Task addFolders(TreeSmartItem pItem, string path)
        {
            TreeViewFolders.IsEnabled = false;

            SmartItem[] folders = await ClientHelper.Client.GetServerFoldersAsync(path);

            pItem.Items.Clear();

            if (folders == null)
            {
                TreeSmartItem item = new TreeSmartItem(AppLanguage.Get("LangTextError"), dummyIcon);
                item.Parent = pItem;
                pItem.Items.Add(item);
                pItem.HasDummy = true;
            }
            else if (folders.Length > 0)
            {
                TreeSmartItem[] items = new TreeSmartItem[folders.Length];

                for (int i = 0; i < folders.Length; i++)
                    items[i] = new TreeSmartItem(folders[i].ItemName, folders[i].ItemFolder);

                pItem.Items = new ObservableCollection<TreeSmartItem>(items);
            }
            else
            {
                TreeSmartItem item = new TreeSmartItem(AppLanguage.Get("LangTextNoFoldersInside"), dummyIcon);
                item.Parent = pItem;
                pItem.Items.Add(item);
                pItem.HasDummy = true;
            }

            TreeViewFolders.IsEnabled = true;
        }
    }
}