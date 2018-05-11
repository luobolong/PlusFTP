using System.Windows;
using System.Windows.Input;
using Hani.Utilities;

namespace PlusFTP.Windows
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private void LocalItem_GotFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void LocalItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            if (LocalList.SelectedItems.Count > 1) e.Handled = true;
        }

        private void LocalItem_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.LeftButton != MouseButtonState.Pressed) || (LocalList.SelectedItems.Count == 0)) return;

            SmartItem[] Items = LocalList.SelectedItems();
            if (Items == null) return;

            if (Items[0].FullName == LocalHelper.ThisPC) return;

            string[] items = new string[Items.Length];
            for (int i = 0; i < Items.Length; i++) items[i] = Items[i].FullName;
            Items = null;

            draggingFrom = DraggingFrom.LocalList;
            DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, items), DragDropEffects.Copy);
        }

        private void LocalItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            goToLocalItem();
        }

        private void LocalItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (LocalList.SelectedItems.Count == 1)
            {
                if (e.Key == Key.Enter) goToLocalItem();
                else if (e.Key == Key.F2) MenuItemLocalRename_Click(sender, e);
            }
        }
    }
}