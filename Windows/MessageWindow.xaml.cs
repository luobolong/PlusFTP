using System.Windows;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MessageWindow : MahApps.Metro.Controls.MetroWindow
    {
        private MessageWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        internal static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
                        MessageBoxButton button, MessageBoxImage icon,
                        MessageBoxResult defaultResult)
        {
            MessageWindow MW = new MessageWindow(owner);
            MW.TextBlockMessageText.Text = messageBoxText;
            MW.Title = caption;

            switch (button)
            {
                case MessageBoxButton.OK:
                    MW.ButtonYes.Content = AppLanguage.Get("LangButtonOK");
                    MW.ButtonYes.Focus();
                    break;

                case MessageBoxButton.OKCancel:
                    MW.ButtonYes.Content = AppLanguage.Get("LangButtonOK");
                    MW.ButtonCancel.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNo:
                    MW.ButtonNo.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNoCancel:
                    MW.ButtonCancel.Visibility = Visibility.Visible;
                    MW.ButtonNo.Visibility = Visibility.Visible;
                    break;

                default:
                    break;
            }

            switch (icon)
            {
                case MessageBoxImage.Question:
                    break;

                case MessageBoxImage.Error:
                    break;

                case MessageBoxImage.Information:
                    break;

                case MessageBoxImage.Warning:
                    break;

                case MessageBoxImage.None:
                default:
                    MW.MessageImage.Visibility = Visibility.Collapsed;
                    break;
            }

            switch (defaultResult)
            {
                case MessageBoxResult.No:
                    MW.ButtonNo.IsDefault = true;
                    MW.ButtonNo.Focus();
                    break;

                case MessageBoxResult.OK:
                case MessageBoxResult.Yes:
                    MW.ButtonYes.IsDefault = true;
                    MW.ButtonYes.Focus();
                    break;

                default:
                    MW.ButtonCancel.IsDefault = true;
                    MW.ButtonCancel.Focus();
                    break;
            }

            MW.ShowDialog();
            if (MW.DialogResult.HasValue)
            {
                if (MW.DialogResult.Value) return MessageBoxResult.Yes;
                else return MessageBoxResult.No;
            }
            else return MessageBoxResult.Cancel;
        }
    }
}