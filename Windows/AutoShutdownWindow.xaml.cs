using System;
using System.Windows;
using System.Windows.Threading;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class AutoShutdownWindow : MahApps.Metro.Controls.MetroWindow
    {
        private DispatcherTimer timer1000 = new DispatcherTimer();
        private int seconds;
        private bool Shutdown = false;

        internal static void Initialize(Window owner, bool shutdown)
        {
            new AutoShutdownWindow(owner, shutdown).ShowDialog();
        }

        private AutoShutdownWindow(Window owner, bool shutdown)
        {
            this.Owner = owner;
            InitializeComponent();

            Shutdown = shutdown;
            if (Shutdown) seconds = 30;
            else seconds = 10;

            if (!this.Owner.IsActive) this.Owner.Activate();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updateMessage();
            timer1000.Interval = TimeSpan.FromMilliseconds(1000);
            timer1000.Tick += timer1000_Tick;
            timer1000.Start();
        }

        private void timer1000_Tick(object sender, EventArgs e)
        {
            if (seconds > 1)
            {
                seconds -= 1;
                updateMessage();
            }
            else
            {
                if (Shutdown)
                {
                    try { System.Diagnostics.Process.Start("shutdown", "/s /t 0"); }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }
                }
                else Application.Current.Shutdown();
            }
        }

        private void ButtonAbort_Click(object sender, RoutedEventArgs e)
        {
            timer1000.Stop();
            DialogResult = false;
        }

        private void updateMessage()
        {
            if (Shutdown)
                TextBlockMessageText.Text = AppLanguage.Get("LangAutomaticShutdownPCIn_X").FormatC(seconds);
            else
                TextBlockMessageText.Text = AppLanguage.Get("LangAutomaticShutdownAppIn_X").FormatC(seconds);
        }
    }
}