using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hani.Utilities
{
    public enum ItemStatus : int
    {
        Unknown = 0,
        Downloading = 1,
        Downloaded = 2,
        DownloadError = 3,

        Uploading = 4,
        Uploaded = 5,// use New
        UploadError = 6,

        Replacing = 7,
        Replaced = 8,
        ReplaceError = 9,

        Renaming = 10,
        Renamed = 11,
        RenameError = 12,
        Ignored = 13,

        Resuming = 14,
        Resumed = 15,// use New
        ResumeError = 16,

        Deleting = 17,
        Deleted = 18,
        DeleteError = 19,

        Moving = 20,
        Moved = 21,
        MoveError = 22,

        PermissionChanged = 23,
        PermissionError = 24,

        //Folders Only
        Creating = 25,
        Created = 26,
        CreateError = 27
    };

    public class SmartItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = null;

        protected void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _transferredUpdating = false;
        private bool _progressBarEnabled = false;
        public bool ProgressBarEnabled
        {
            get { return _progressBarEnabled; }
            set
            {
                _progressBarEnabled = value;
                if (_progressBarEnabled) ProgressBarVisibility = Visibility.Visible;
                else
                {
                    Transferred = 0;
                    ProgressBarVisibility = Visibility.Hidden;
                }
            }
        }

        private long _transferred = 0;
        public long Transferred
        {
            get { return _transferred; }
            set
            {
                _transferred = value;
                if (_progressBarEnabled && (_transferred != 0))
                {
                    if (!_transferredUpdating)
                    {
                        _transferredUpdating = true;
                        Task.Run(async delegate
                        {
                            await Task.Delay(200);

                            /*switch (status)
                            {
                                case ItemStatus.Uploading:
                                case ItemStatus.Replacing:
                                case ItemStatus.Resuming:
                                    FileSize = SizeUnit.Parse(transferred);
                                    break;
                            }*/
                            if (_status == ItemStatus.Uploading) FileSize = SizeUnit.Parse(_transferred);
                            FirePropertyChanged("Transferred");
                            _transferredUpdating = false;
                        });
                    }
                }
                else FirePropertyChanged("Transferred");
            }
        }

        private string _itemName = string.Empty;
        public string ItemName { get { return _itemName; } set { _itemName = value; FirePropertyChanged("ItemName"); } }

        private string _permissions = string.Empty;
        public string Permissions { get { return _permissions; } set { _permissions = value; FirePropertyChanged("Permissions"); } }

        private SolidColorBrush _optColor = SolidColors.Black;
        public SolidColorBrush OptColor { get { return _optColor; } set { _optColor = value; FirePropertyChanged("OptColor"); } }

        private BitmapSource _itemIcon;
        public BitmapSource ItemIcon
        {
            get
            {
                if (_itemIcon == null) _itemIcon = IconHelper.Get(_fullName, IsFile, Extension, IsLink);
                return _itemIcon;
            }
            set { _itemIcon = value; FirePropertyChanged("ItemIcon"); }
        }

        private ItemStatus _status = ItemStatus.Unknown;
        public ItemStatus Status { get { return _status; } set { _status = value; } }

        private Visibility _progressBarVisibility = Visibility.Hidden;
        public Visibility ProgressBarVisibility
        {
            get { return _progressBarVisibility; }
            set { _progressBarVisibility = value; FirePropertyChanged("ProgressBarVisibility"); }
        }

        private string _operation = string.Empty;
        public string Operation { get { return _operation; } set { _operation = value; FirePropertyChanged("Operation"); } }

        private string _lastModified = string.Empty;
        public string LastModified
        {
            get
            {
                if (_lastModified.NullEmpty()) _lastModified = DaysWord.Parse(new DateTime(Modified).String(DateFormatHelper.SysDateTimeFormat));
                return _lastModified;
            }
            set { _lastModified = value; FirePropertyChanged("LastModified"); }
        }

        private string _fileSize;
        public string FileSize
        {
            get
            {
                if (!IsFile) return string.Empty;
                else if (_fileSize == null) _fileSize = SizeUnit.Parse(Length);
                return _fileSize;
            }
            set { _fileSize = value; FirePropertyChanged("FileSize"); }
        }

        private string _extension;
        public string Extension
        {
            get
            {
                if (!IsFile) _extension = string.Empty;
                else if (_extension == null) _extension = FileHelper.GetExtension(_itemName);
                return _extension;
            }
            set { _extension = value; FirePropertyChanged("Extension"); }
        }

        private long _length = 0;
        public long Length { get { return _length; } set { _length = value; FirePropertyChanged("Length"); } }

        private string _itemFolder = string.Empty;
        public string ItemFolder { get { return _itemFolder; } set { _itemFolder = value; } }

        private string _fullName = string.Empty;
        public string FullName { get { return _fullName; } set { _fullName = value; } }

        private string _destination = string.Empty;
        public string Destination { get { return _destination; } set { _destination = value; } }

        public long Modified { get; set; }
        public int ParentId { get; set; }
        public bool IsFile { get; set; }
        public bool IsLink { get; set; }
        public bool HasError { get; set; }
        public bool Exist { get; set; }

        internal SmartItem()
        {
        }

        // Server File / Folder Only!
        internal SmartItem(string name, string path, long modified = 0)
        {
            _itemName = name;
            ItemFolder = path;
            FullName = path + name;
            Modified = modified;
        }

        // Local File Only!
        internal SmartItem(FileInfo file, string destination = "", int parentID = -1)
        {
            _itemName = file.Name;
            FullName = file.FullName;
            ItemFolder = file.DirectoryName;
            Destination = destination;
            IsFile = true;
            Length = file.Length;
            Modified = file.LastWriteTime.Ticks;
            ParentId = parentID;
        }

        // Local Folder Only!
        internal SmartItem(DirectoryInfo dir, string destination = "", int parentID = -1)
        {
            _itemName = dir.Name;
            FullName = dir.FullName;
            DirectoryInfo parent = dir.Parent;
            if (parent != null) { ItemFolder = parent.FullName; }
            Destination = destination;
            ParentId = parentID;
        }
    }
}