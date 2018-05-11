using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class FileExistWindow : MahApps.Metro.Controls.MetroWindow
    {
        private TransferWindow TW;

        internal FileExistWindow(TransferWindow parent, SmartItem eItem, bool canResume)
        {
            this.Owner = TW = parent;
            InitializeComponent();

            this.ButtonResume.IsEnabled = canResume;

            SmartItem tItem = TW.client.TransferEvent.Item;
            if (eItem == null) eItem = new SmartItem();

            this.GroupBoxFileExist.Header = tItem.ItemName;

            TextBoxSource.DataContext = tItem;
            TextBoxTarget.DataContext = eItem;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonReplace.Focus();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (TW.client.TransferEvent.Action == TransferAction.Unknown)
                TW.client.TransferEvent.Action = TransferAction.Ignore;
            Owner.Focus();
        }

        private void SetLongAction()
        {
            if ((bool)RadioButtonTillClose.IsChecked)
                TransferEvents.TillCloseAction = TW.client.TransferEvent.Action;
            else if ((bool)RadioButtonThisSession.IsChecked)
                TW.client.TransferEvent.SessionAction = TW.client.TransferEvent.Action;
        }

        private void ButtonIgnore_Click(object sender, RoutedEventArgs e)
        {
            TW.client.TransferEvent.Action = TransferAction.Ignore;
            SetLongAction();
        }

        private void ButtonRename_Click(object sender, RoutedEventArgs e)
        {
            TW.client.TransferEvent.Action = TransferAction.Rename;
            SetLongAction();
            Close();
        }

        private void ButtonReplace_Click(object sender, RoutedEventArgs e)
        {
            TW.client.TransferEvent.Action = TransferAction.Replace;
            SetLongAction();
            Close();
        }

        private void ButtonResume_Click(object sender, RoutedEventArgs e)
        {
            TW.client.TransferEvent.Action = TransferAction.Resume;
            SetLongAction();
            Close();
        }
    }
}