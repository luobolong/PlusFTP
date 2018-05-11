using System;
using System.Windows.Media;

namespace Hani.Utilities
{
    internal sealed class HistoryItem
    {
        public string Server { get; set; }
        public string ItemName { get; set; }
        public string ItemPath { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Item_Status { get; set; }
        public SolidColorBrush StatusColor { get; set; }
        public string Status { get; set; }
        public long Ticks { get; set; }

        private string itemDate = string.Empty;
        public string ItemDate
        {
            get
            {
                if (itemDate.NullEmpty()) itemDate = DaysWord.Parse(new DateTime(Ticks).String(DateFormatHelper.SysDateTimeFormat));
                return itemDate;
            }
            set { itemDate = value; }
        }

        internal HistoryItem(string server, string name, string path, string oldValue, string newValue, string status, DateTime date)
        {
            Server = server;
            ItemName = name;
            ItemPath = path;
            OldValue = oldValue;
            NewValue = newValue;
            Status = status;

            if (status.Ends("Error")) StatusColor = SolidColors.DarkRed;
            else if (status.Equal("Ignored")) StatusColor = SolidColors.DarkOrange;
            else StatusColor = SolidColors.DarkGreen;
            Item_Status = AppLanguage.Get("LangOperation" + status);

            Ticks = date.Ticks;
        }
    }
}