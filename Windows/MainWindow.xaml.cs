using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private BackForthStack LocalBackForthStack = new BackForthStack();
        private BackForthStack ServerBackForthStack = new BackForthStack();
        private SmartItem[] CachedItems;
        private DraggingFrom draggingFrom;

        private enum DraggingFrom : ushort
        {
            None = 0,
            LocalList = 1,
            ServerList = 2,
        };

        private double columnNameWidth = 0.0;

        public MainWindow()
        {
            AppLanguage.SetCurrentThreadCulture(Thread.CurrentThread);
            _initialize();
        }

        private void _initialize()
        {
            InitializeComponent();

            this.SetWidthHeight("MainWindow", this.MinWidth, this.MinHeight);
            if (AppSettings.Get("MainWindow", "Maximized", false)) WindowState = WindowState.Maximized;
            else this.SetTopLeft("MainWindow");

            if (AppSettings.Get("DetailList", "Minimized", !AppMessage.IsVisible))
            {
                VisualBrushHideDetailList.Visual = this.GetVisual("appbar_chevron_up");
                ButtonHideDetailList.ToolTip = AppLanguage.Get("LangButtonShowDetailList");
                DetailListRow.Height = DLTAniTo;
                AppMessage.IsVisible = false;
            }

            ColumnLocal.Width = new GridLength(AppSettings.Get("MainWindow", "GridSplitter", 235.0));

            //string[] sColumns = new string[] { "ItemName", "Length", "Extension", "Permissions", "LastModified" };
            /*for (int y = 0; y < sColumns.Length; y++)
                ((GridView)ServerList.View).Columns.Move(y, AppSettings.Get("ServerList", sColumns[y], y));*/
            AppSettings.SetGridView("ServerList", ServerList.View);
            AppSettings.SetGridView("DetailList", DetailList.View);

            this.Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ColumnLocal.ActualWidth > (this.ActualWidth - 15.0))
                ColumnLocal.Width = new GridLength(this.ActualWidth - 15.0);

            this.PreviewMouseUp += new MouseButtonEventHandler(window_PreviewMouseUp);
            this.Drop += new DragEventHandler(window_Drop);
            this.Closing += new CancelEventHandler(Window_Closing);
            this.GiveFeedback += new GiveFeedbackEventHandler(window_GiveFeedback);

            ClientHelper.Owner = this;
            ClientHelper.OnLock += new ClientHelper.ClientHandler(Lock);
            ClientHelper.OnUnLock += new ClientHelper.ClientHandler(UnLock);
            ClientHelper.OnConnecting += new ClientHelper.StateHandler(Connecting);
            ClientHelper.OnConnected += new ClientHelper.StateHandler(Connected);
            ClientHelper.OnDisconnected += new ClientHelper.StateHandler(Disconnected);

            ButtonServerConnect.Focus();

            SetPhrases();

            DragThumb.Set(this);

            await setLocalList(LocalHelper.Home);
            LocalHelper.OnListChanged += new LocalHelper.DirectoryHandler(localListChanged);
            TaskbarHelper.MainTaskbar = MainTaskbar;

            AppMessage.Set(DetailList);
            DetailList.ItemsSource = AppMessage.Items;

            LabelServerTime.DataContext = ClientHelper.Counts;
            LabelServerCount.DataContext = ClientHelper.Counts;

            ((INotifyPropertyChanged)(ServerList.View as GridView).Columns[0]).PropertyChanged += new PropertyChangedEventHandler(WidthPropertyChanged);
            hideServerToolbarAnimation.Completed += new EventHandler(hideServerToolbarAnimationCompleted);
            ServerItemsToolbarAnimation.Freeze();
            hideServerToolbarAnimation.Freeze();

            DragSelection.Register(this, LocalList, 0.0, 0.0, "ListBoxItem");
            DragSelection.Register(this, ServerList, 0.0, 20.0, "ListViewItem");

            DragWatcher.Source = "PlusFTP.DragItems";
            DragWatcher.OnDraged += new DragWatcher.DragWatcherHandler(DragWatcher_Draged);

            AppLanguage.OnUpdated += new AppLanguage.UpdatedHandler(LanguageUpdated);

            CryptoHashing.Password = "5A^jIfGyhT@zQ@121%$9D2*2CpCoMqkE";

            VersionHelper.AppUrlHomePage = "http://PlusFTP.com/";
            VersionHelper.VersionUrl = "http://PlusFTP.com/v";
            VersionHelper.NewAppUrl = "http://PlusFTP.com/download/";
            if (AppSettings.Get("App", "CheckVersion", true) && (bool)await VersionHelper.Check())
                NewVersionWindow.Initialize(this);
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            Hide();

            AppSettings.Set("MainWindow", "GridSplitter", ColumnLocal.ActualWidth);

            AppSettings.SaveGridView("ServerList", ServerList.View);
            AppSettings.SaveGridView("DetailList", DetailList.View);

            this.SaveWidthHeight("MainWindow");
            this.SaveTopLeft("MainWindow");

            if (WindowState == WindowState.Normal) AppSettings.Set("MainWindow", "Maximized", false);
            else if (WindowState == WindowState.Maximized) AppSettings.Set("MainWindow", "Maximized", true);

            await AppSettings.Save();
            await ClientHelper.DisconnectAsync();
        }

        private void window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            draggingFrom = DraggingFrom.None;
            DragThumb.Hide();
        }

        private void window_Drop(object sender, DragEventArgs e)
        {
            draggingFrom = DraggingFrom.None;
        }

        private void window_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            switch (e.Effects)
            {
                case DragDropEffects.Copy:
                case DragDropEffects.Move:
                    e.UseDefaultCursors = false;
                    Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
                    break;

                default: e.UseDefaultCursors = true; break;
            }

            e.Handled = true;
        }

        private void ButtonOptions_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow.Initialize(this);
        }

        private void ButtonAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow.Initialize(this);
        }

        private void SetPhrases()
        {
            AppLanguage.SetCurrentThreadCulture(Thread.CurrentThread);

            LocalHelper.ThisPC = AppLanguage.Get("LangThisPC");

            DaysWord.TodayWord = AppLanguage.Get("LangTextToday");
            DaysWord.YesterdayWord = AppLanguage.Get("LangTextYesterday");
            DaysWord._set();

            SizeUnit.SizeB = AppLanguage.Get("LangSizeBytes");
            SizeUnit.SizeKB = AppLanguage.Get("LangSizeKB");
            SizeUnit.SizeMB = AppLanguage.Get("LangSizeMB");
            SizeUnit.SizeGB = AppLanguage.Get("LangSizeGB");
            SizeUnit.SizeTB = AppLanguage.Get("LangSizeTB");

            LabelServerTime.ContentStringFormat = AppLanguage.Get("LangLabelElapsedTime_X");
        }

        private void LanguageUpdated()
        {
            SetPhrases();
            //await setLocalList(LocalHelper.CurrentPath);
            //refreshServerItems();
        }

        private void DragWatcher_Draged(string path)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                draggingFrom = DraggingFrom.None;
                ClientHelper.TransferItemsAsync(CachedItems, path, false);
            }));
        }

        private void showDragThumb(IDataObject idb)
        {
            ImageSource icon;
            int count = 0;

            string[] files = idb.GetData(DataFormats.FileDrop) as string[];

            if ((files.Length == 1) && (files[0].Ends(DragWatcher.Source)))
            {
                icon = IconHelper.Get(CachedItems[0].FullName, CachedItems[0].IsFile, CachedItems[0].Extension, false);
                count = CachedItems.Length;
            }
            else
            {
                bool isFile = FileHelper.Exists(files[0]);
                icon = IconHelper.Get(files[0], isFile, isFile ? FileHelper.GetExtension(files[0]) : string.Empty, false);
                count = files.Length;
            }

            DragThumb.Show(icon, count);
        }
    }
}