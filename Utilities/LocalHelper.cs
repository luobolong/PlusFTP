using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static partial class LocalHelper
    {
        private static FileSystemWatcher fsw;

        internal static ObservableCollection<SmartItem> Items;
        internal static string CurrentPath { get; private set; }
        internal static string Home;
        internal static string ThisPC;
        internal static string LastPath;
        internal static string ParentPath;

        static LocalHelper()
        {
            _set();
        }

        private static void _set()
        {
            Home = DirectoryHelper.DesktopDirectory;
            ThisPC = AppLanguage.Get("LangThisPC");

            fsw = new FileSystemWatcher();
            fsw.Filter = "*.*";
            fsw.NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fsw.Changed += new FileSystemEventHandler(ListChanged);
            fsw.Created += new FileSystemEventHandler(ListChanged);
            fsw.Deleted += new FileSystemEventHandler(ListChanged);
            fsw.Renamed += new RenamedEventHandler(ListChanged);
        }

        /*internal static string Up(string path)
        {
            if (!path.NullEmpty())
            {
                if (path == myPC) return Home;

                try { return (new DirectoryInfo(path).Parent).FullName; }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return string.Empty;
        }*/

        internal static async void Delete(SmartItem[] items)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].IsFile) FileHelper.Delete(items[i].FullName, true);
                    else DirectoryHelper.Delete(items[i].FullName);
                }
            });
        }

        internal static bool NameExists(string name)
        {
            if (Items.Count == 0) return false;

            bool exists = false;
            Parallel.For(0, Items.Count, (i, loopState) =>
            {
                if (Items[i].ItemName == name)
                {
                    exists = true;
                    loopState.Stop();
                }
            });

            return exists;
        }

        internal static async Task<bool> SetItemsAsync(string path)
        {
            if (path.NullEmpty()) return false;
            if ((path != LocalHelper.ThisPC) && !DirectoryHelper.Exists(path)) return false;

            fsw.EnableRaisingEvents = false;
            List<SmartItem> items = new List<SmartItem>();
            bool listed = false;
            await Task.Run(() =>
            {
                if (path == Home)
                {
                    SmartItem item = new SmartItem();
                    item.FullName = item.ItemName = LocalHelper.ThisPC;
                    item.ItemIcon = IconHelper.Get((int)ImageList.SpecialFolderCSIDL.DRIVES);

                    items.Add(item);
                    //items.Add(documentsPath);
                    listed = true;
                }

                if (path == LocalHelper.ThisPC)
                {
                    DriveInfo[] localDrives = DirectoryHelper.GetDrives();
                    try
                    {
                        for (int i = 0; i < localDrives.Length; i++)
                        {
                            SmartItem Ditem = new SmartItem(new DirectoryInfo(localDrives[i].Name));
                            Ditem.ItemName = (localDrives[i].IsReady ? localDrives[i].VolumeLabel : string.Empty) + " (" + localDrives[i].Name.TrimEnd('\\') + ')' + (localDrives[i].IsReady ?
                                 " " + AppLanguage.Get("LangFreeSpaceX").FormatC(SizeUnit.Parse(localDrives[i].AvailableFreeSpace)) : string.Empty);
                            items.Add(Ditem);
                        }
                        listed = true;
                    }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }
                    localDrives = null;
                }
                else
                {
                    DirectoryInfo Localdir = null;
                    try { Localdir = new DirectoryInfo(path); }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }

                    if (Localdir != null)
                    {
                        listed = true;
                        DirectoryInfo[] dirs = null;
                        try
                        {
                            dirs = Localdir.GetDirectories();
                            for (int i = 0; i < dirs.Length; i++)
                                if ((dirs[i].Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                                    items.Add(new SmartItem(dirs[i]));
                        }
                        catch (Exception exp) { ExceptionHelper.Log(exp); }
                        dirs = null;

                        FileInfo[] files = null;
                        try
                        {
                            files = Localdir.GetFiles();
                            for (int i = 0; i < files.Length; i++)
                                if ((files[i].Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                                    items.Add(new SmartItem(files[i]));
                        }
                        catch (Exception exp) { ExceptionHelper.Log(exp); }
                        files = null;
                    }
                    Localdir = null;
                }
            });

            if (listed)
            {
                if (DirectoryHelper.Exists(path))
                {
                    if (fsw.Path != path) fsw.Path = path;
                    fsw.EnableRaisingEvents = true;
                }

                LastPath = CurrentPath;
                CurrentPath = path;

                if (path == LocalHelper.ThisPC) ParentPath = Home;
                else ParentPath = DirectoryHelper.GetParentPath(path);

                Items = new ObservableCollection<SmartItem>(items);
                return true;
            }
            else return false;
        }
    }
}