using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class HistoryWindow : MahApps.Metro.Controls.MetroWindow
    {
        private static ICollectionView historyListView;
        private bool isLoaded = false;

        internal static void Initialize(Window owner)
        {
            new HistoryWindow(owner).ShowDialog();
        }

        private HistoryWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();

            this.SetTopLeft("HistoryWindow");
            this.SetWidthHeight("HistoryWindow", this.MinWidth, this.MinHeight);
            AppSettings.SetGridView("HistoryList", HistoryList.View);

            CheckBoxSaveHistory.IsChecked = AppSettings.Get("App", "SaveHistory", true);
            CheckBoxSaveHistory.Checked += new RoutedEventHandler(CheckBoxSaveHistory_Checked);
            CheckBoxSaveHistory.Unchecked += new RoutedEventHandler(CheckBoxSaveHistory_Unchecked);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HistoryListProgress = true;
            HistoryList.ItemsSource = await AppHistory.GetItems();
            isLoaded = true;
            HistoryList.UnselectAll();
            historyListView = CollectionViewSource.GetDefaultView(HistoryList.ItemsSource);
            HistoryListProgress = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.SaveWidthHeight("HistoryWindow");
            this.SaveTopLeft("HistoryWindow");
            AppSettings.SaveGridView("HistoryList", HistoryList.View);
        }

        private void HistoryList_LostFocus(object sender, RoutedEventArgs e)
        {
            HistoryList.UnselectAll();
        }

        private async void ButtonEmptyList_Click(object sender, RoutedEventArgs e)
        {
            HistoryList.IsEnabled = false;
            AppHistory.Clear();
            await AppHistory.Delete();
            HistoryList.IsEnabled = true;
        }

        private void CheckBoxSaveHistory_Checked(object sender, RoutedEventArgs e)
        {
            HistoryList.IsEnabled = false;
            AppHistory.IsEnabled = true;
            AppSettings.Set("App", "SaveHistory", true);
            AppHistory.Save();
            HistoryList.IsEnabled = true;
        }

        private async void CheckBoxSaveHistory_Unchecked(object sender, RoutedEventArgs e)
        {
            HistoryList.IsEnabled = true;
            AppHistory.IsEnabled = false;
            AppSettings.Set("App", "SaveHistory", false);
            await AppHistory.Delete();
            HistoryList.IsEnabled = true;
        }

        private void HistoryList_SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            historyListView.Filter = new Predicate<object>(searchHistoryList);
        }

        private bool searchHistoryList(object obj)
        {
            string search = HistoryList_SearchBox.Text.Trim().Lower();
            if (search.Length == 0) return true; // the filter is empty - pass all items

            HistoryItem item = obj as HistoryItem;
            if (item == null) return true;

            if (item.ItemName.Lower().Contains(search)) return true;
            return false;
        }

        private bool HistoryListProgress
        {
            set
            {
                if (value == true)
                {
                    Task.Run(async delegate
                    {
                        await Task.Delay(500);
                        if (!isLoaded) Dispatcher.Invoke((Action)(() => { HistoryListProgressRing.Roll(true); }));
                    });
                }
                else HistoryListProgressRing.Roll(false);
            }
        }
    }
}