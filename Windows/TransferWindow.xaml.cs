using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class TransferWindow : MahApps.Metro.Controls.MetroWindow
    {
        internal NetworkClient client;

        private long maximum = 0;
        private ShrinkWindow shrink;
        private DispatcherTimer timer1000 = new DispatcherTimer();
        private DispatcherTimer timer200 = new DispatcherTimer();

        private static DateTime zeroTime = new DateTime(0);
        private static int count;

        //Flags
        private bool ownerClosing = false;
        private bool closing = false;
        private bool itemChanged = false;
        private bool sentChanged = false;
        private bool started = false, ended = false;

        internal static void Initialize(Window owner, NetworkClient client)
        {
            TransferWindow TW = new TransferWindow(owner);
            TW.client = client;

            switch (AppSettings.Get("FTP", "UploadBehavior", 0))
            {
                case 1: client.DefaultAction = TransferAction.Ignore; break;
                case 2: client.DefaultAction = TransferAction.Rename; break;
                case 3: client.DefaultAction = TransferAction.Replace; break;
                default: client.DefaultAction = TransferAction.Unknown; break;
            }

            client.TransferEvent = new TransferEvents();
            client.TransferEvent.OnStarting += new TransferEvents.TransferHandler(TW.OnStarting);
            client.TransferEvent.OnStarted += new TransferEvents.TransferHandler(TW.OnStarted);
            client.TransferEvent.OnItemChanged += new TransferEvents.TransferHandler(TW.OnItemChanged);
            client.TransferEvent.OnRequestingAction += new TransferEvents.RequestingActionHandler(TW.OnRequestingAction);
            client.TransferEvent.OnSentChanged += new TransferEvents.TransferHandler(TW.OnSentChanged);
            client.TransferEvent.OnPathChanged += new TransferEvents.PathChangedHandler(TW.OnPathChanged);
            client.TransferEvent.OnEnded += new TransferEvents.TransferHandler(TW.OnEnded);
        }

        private TransferWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
            this.SetTopLeft("TransferWindow");
            Width = AppSettings.Get("TransferWindow", "Width", this.MinWidth);

            ButtonClose.Click += new RoutedEventHandler(ButtonClose_Click);
            ButtonSkip.Click += new RoutedEventHandler(ButtonSkip_Click);
            shrink = new ShrinkWindow(this, FullWindow, MiniWindow);
            count++;

            ClientHelper.Owner.Closing += OwnerClosing;
            MiniProgressBar.IsIndeterminate = true;
            ProgressBarExtension.NormalColor = MiniProgressBar.Foreground;
        }

        private void OwnerClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ownerClosing = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlockSession.Text = "#" + count.String();
            timer200.Interval = TimeSpan.FromMilliseconds(200);
            timer200.Tick += new EventHandler(timer200_Tick);

            timer1000.Interval = TimeSpan.FromMilliseconds(1000);
            timer1000.Tick += new EventHandler(timer1000_Tick);

            client.OnConnecting += Connecting;            //new FTPClient.StateHandler() see -=
            client.OnConnected += Connected;
            client.OnFailedToConnect += FailedToConnected;

            Title = AppLanguage.Get("LangTitleCalculatingItems");
            Task.Run(async delegate
            {
                await Task.Delay(300);
                if (!started) Dispatcher.Invoke((Action)(() =>
                {
                    TransferProgress.Roll(true);
                    TextBlockCounts.DataContext = client.TransferEvent;
                    TextBlockCounts.Visibility = Visibility.Visible;
                }));
            });

            switch (AppSettings.Get("FTP", "FishedBehavior", 0))
            {
                case 1:
                    fishedAction = FishedAction.CloseWindow;
                    MenuItemFishedCloseWindow.IsChecked = true;
                    break;
                case 2:
                    fishedAction = FishedAction.CloseApp;
                    MenuItemFishedCloseApp.IsChecked = true;
                    break;
                case 3:
                    fishedAction = FishedAction.Shutdown;
                    MenuItemFishedShutdown.IsChecked = true;
                    break;
                default:
                    MenuItemFishedDisabled.IsChecked = true;
                    break;
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ownerClosing) return;

            e.Cancel = false;
            if (!ended && conformCancel())
            {
                closing = true;
                //Hide();

                if (client.IsChild)
                    await client.DisconnectAsync(false, false);
                else
                {
                    client.OnConnecting -= Connecting;
                    client.OnConnected -= Connected;
                    client.OnFailedToConnect -= FailedToConnected;
                    client.IsCanceled = true;
                    client.Paused = false;
                    await client.Abort();
                }

                while (!ended) { await Task.Delay(100); }
            }

            e.Cancel = !ended && !closing;

            if (ended)
            {
                if (!shrink.Shrinked)
                {
                    AppSettings.Set("TransferWindow", "Width", ActualWidth);
                    this.SaveTopLeft("TransferWindow");
                }

                ClientHelper.Owner.Closing -= OwnerClosing;
                ClientHelper.Owner.Activate();
            }
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            shrink.Do();
        }

        private void ButtonSkip_Click(object sender, RoutedEventArgs e)
        {
            client.FlagSkipIt = true;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            client.Paused = true;
            timer1000.Stop();
            timer200.Stop();

            ButtonResume.Visibility = Visibility.Visible;
            ButtonPause.Visibility = Visibility.Collapsed;
            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Paused);
        }

        private void ButtonResume_Click(object sender, RoutedEventArgs e)
        {
            if (ownerClosing || closing) return;

            client.Paused = false;
            ButtonPause.Visibility = Visibility.Visible;
            ButtonResume.Visibility = Visibility.Collapsed;
            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Normal);
            timer1000.Start();
            timer200.Start();
        }

        private void ButtonOpenTarget_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string target = client.TransferEvent.Items[0].Destination + client.TransferEvent.Items[0].ItemName;
                if (client.TransferEvent.Items[0].IsFile)
                    System.Diagnostics.Process.Start("explorer.exe", @"/select, " + target);
                else System.Diagnostics.Process.Start(target);
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
        }

        private void Connecting()
        {
            GroupBoxCurrentX.Opacity = GroupBoxTotal.Opacity = 0.2;
            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Paused);
            ButtonFished.Visibility = Visibility.Hidden;
            ButtonPause.Visibility = Visibility.Hidden;
            ButtonSkip.Visibility = Visibility.Hidden;
            TransferProgress.Roll(true);
            LabelReconnecting.Visibility = Visibility.Visible;
            TextBlockCounts.Visibility = Visibility.Collapsed;
        }

        private void Connected()
        {
            GroupBoxCurrentX.Opacity = GroupBoxTotal.Opacity = 1;
            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Normal);
            ButtonFished.Visibility = Visibility.Visible;
            ButtonPause.Visibility = Visibility.Visible;

            if (client.TransferEvent.Item.IsFile)
                ButtonSkip.Visibility = Visibility.Visible;

            TransferProgress.Roll(false);
            LabelReconnecting.Visibility = Visibility.Collapsed;

            if ((client.TransferEvent.ItemSent == 0) && (client.TransferEvent.TotalTransferredFolders == 0))
                TextBlockCounts.Visibility = Visibility.Visible;
        }

        private void FailedToConnected()
        {
            client.TransferEvent.HasErrors = true;
            TransferProgress.Roll(false);
            LabelReconnecting.Content = AppLanguage.Get("LangTextUnableToConnect");
        }

        internal void OnStarting()
        {
            Show();
        }

        internal void OnStarted()
        {
            started = true;
            MiniProgressBar.IsIndeterminate = false;
            if (client.TransferEvent.IsUpload) Title = AppLanguage.Get("LangTitleUploading_x").FormatC(0);
            else Title = AppLanguage.Get("LangTitleDownloading_x").FormatC(0);

            GroupBoxCurrentX.Visibility = GroupBoxTotal.Visibility = Visibility.Visible;
            LabelETA.Visibility = Visibility.Visible;
            TransferProgress.Roll(false);
            TextBlockCounts.Visibility = Visibility.Collapsed;

            ButtonPause.Visibility = Visibility.Visible;

            if (client.TransferEvent.TotalFiles > 0)
            {
                LabelFilesXY.Content = AppLanguage.Get("LangLabelFiles_X_Y").FormatC(0, client.TransferEvent.TotalFiles);
                LabelFilesXY.Visibility = Visibility.Visible;
            }

            if (client.TransferEvent.TotalFolders > 0)
            {
                LabelFoldersXY.Content = AppLanguage.Get("LangLabelFolders_X_Y").FormatC(0, client.TransferEvent.TotalFolders);
                LabelFoldersXY.Visibility = Visibility.Visible;
            }

            cacher.TotalSize = SizeUnit.Parse(client.TransferEvent.TotalSize);
            LabelTotalSizeXY.Content = AppLanguage.Get("LangLabelTransferredSize_X_FromTotal_Y").FormatC(0, cacher.TotalSize);
            LabelTotalRemainsX.Content = AppLanguage.Get("LangLabelRemains_X").FormatC(cacher.TotalSize);

            maximum = client.TransferEvent.TotalFolders + client.TransferEvent.TotalFiles + client.TransferEvent.TotalSize;
            if (maximum == 0) maximum = 1;

            LabelTransferSpeed.Content = AppLanguage.Get("LangLabelAverageTransferSpeed_X").FormatC(SizeUnit.Parse(0));
            ElapsedTimeLabel.Content = AppLanguage.Get("LangLabelElapsedTime_X").FormatC(zeroTime);
            LabelETA.Content = AppLanguage.Get("LangLabelETATime_X").FormatC(zeroTime);

            ButtonFished.Visibility = Visibility.Visible;
            TaskbarHelper.Add(ProgressBarTotal);

            timer200.Start();
            timer1000.Start();
        }

        internal void OnItemChanged()
        {
            itemChanged = true;
        }

        internal void OnSentChanged()
        {
            sentChanged = true;
        }

        internal void OnRequestingAction(SmartItem EItem, bool canResume)
        {
            timer1000.Stop();
            timer200.Stop();

            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Paused);
            ButtonResume.Visibility = Visibility.Visible;
            ButtonPause.Visibility = Visibility.Collapsed;
            ButtonSkip.Visibility = Visibility.Hidden;

            new FileExistWindow(this, EItem, canResume).ShowDialog();

            ButtonSkip.Visibility = Visibility.Visible;
            ButtonResume.Visibility = Visibility.Collapsed;
            ButtonPause.Visibility = Visibility.Visible;
            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Normal);
            timer1000.Start();
            timer200.Start();
        }

        internal void OnPathChanged(string from, string to)
        {
            pathChanged.From = from;
            pathChanged.To = to;
            pathChanged.Changed = true;
        }

        internal async void OnEnded()
        {
            ended = true;
            timer1000.Stop();
            timer200.Stop();

            if (ownerClosing) return;

            TaskbarHelper.Remove(ProgressBarTotal);

            if (closing) return;

            if (client.IsChild) await client.DisconnectAsync(false, false);

            if (client.TransferEvent.Items.Length == 0)
            {
                if (client.TransferEvent.IsUpload)
                    AppMessage.Add("Nothing to upload.", MessageType.Warning);
                else
                    AppMessage.Add("Nothing to download.", MessageType.Warning);
                this.Close();
                return;
            }

            ButtonCancel.IsEnabled = false;
            ButtonCancel.Visibility = Visibility.Collapsed;
            ButtonSkip.Visibility = Visibility.Hidden;
            ButtonPause.Visibility = Visibility.Hidden;

            if (client.TransferEvent.HasErrors) MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Error);
            else MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Indeterminate);

            switch (fishedAction)
            {
                case FishedAction.CloseWindow:
                    if (!this.IsActive && !ClientHelper.Owner.IsActive) ClientHelper.Owner.FlashWindow(3);
                    this.Close();
                    return;

                case FishedAction.CloseApp:
                    AutoShutdownWindow.Initialize(this, false);
                    this.Close();
                    return;

                case FishedAction.Shutdown:
                    AutoShutdownWindow.Initialize(this, true);
                    this.Close();
                    return;
            }

            updateCurrentItem();
            UpdateProgress();
            updateTotalFF();

            LabelCurrentRemainsX.Visibility = Visibility.Collapsed;
            LabelTotalRemainsX.Visibility = Visibility.Collapsed;
            LabelETA.Visibility = Visibility.Collapsed;

            if (client.TransferEvent.IsUpload)
                this.Title = client.TransferEvent.HasErrors ? AppLanguage.Get("LangTitleUploadFinishedErrors") : AppLanguage.Get("LangTitleUploadDone");
            else
            {
                this.Title = client.TransferEvent.HasErrors ? AppLanguage.Get("LangTitleDownloadFinishedErrors") : AppLanguage.Get("LangTitleDownloadDone");
                ButtonOpenTarget.Visibility = Visibility.Visible;
            }

            ButtonClose.Visibility = Visibility.Visible;
            ButtonClose.IsDefault = true;
            ButtonClose.IsCancel = true;
            ButtonClose.Focus();
        }

        private void timer200_Tick(object sender, EventArgs e)
        {
            if (ended) return;

            if (sentChanged || itemChanged)
            {
                sentChanged = false;
                UpdateProgress();
            }

            if (itemChanged && !shrink.Shrinked)
            {
                itemChanged = false;
                updateCurrentItem();
                updateTotalFF();
            }
        }

        private void timer1000_Tick(object sender, EventArgs e)
        {
            cacher.StartTime = cacher.StartTime.AddSeconds(1.0);
            speedy.Transferred = client.TransferEvent.TotalSent;
            int average = speedy.Average() / 5;

            if (average > 0)
            {
                if ((average > NetworkClient.BUFFER_SIZE) && (average < 1043741824)) client.BufferSize = average;

                if (!shrink.Shrinked)
                {
                    average = average * 5;
                    ElapsedTimeLabel.Content = AppLanguage.Get("LangLabelElapsedTime_X").FormatC(cacher.StartTime);
                    LabelTransferSpeed.Content = AppLanguage.Get("LangLabelAverageTransferSpeed_X").FormatC(SizeUnit.Parse(average));

                    LabelETA.Content = AppLanguage.Get("LangLabelETATime_X").FormatC(
                             zeroTime.AddSeconds((client.TransferEvent.TotalSize - client.TransferEvent.TotalSent) / average));
                }
            }
            else if (!shrink.Shrinked) LabelETA.Content = AppLanguage.Get("LangLabelETATime_X").FormatC(zeroTime);
        }

        private void UpdateProgress()
        {
            SmartItem item = client.TransferEvent.Item;
            ProgressBarTotal.Value = (((client.TransferEvent.TotalTransferredFolders + client.TransferEvent.TotalTransferredFiles + client.TransferEvent.TotalSent) * 100) / maximum);

            if (!ended && (ProgressBarTotal.Value > 0))
            {
                if (client.TransferEvent.IsUpload) Title = AppLanguage.Get("LangTitleUploading_x").FormatC(ProgressBarTotal.Value);
                else Title = AppLanguage.Get("LangTitleDownloading_x").FormatC(ProgressBarTotal.Value);
            }

            if (!shrink.Shrinked)
            {
                ProgressBarCurrent.Value = (item.Length == 0) ? 100 : client.TransferEvent.ItemSent * 100 / item.Length;
                GroupBoxCurrentX.Header = AppLanguage.Get("LangGroupBoxCurrent_X").FormatC(ProgressBarCurrent.Value);
                LabelCurrentSizeXY.Content = AppLanguage.Get("LangLabelTransferredSize_X_FromTotal_Y").FormatC(SizeUnit.Parse(client.TransferEvent.ItemSent), cacher.CurrentSize);
                LabelCurrentRemainsX.Content = AppLanguage.Get("LangLabelRemains_X").FormatC(SizeUnit.Parse(item.Length - client.TransferEvent.ItemSent));
                LabelTotalSizeXY.Content = AppLanguage.Get("LangLabelTransferredSize_X_FromTotal_Y").FormatC(SizeUnit.Parse(client.TransferEvent.TotalSent), cacher.TotalSize);
                LabelTotalRemainsX.Content = AppLanguage.Get("LangLabelRemains_X").FormatC(SizeUnit.Parse(client.TransferEvent.TotalSize - client.TransferEvent.TotalSent));
            }
        }

        private void updateCurrentItem()
        {
            SmartItem item = client.TransferEvent.Item;
            LabelItemName.Text = item.ItemName;
            ProgressBarCurrent.Value = 0.0;

            if (item.IsFile)
            {
                if (!ended) ButtonSkip.Visibility = Visibility.Visible;
                GroupBoxCurrentX.Header = AppLanguage.Get("LangGroupBoxCurrent_X").FormatC(0);
                cacher.CurrentSize = item.FileSize;
                LabelCurrentSizeXY.Content = AppLanguage.Get("LangLabelTransferredSize_X_FromTotal_Y").FormatC(0, cacher.CurrentSize);
            }
            else
            {
                GroupBoxCurrentX.Header = AppLanguage.Get("LangGroupBoxCurrent_X").FormatC(100);
                ButtonSkip.Visibility = Visibility.Hidden;
            }

            if (pathChanged.Changed)
            {
                LabelItemFrom.Text = pathChanged.From;
                LabelItemTo.Text = pathChanged.To.Length > 1 ? pathChanged.To.TrimEnd('/') : pathChanged.To;
            }
        }

        private void updateTotalFF()
        {
            if (client.TransferEvent.TotalFiles > 0)
                LabelFilesXY.Content = AppLanguage.Get("LangLabelFiles_X_Y").FormatC(client.TransferEvent.TotalTransferredFiles, client.TransferEvent.TotalFiles);
            if (client.TransferEvent.TotalFolders > 0)
                LabelFoldersXY.Content = AppLanguage.Get("LangLabelFolders_X_Y").FormatC(client.TransferEvent.TotalTransferredFolders, client.TransferEvent.TotalFolders);
        }

        private void ButtonFished_Click(object sender, RoutedEventArgs e)
        {
            ButtonFished.ContextMenu.PlacementTarget = ButtonFished;
            ButtonFished.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void MenuItemFishedDisabled_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Set("FTP", "FishedBehavior", 0);
            fishedAction = FishedAction.Disable;
            MenuItemFishedCloseWindow.IsChecked = false;
            MenuItemFishedCloseApp.IsChecked = false;
            MenuItemFishedShutdown.IsChecked = false;
        }

        private void MenuItemFishedCloseWindow_Checked(object sender, RoutedEventArgs e)
        {
            MenuItemFishedDisabled.IsChecked = false;
            AppSettings.Set("FTP", "FishedBehavior", 1);
            fishedAction = FishedAction.CloseWindow;
            MenuItemFishedCloseApp.IsChecked = false;
            MenuItemFishedShutdown.IsChecked = false;
        }

        private void MenuItemFishedCloseApp_Checked(object sender, RoutedEventArgs e)
        {
            MenuItemFishedDisabled.IsChecked = false;
            MenuItemFishedCloseWindow.IsChecked = false;
            AppSettings.Set("FTP", "FishedBehavior", 2);
            fishedAction = FishedAction.CloseApp;
            MenuItemFishedShutdown.IsChecked = false;
        }

        private void MenuItemFishedShutdown_Checked(object sender, RoutedEventArgs e)
        {
            MenuItemFishedDisabled.IsChecked = false;
            MenuItemFishedCloseWindow.IsChecked = false;
            MenuItemFishedCloseApp.IsChecked = false;
            AppSettings.Set("FTP", "FishedBehavior", 3);
            fishedAction = FishedAction.Shutdown;
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!MenuItemFishedCloseWindow.IsChecked &&
                !MenuItemFishedCloseApp.IsChecked &&
                !MenuItemFishedShutdown.IsChecked)
                MenuItemFishedDisabled.IsChecked = true;
        }

        private bool conformCancel()
        {
            client.Paused = true;
            timer1000.Stop();
            timer200.Stop();
            MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Paused);
            ButtonResume.Visibility = Visibility.Visible;
            ButtonPause.Visibility = Visibility.Collapsed;
            ButtonSkip.Visibility = Visibility.Hidden;

            if (MessageWindow.Show(this,
                AppLanguage.Get("LangMBCancelTransfer"),
                AppLanguage.Get("LangMBDCancelTransferTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes) != MessageBoxResult.Yes)
            {
                if (ownerClosing || closing) return false;

                client.Paused = false;
                MiniProgressBar.SetStateColor(ProgressBarExtension.ProgressState.Normal);
                if (started && client.TransferEvent.Item.IsFile) ButtonSkip.Visibility = Visibility.Visible;
                ButtonResume.Visibility = Visibility.Collapsed;
                ButtonPause.Visibility = Visibility.Visible;
                timer1000.Start();
                timer200.Start();
                return false;
            }

            return true;
        }

        private void MiniWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void TextBlockSession_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) shrink.Undo();
        }

        private enum FishedAction : ushort
        {
            Disable = 0,
            CloseWindow = 1,
            CloseApp = 2,
            Shutdown = 3
        };

        private FishedAction fishedAction = FishedAction.Disable;

        private class Cacher
        {
            internal string TotalSize, CurrentSize = "0";
            internal DateTime StartTime = zeroTime;
        }

        private class Speedy
        {
            private int _X = 0;
            private int[] transfers = new int[5];
            private long transferred = 0;

            internal long Transferred
            {
                set
                {
                    try
                    {
                        transfers[_X] = ((int)(value - transferred));
                        if (_X < 4) _X++;
                        else _X = 0;
                    }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }
                    transferred = value;
                }
            }

            internal int Average()
            {
                int sum = 0;
                int count = 0;
                for (int x = 0; x < transfers.Length; x++)
                {
                    if (transfers[x] > 0)
                    {
                        count++;
                        sum += transfers[x];
                    }
                }
                if (count > 0) return (sum / count);
                return 0;
            }
        }

        private class PathChanged
        {
            internal string From, To = string.Empty;
            internal bool Changed = false;
        }

        private Cacher cacher = new Cacher();
        private PathChanged pathChanged = new PathChanged();
        private Speedy speedy = new Speedy();

        private void ButtonRestore_Click(object sender, RoutedEventArgs e)
        {
            shrink.Undo();
        }

        private void TextBlockSession_ToolTipOpening(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            TextBlockSession.ToolTip = AppLanguage.Get("LangLabelAverageTransferSpeed_X").FormatC(SizeUnit.Parse(speedy.Average()));
        }
    }
}