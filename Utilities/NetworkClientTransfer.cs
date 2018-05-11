using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal abstract partial class NetworkClient
    {
        internal async Task TransferItemsAsync(object items, string destination, bool IsUpload)
        {
            TransferEvent.IsUpload = IsUpload;
            TransferEvent.Starting();
            TransferEvent.Items = IsUpload ? await _prepareForUpload(items as string[], destination.TrimEnd('/') + '/') :
                                             await _prepareForDownload(items as SmartItem[], destination.TrimEnd('\\') + '\\');
            items = null;
            if (TransferEvent.Items.Length > 0)
            {
                SmartItem[] IIDP = new SmartItem[] { }; // Items In Destination Path
                TransferEvent.Action = TransferAction.Unknown;
                TransferEvent.SessionAction = TransferAction.Unknown;
                string dstination = string.Empty; // Last Destination Path
                int j = -1;
                bool hasParent = false;
                bool mayExist = false;
                bool samePath = false;

                if (DefaultAction != TransferAction.Unknown) TransferEvents.TillCloseAction = DefaultAction;

                TransferEvent.Started();
                for (int i = 0; i < TransferEvent.Items.Length; i++)
                {
                    if (IsCanceled) break;
                    TransferEvent.Item = TransferEvent.Items[i];
                    TransferEvent.Item.HasError = false;
                    //TransferEvent.Item.Status = ItemStatus.Unknown;
                    if (dstination != TransferEvent.Item.Destination)
                    {
                        dstination = TransferEvent.Item.Destination;
                        TransferEvent.PathChanged(TransferEvent.Item.ItemFolder, dstination);

                        hasParent = (TransferEvent.Item.ParentId != -1);
                        mayExist = (!hasParent || TransferEvent.Items[TransferEvent.Item.ParentId].Exist);

                        if (mayExist)
                        {
                            if (IsUpload)
                            {
                                IIDP = await getServerItemsAsync(TransferEvent.Item.Destination);
                                if (IIDP == null) IIDP = new SmartItem[] { };
                            }
                            else IIDP = await _getLocalItemsAsync(TransferEvent.Item.Destination);

                            mayExist = (IIDP.Length != 0);
                        }
                    }

                    samePath = ((IsUpload ? TransferEvent.Item.Destination : TransferEvent.Item.ItemFolder) == BrowsedPath);

                    if (hasParent && TransferEvent.Items[TransferEvent.Item.ParentId].HasError)
                    {
                        TransferEvent.Item.HasError = true;
                        if (TransferEvent.Item.IsFile) TransferEvent.TotalTransferredFiles++;
                        else TransferEvent.TotalTransferredFolders++;
                        TransferEvent.Item.Status = ItemStatus.Ignored;
                        transferHistoryLog(TransferEvent.Item, TransferEvent.Item.ItemFolder);
                        if (samePath) TransferEvents.ItemStatusChanged(TransferEvent.Item);
                        continue;
                    }

                    j = -1;
                    if (mayExist)
                    {
                        j = ListExtensions.GetItemID(IIDP, TransferEvent.Item.Destination + TransferEvent.Item.ItemName);
                        TransferEvent.Item.Exist = (j > -1);
                    }

                    if (TransferEvent.Item.IsFile)
                    {
                        await setTransferAction(IsUpload, IIDP, j);

                        if (samePath) TransferEvents.ItemStatusChanged(TransferEvent.Item);

                        if (IsUpload)
                        {
                            string localFolder = TransferEvent.Item.ItemFolder;
                            await _uploadFileAsync();
                            uploadEnded(TransferEvent.Item, localFolder);
                        }
                        else
                        {
                            await _downloadFileAsync();
                            downloadEnded(TransferEvent.Item, TransferEvent.Item.ItemFolder);
                        }
                        TransferEvent.TotalTransferredFiles++;
                    }
                    else
                    {
                        if (IsUpload)
                            TransferEvent.Item.HasError = !(TransferEvent.Item.Exist || await CreateFolderAsync(TransferEvent.Item.ItemName, TransferEvent.Item.Destination, false));
                        else
                        {
                            TransferEvent.Item.HasError = !(TransferEvent.Item.Exist || (_createLocalFolder(TransferEvent.Item.Destination + TransferEvent.Item.ItemName)));
                            //TransferEvent.Item.Status = (!TransferEvent.Item.HasError) ? ItemStatus.Downloaded : ItemStatus.DownloadError;
                            //TransferEvents.ItemStatusChanged(TransferEvent.Item.FullName);
                        }

                        TransferEvent.TotalTransferredFolders++;
                    }
                }
            }
            TransferEvent.Ended();
        }

        private static void transferHistoryLog(SmartItem item, string localFolder)
        {
            ItemStatus HistoryStatus = item.Status;
            switch (HistoryStatus)
            {
                case ItemStatus.Ignored:
                    break;
                case ItemStatus.Uploading:
                    HistoryStatus = (!item.HasError) ? ItemStatus.Uploaded : ItemStatus.UploadError;
                    break;
                case ItemStatus.Downloading:
                    HistoryStatus = (!item.HasError) ? ItemStatus.Downloaded : ItemStatus.DownloadError;
                    break;
                case ItemStatus.Replacing:
                    HistoryStatus = (!item.HasError) ? ItemStatus.Replaced : ItemStatus.ReplaceError;
                    break;
                case ItemStatus.Resuming:
                    HistoryStatus = (!item.HasError) ? ItemStatus.Resumed : ItemStatus.ResumeError;
                    break;
            }

            AppHistory.Add(item.ItemName, item.Destination, localFolder, item.Destination, HistoryStatus);
        }

        private async Task setTransferAction(bool IsUpload, SmartItem[] IIDP, int j)
        {
            if (TransferEvent.Item.Exist)
            {
                if (TransferEvents.TillCloseAction != TransferAction.Unknown)
                    TransferEvent.Action = TransferEvents.TillCloseAction;
                else if (TransferEvent.SessionAction != TransferAction.Unknown)
                    TransferEvent.Action = TransferEvent.SessionAction;
                else TransferEvent.RequestingAction(IIDP[j], ((TransferEvent.Item.Length > 1048576) &&
                        (TransferEvent.Item.Length > IIDP[j].Length)));
            }

            switch (TransferEvent.Action)
            {
                case TransferAction.Unknown:
                    TransferEvent.Item.Status = IsUpload ? ItemStatus.Uploading : ItemStatus.Downloading;
                    break;

                case TransferAction.Ignore:
                    TransferEvent.Item.Status = ItemStatus.Ignored;
                    break;

                case TransferAction.Rename:
                    if (TransferEvent.Item.Exist)
                    {
                        string newName = await IIDP.GetNewName(IIDP[j]);
                        if (IsUpload)
                        {
                            if (await RenameItemAsync(IIDP[j], newName))
                            {
                                IIDP[j].ItemName = newName;
                                IIDP[j].FullName = IIDP[j].ItemFolder + newName;
                                TransferEvent.Item.Exist = false;
                                TransferEvent.Item.Status = ItemStatus.Uploading;
                            }
                            else { TransferEvent.Item.Status = ItemStatus.UploadError; TransferEvent.Item.HasError = true; }
                        }
                        else
                        {
                            bool renamed;

                            if (IIDP[j].IsFile) renamed = FileHelper.Rename(IIDP[j].FullName, IIDP[j].ItemFolder + @"\" + newName);
                            else renamed = DirectoryHelper.Rename(IIDP[j].FullName, IIDP[j].ItemFolder + @"\" + newName);

                            if (renamed)
                            {
                                IIDP[j].ItemName = IIDP[j].ItemName = newName;
                                IIDP[j].FullName = IIDP[j].ItemFolder + @"\" + newName;
                                TransferEvent.Item.Exist = false;
                                TransferEvent.Item.Status = ItemStatus.Downloading;
                            }
                            else { TransferEvent.Item.Status = ItemStatus.DownloadError; TransferEvent.Item.HasError = true; }
                        }
                    }
                    else TransferEvent.Item.Status = IsUpload ? ItemStatus.Uploading : ItemStatus.Downloading;
                    break;

                case TransferAction.Replace:
                    TransferEvent.Item.Status = (IsUpload) ? ItemStatus.Replacing : ItemStatus.Downloading;
                    break;

                case TransferAction.Resume:
                    TransferEvent.Item.Status = ItemStatus.Resuming;
                    break;
            }
        }

        protected async Task<SmartItem[]> _prepareForUpload(string[] items, string destination)
        {
            List<SmartItem> uploadList = new List<SmartItem>();
            IList<string> folders = new List<string>();

            await Task.Run(async delegate
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                    if (IsCanceled) break;
                    if (FileHelper.Exists(items[i]))
                    {
                        FileInfo file = null;
                        try
                        {
                            file = new FileInfo(items[i]);
                            uploadList.Add(new SmartItem(file, destination));
                            TransferEvent.TotalSize += file.Length;
                            TransferEvent.TotalFiles++;
                        }
                        catch (Exception exp) { ExceptionHelper.Log(exp); }
                        file = null;
                    }
                    else if (DirectoryHelper.Exists(items[i])) folders.Add(items[i]);
                }
                items = null;

                for (int i = 0; i < folders.Count; i++)
                {
                    if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                    if (IsCanceled) break;
                    uploadList.AddRange(await _getLocalItems(new DirectoryInfo(folders[i]), destination, -1, uploadList.Count));
                }
                folders = null;
            });

            if (IsCanceled) return new SmartItem[] { };
            return uploadList.ToArray();
        }

        protected async Task<SmartItem[]> _getLocalItems(DirectoryInfo dir, string destination, int parentID, int count)
        {
            List<SmartItem> uploadList = new List<SmartItem>();

            await Task.Run(async delegate
            {
                uploadList.Add(new SmartItem(dir, destination, parentID));

                TransferEvent.TotalFolders++;
                parentID = parentID + count + 1;
                FileInfo[] files = null;
                try
                {
                    files = dir.GetFiles();
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                        if (IsCanceled) break;
                        uploadList.Add(new SmartItem(files[i], destination + dir.Name + '/', parentID));
                        TransferEvent.TotalSize += files[i].Length;
                        TransferEvent.TotalFiles++;
                    }
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
                files = null;

                DirectoryInfo[] dirs = null;
                try
                {
                    dirs = dir.GetDirectories();
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                        if (IsCanceled) break;
                        uploadList.AddRange(await _getLocalItems(dirs[i], destination + dir.Name + '/', parentID, uploadList.Count - 1));
                    }
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
                dirs = null;
                dir = null;
            });

            if (IsCanceled) return new SmartItem[] { };
            return uploadList.ToArray();
        }

        protected async Task<SmartItem[]> _getLocalItemsAsync(string path)
        {
            List<SmartItem> items = new List<SmartItem>();

            await Task.Run(() =>
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = null;
                try
                {
                    files = dir.GetFiles();
                    for (int i = 0; i < files.Length; i++)
                        items.Add(new SmartItem(files[i]));
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
                files = null;

                DirectoryInfo[] subDirs = null;
                try
                {
                    subDirs = dir.GetDirectories();
                    for (int i = 0; i < subDirs.Length; i++)
                        items.Add(new SmartItem(subDirs[i]));
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
                subDirs = null;
                dir = null;
            });

            return items.ToArray();
        }

        protected bool _createLocalFolder(string dirName)
        {
            return !IsCanceled && DirectoryHelper.Create(dirName);
        }

        private static void uploadEnded(SmartItem item, string localFolder)
        {
            transferHistoryLog(item, localFolder);

            SmartItem eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem == null) return;

            eitem.Status = item.Status;
            eitem.Length = eitem.Transferred;
            eitem.FileSize = null;
            eitem.HasError = item.HasError;
            eitem.ProgressBarEnabled = false;

            switch (eitem.Status)
            {
                case ItemStatus.Ignored:
                    break;
                case ItemStatus.Uploading:
                    eitem.Status = (eitem.HasError) ? ItemStatus.UploadError : ItemStatus.Uploaded;
                    break;
                case ItemStatus.Replacing:
                    eitem.Status = (eitem.HasError) ? ItemStatus.ReplaceError : ItemStatus.Replaced;
                    break;
                case ItemStatus.Resuming:
                    eitem.Status = (eitem.HasError) ? ItemStatus.ResumeError : ItemStatus.Resumed;
                    break;
            }
            TransferEvents.ItemStatusChanged(eitem);
        }

        private static void downloadEnded(SmartItem item, string localFolder)
        {
            transferHistoryLog(item, localFolder);

            SmartItem eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem == null) return;

            eitem.Status = item.Status;
            eitem.HasError = item.HasError;
            eitem.ProgressBarEnabled = false;

            if (eitem.Status != ItemStatus.Ignored)
                eitem.Status = (eitem.HasError) ? ItemStatus.DownloadError : ItemStatus.Downloaded;
            TransferEvents.ItemStatusChanged(eitem);
        }
    }
}