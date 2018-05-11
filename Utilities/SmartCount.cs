using System;
using System.ComponentModel;

namespace Hani.Utilities
{
    internal sealed class SmartCount : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = null;

        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private int folders;
        public int Folders
        {
            get { return folders; }
            set { folders = value; }
        }

        private int files;
        public int Files
        {
            get { return files; }
            set { files = value; }
        }

        private string items = string.Empty;
        public string Items
        {
            get { return items; }
            set { items = value; FirePropertyChanged("Items"); }
        }

        private DateTime? time;
        public DateTime? Time
        {
            get { return time; }
            set { time = value; FirePropertyChanged("Time"); }
        }

        internal void Update()
        {
            string count = string.Empty;

            if ((files != 0) || (folders != 0))
            {
                if (folders != 0)
                {
                    count = AppLanguage.Get("LangTextFoldersX").FormatC(folders);
                    if (files != 0) count += AppLanguage.Get("LangTextSpaceComma") + " ";
                }
                if (files != 0) count += AppLanguage.Get("LangTextFilesX").FormatC(files);
            }
            Items = count;
        }
    }
}