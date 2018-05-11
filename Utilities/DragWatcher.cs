using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class DragWatcher
    {
        internal delegate void DragWatcherHandler(string path);
        internal static event DragWatcherHandler OnDraged;

        internal static string Source { get; set; }

        private static string lastPath { get; set; }
        private static IList<FileSystemWatcher> watchList;

        static DragWatcher()
        {
            _set();
        }

        private static void _set()
        {
            watchList = new List<FileSystemWatcher>(5);
        }

        internal static bool Start()
        {
            if (lastPath != null) FileHelper.Delete(lastPath + @"\" + Source);

            if (hasTmpFile() && canWatch())
            {
                for (int i = 0; i < watchList.Count; i++) watchList[i].EnableRaisingEvents = true;
                return true;
            }

            return false;
        }

        private static bool hasTmpFile()
        {
            string tmpFile = DirectoryHelper.Temp + DragWatcher.Source;

            if (FileHelper.Exists(tmpFile)) return true;
            else
            {
                using (FileStream fs = FileHelper.Create(tmpFile))
                {
                    if (fs != null)
                    {
                        File.SetAttributes(tmpFile, FileAttributes.Hidden | FileAttributes.System);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool canWatch()
        {
            DriveInfo[] drives = DirectoryHelper.GetDrives();
            if (drives.Length > watchList.Count)
            {
                clear();

                FileSystemWatcher watcher;
                for (int i = 0; i < drives.Length; i++)
                {
                    watcher = new FileSystemWatcher(drives[i].Name, Source);
                    watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                    watcher.Created += new FileSystemEventHandler(draged);
                    watcher.Changed += new FileSystemEventHandler(draged);
                    watcher.IncludeSubdirectories = true;
                    watchList.Add(watcher);
                }
            }

            return (watchList.Count != 0);
        }

        private static void clear()
        {
            for (int i = 0; i < watchList.Count; i++) watchList[i].Dispose();
            watchList.Clear();
        }

        private static void draged(string path)
        {
            if (OnDraged != null) new DragWatcherHandler(OnDraged)(path);
        }

        private static void draged(object sender, FileSystemEventArgs e)
        {
            string path = DirectoryHelper.GetPath(e.FullPath);
            lastPath = path;
            if (!path.NullEmpty())
            {
                deleteSource(e.FullPath);
                stop();
                draged(path);
            }
        }

        private static void stop()
        {
            for (int i = 0; i < watchList.Count; i++)
                watchList[i].EnableRaisingEvents = false;
        }

        private static void deleteSource(string path)
        {
            Task.Run(async delegate
            {
                FileHelper.Delete(path);
                bool exists = true;
                int t = 0;
                while (exists && (t < 10))
                {
                    await Task.Delay(200);
                    exists = !FileHelper.Delete(path);
                    t++;
                }
            });
        }
    }
}