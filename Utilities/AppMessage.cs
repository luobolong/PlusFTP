using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Hani.Utilities
{
    internal static class AppMessage
    {
        internal static ObservableCollection<MessageItem> Items;
        internal static int Count { get { return Items.Count; } }
        internal static ListView Owner { get; set; }
        internal static bool IsVisible;

        private static List<MessageItem> newItems;
        private static DispatcherTimer timer;
        private static bool isUpdating;

        static AppMessage()
        {
            _set();
        }

        private static void _set()
        {
            Items = new ObservableCollection<MessageItem>();
            IsVisible = true;
            newItems = new List<MessageItem>();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Tick += new EventHandler(timer_Tick);
        }

        private static void timer_Tick(object sender, EventArgs e)
        {
            isUpdating = true;
            timer.Stop();

            //Messages.IsBindingPaused = true;
            for (int i = 0; i < newItems.Count; i++) Items.Add(newItems[i]);
            //Messages.IsBindingPaused = false;

            newItems.Clear();
            if (IsVisible) scrollToLast();
            isUpdating = false;
        }

        private static void scrollToLast()
        {
            int id = Owner.Items.Count - 1;
            if (id > 0) Owner.ScrollIntoView(Owner.Items[id]);
        }

        internal static void Set(ListView owner)
        {
            Owner = owner;
            Owner.ItemsSource = Items;
        }

        internal static void Clear()
        {
            Items.Clear();
        }

        internal static async void Add(string message, MessageType messageType)
        {
            SolidColorBrush c;
            switch (messageType)
            {
                case MessageType.Info:
                    c = SolidColors.DimGray;
                    break;

                case MessageType.Received:
                    char em = message[0];
                    if (em == '5') c = SolidColors.DarkRed;
                    else if (em == '4') c = SolidColors.Orange;
                    else c = SolidColors.SolidGreen;
                    break;

                case MessageType.Sent:
                    c = SolidColors.SolidBlue;
                    break;

                case MessageType.Warning:
                    c = SolidColors.Orange;
                    break;

                case MessageType.Error:
                    c = SolidColors.DarkRed;
                    break;

                default:
                    c = SolidColors.Black;
                    break;
            }

            MessageItem m = new MessageItem(message, c);
            if (isUpdating) while (isUpdating) { await Task.Delay(100); }
            timer.Start();

            newItems.Add(m);
        }
    }

    internal sealed class MessageItem
    {
        public string MText { get; set; }
        public string MDate { get; set; }
        public long DateTicks { get; set; }
        public SolidColorBrush MColor { get; set; }

        internal MessageItem(string message, SolidColorBrush color)
        {
            MText = message;
            MColor = color;

            DateTime now = DateTime.Now;
            DateTicks = now.Ticks;
            MDate = now.String(DateFormatHelper.SysDateTimeFormat);
        }
    }

    internal enum MessageType : ushort
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Received = 4,
        Sent = 5
    }
}