using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal partial class NetworkClient : IDisposable
    {
        internal enum NetworkTextEncoding : ushort { ASCII = 0, UTF8 = 1 };

        public const int BUFFER_SIZE = 8192; //20874836
        protected const int RETRY_TIMES = 60;

        public static SmartCollection Items { get; protected set; }
        public static string BrowsedPath { get; protected set; }
        public static string HomePath { get; protected set; }
        internal static string Host;
        internal static string UserName;
        internal static string Password;
        internal static int Port;
        internal static bool CacheFolders;
        internal static bool IsUTF8;
        protected static Dictionary<string, SmartCollection> CachedFolders;

        public bool IsDisposed { get; protected set; }
        public bool IsConnected { get; protected set; }
        public bool IsChild { get; protected set; }

        internal bool? IsUnix;
        internal TransferEvents TransferEvent;
        internal TransferAction DefaultAction;
        internal int BufferSize; //Max 1043741824

        protected int connectTicker;
        internal int ConnectTicker { get { return connectTicker; } set { setConnectTicker(value); } }

        internal bool IsCanceled;
        internal bool FlagSkipIt;
        internal bool Paused;
        internal bool DisplayEvents;

        protected NetworkClient parent;
        protected Encoding _encoding;
        protected Socket ControlSocket;
        protected Stream ControlStream;
        protected Socket DataSocket;
        protected Stream DataStream;
        protected bool needToAbort;
        protected bool? isIPV6;
        protected bool single;

        static NetworkClient()
        {
            _set();
        }

        private static void _set()
        {
            CachedFolders = new Dictionary<string, SmartCollection>();
            Items = new SmartCollection();
            BrowsedPath = string.Empty;
            HomePath = string.Empty;
        }

        internal NetworkClient()
        {
            TransferEvent = new TransferEvents();
            DefaultAction = TransferAction.Replace;
            _encoding = Encoding.ASCII;
            BufferSize = BUFFER_SIZE;
            IsDisposed = false;
            DisplayEvents = true;
            IsConnected = false;
        }

        internal void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    closeDataConnection();
                    closeConnection();
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal async void Disconnect(bool getResponse = true, bool DisposeIt = true)
        {
            await DisconnectAsync(getResponse, DisposeIt);
        }

        internal async Task<bool> SetItemsAsync(string path)
        {
            if (CacheFolders)
            {
                PathHelper.AddEndningSlash(ref path);

                if (CachedFolders.ContainsKey(BrowsedPath))
                {
                    CachedFolders[BrowsedPath] = new SmartCollection();
                    CachedFolders[BrowsedPath].SetItems(Items);
                }

                if (CachedFolders.ContainsKey(path))
                {
                    BrowsedPath = path;
                    Items = CachedFolders[path];
                    return true;
                }
            }

            SmartItem[] _ServerItems = await getServerItemsAsync(path, true);
            if (_ServerItems != null)
            {
                Items.SetItems(_ServerItems);
                if (CacheFolders) CachedFolders.Add(path, null);
                return true;
            }

            return false;
        }

        internal static void ClearCachedPath(string path)
        {
            if (CacheFolders && CachedFolders.ContainsKey(path)) CachedFolders.Remove(path);
        }

        internal static void ClearCached()
        {
            if (CacheFolders) CachedFolders.Clear();
        }

        internal static SmartItem GetServerItem(string name, string path, SmartItem item = null, bool clearCacheAfter = false)
        {
            if ((path != BrowsedPath) || ((Items.Count == 0) && (item == null))) return null;

            int id = Items.GetID(path + name);
            if (clearCacheAfter) Items.ClearCache();

            if (id == -1) { if (item != null) return InsertItem(item, false); }
            else
            {
                try
                {
                    if (item != null) Items[id] = item;
                    return Items[id];
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return null;
        }

        protected static SmartItem InsertItem(SmartItem item, bool checkPath = true)
        {
            if (checkPath && (item.ItemFolder != BrowsedPath)) return null;

            try
            {
                int id = Items.GetLastFolderID();
                Items.Insert(id, item);
                return Items[id];
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return null;
        }

        protected static Stream setDeflateStream(Stream stream, bool isRead)
        {
            if (stream == null) return null;
            DeflateStream s = null;
            try
            {
                if (isRead)
                {
                    s = new DeflateStream(stream, CompressionMode.Decompress, false);
                    s.BaseStream.ReadByte();
                    s.BaseStream.ReadByte();
                }
                else
                {
                    s = new DeflateStream(stream, CompressionMode.Compress, false);
                    s.BaseStream.WriteByte(120);
                    s.BaseStream.WriteByte(218);
                }
            }
            catch (Exception exp) { s = null; ExceptionHelper.Log(exp); }

            return s;
        }

        protected void InfoMessage(string message, MessageType messageType)
        {
            if (!DisplayEvents || message.NullEmpty()) return;
            AppMessage.Add(message, messageType);
        }

        protected async Task<Socket> createStreamAsync(string host, int port)
        {
            if (IsCanceled) return null;
            Socket s = null;

            if ((ProxyClient.Proxy != null))
            {
                s = await ProxyClient.ConnectAsync(host, port);
                if (s != null)
                {
                    InfoMessage("Proxy connection established.", MessageType.Info);
                }
                else InfoMessage("unable to Connect to the proxy", MessageType.Error);
            }
            else
            {
                Socket socket = await SocketHelper.ConnectAsync(host, port);
                if (socket != null)
                {
                    s = socket;
                    if (!isIPV6.HasValue) isIPV6 = SocketHelper.IsIPV6();
                }
            }

            return s;
        }

        protected void closeConnection()
        {
            if (ControlSocket != null)
            {
                ControlSocket.Close();
                ControlSocket = null;
            }

            if (ControlStream != null)
            {
                ControlStream.Dispose();
                ControlStream = null;
            }
        }

        protected void closeDataConnection()
        {
            needToAbort = false;

            if (DataSocket != null)
            {
                DataSocket.Close();
                DataSocket = null;
            }

            if (DataSocket != null)
            {
                DataStream.Dispose();
                DataStream = null;
            }
        }

        protected async Task _uploadFileAsync()
        {
            string source = TransferEvent.Item.FullName;
            TransferEvent.Item.FullName = TransferEvent.Item.Destination + TransferEvent.Item.ItemName;
            TransferEvent.Item.ItemFolder = TransferEvent.Item.Destination;
            bool notIgnored = (!TransferEvent.Item.HasError && (TransferEvent.Item.Status != ItemStatus.Ignored));

            if (notIgnored)
            {
                SmartItem item = GetServerItem(TransferEvent.Item.ItemName, TransferEvent.Item.ItemFolder, TransferEvent.Item);
                if (item != null)
                {
                    item.Modified = DateTime.Now.Ticks;
                    item.LastModified = null;
                    item.ProgressBarEnabled = (item.Length > 1048576);
                }

                await _sendFileAsync(source);
            }
        }

        protected async Task _downloadFileAsync()
        {
            if (!TransferEvent.Item.HasError && (TransferEvent.Item.Status != ItemStatus.Ignored))
            {
                TransferEvent.Item.ProgressBarEnabled = ((GetServerItem(TransferEvent.Item.ItemName, TransferEvent.Item.ItemFolder) != null) && (TransferEvent.Item.Length > 1048576));
                await _getFileAsync();
            }
        }

        internal virtual Task<NetworkClient> NewInstance()
        {
            throw new NotImplementedException();
        }

        internal virtual Task<bool> ConnectAsync(bool reconnect = false, bool tryReconnect = true)
        {
            throw new NotImplementedException();
        }

        internal virtual Task DisconnectAsync(bool getResponse = true, bool DisposeIt = true)
        {
            throw new NotImplementedException();
        }

        internal virtual Task<SmartItem[]> GetServerFoldersAsync(string path)
        {
            throw new NotImplementedException();
        }

        internal virtual Task<bool> DeleteItemAsync(SmartItem item)
        {
            throw new NotImplementedException();
        }

        internal virtual Task<bool> RenameItemAsync(SmartItem item, string toName)
        {
            throw new NotImplementedException();
        }

        internal virtual Task<bool> MoveItemAsync(SmartItem item, string toPath)
        {
            throw new NotImplementedException();
        }

        internal virtual Task<bool> ChangePermissionsync(SmartItem item, string permission)
        {
            throw new NotImplementedException();
        }

        internal virtual Task<bool> CreateFolderAsync(string name, string path, bool replaceSpaces = true)
        {
            throw new NotImplementedException();
        }

        internal virtual Task Abort()
        {
            throw new NotImplementedException();
        }

        protected virtual Task<SmartItem[]> _prepareForDownload(SmartItem[] items, string destination)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<SmartItem[]> getServerItemsAsync(string path, bool setCurrentPath = false)
        {
            throw new NotImplementedException();
        }

        protected virtual Task _sendFileAsync(string source)
        {
            throw new NotImplementedException();
        }

        protected virtual Task _getFileAsync()
        {
            throw new NotImplementedException();
        }

        protected virtual void setConnectTicker(int value)
        {
            connectTicker = value;
        }
    }
}