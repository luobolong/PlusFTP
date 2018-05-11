using System.ComponentModel;

namespace Hani.Utilities
{
    internal enum TransferAction : ushort
    {
        Unknown = 0,
        Replace = 1,
        Ignore = 2,
        Rename = 3,
        Resume = 4
    };

    internal sealed class TransferEvents : INotifyPropertyChanged
    {
        public static TransferAction TillCloseAction = TransferAction.Unknown;
        public TransferAction SessionAction = TransferAction.Unknown;
        public TransferAction Action = TransferAction.Unknown;
        public SmartItem[] Items = new SmartItem[] { };

        public event PropertyChangedEventHandler PropertyChanged = null;

        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private SmartItem item = new SmartItem();
        public SmartItem Item
        {
            get { return item; }
            set { if (item.HasError) { HasErrors = true; } item = value; ItemChanged(); }
        }

        private long totalSize;
        public long TotalSize
        {
            get { return totalSize; }
            set { totalSize = value; }
        }

        private long totalSent;
        public long TotalSent
        {
            get { return totalSent; }
            private set { totalSent = value; }
        }

        private long itemSent;
        public long ItemSent
        {
            get { return itemSent; }
            set { TotalSent += value - itemSent; itemSent = value; sentChanged(); }
        }

        public int TotalTransferredFiles;
        public int TotalTransferredFolders;

        private int totalFiles;
        public int TotalFiles
        {
            get { return totalFiles; }
            set { totalFiles = value; FirePropertyChanged("TotalFiles"); }
        }

        private int totalFolders;
        public int TotalFolders
        {
            get { return totalFolders; }
            set { totalFolders = value; FirePropertyChanged("TotalFolders"); }
        }

        public bool HasErrors { get; set; }
        public bool IsUpload { get; set; }

        //Transfer Handler
        internal delegate void TransferHandler();
        //Transfer Event
        internal event TransferHandler OnStarting, OnStarted, OnItemChanged, OnSentChanged, OnEnded;

        //Transfer Request Action Handler
        internal delegate void RequestingActionHandler(SmartItem existItem, bool canResume);
        //Requesting Action Event
        internal event RequestingActionHandler OnRequestingAction;

        //Path Changed Handler
        internal delegate void PathChangedHandler(string from, string to);
        //Path Changed Event
        internal event PathChangedHandler OnPathChanged;

        //Item Status Changed Handler
        internal delegate void ItemStatusChangedHandler(SmartItem item);
        //Item Status Changed Event
        internal static event ItemStatusChangedHandler OnItemStatusChanged;

        internal void Starting()
        {
            if (OnStarting != null) new TransferHandler(OnStarting)();
        }

        internal void Started()
        {
            //if (Items.Length > 0) Item = Items[0];
            if (OnStarted != null) new TransferHandler(OnStarted)();
        }

        internal void ItemChanged()
        {
            itemSent = 0;
            if (OnItemChanged != null) new TransferHandler(OnItemChanged)();
        }

        internal void RequestingAction(SmartItem existItem, bool canResume)
        {
            if (OnRequestingAction != null) new RequestingActionHandler(OnRequestingAction)(existItem, canResume);
        }

        internal void PathChanged(string from, string to)
        {
            if (OnPathChanged != null) new PathChangedHandler(OnPathChanged)(from, to);
        }

        internal static void ItemStatusChanged(SmartItem item)
        {
            if (OnItemStatusChanged != null) new ItemStatusChangedHandler(OnItemStatusChanged)(item);
        }

        internal void Ended()
        {
            if (item.HasError) { HasErrors = true; }
            if (OnEnded != null) new TransferHandler(OnEnded)();
        }

        private void sentChanged()
        {
            if (OnSentChanged != null) new TransferHandler(OnSentChanged)();
        }
    }
}