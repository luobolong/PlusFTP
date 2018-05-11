using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Hani.Utilities;
using System.Security.Authentication;

namespace Hani.FTP
{
    internal sealed partial class FTPClient : NetworkClient
    {
        private const string CLIENT_NAME = "PlusFTP 2.0";
        private const int noopAfter = 290;

        internal static FTPEncryption Encryption;
        internal static bool EnableMODEZ;

        internal bool IsMLSD { get; private set; }

        private string[] serverFeat;
        private string tmpResponsed;
        private bool storeResponse;
        private bool commandIn;
        private bool secureConnection;
        private bool isRMDA;
        private bool isMODEZ;

        internal FTPClient()
            : base()
        {
            tmpResponsed = string.Empty;
        }

        static FTPClient()
        {
            _set();
        }

        private static void _set()
        {
            Encryption = new FTPEncryption(EncryptionType.PlainType, FTPSslProtocol.None);
        }

        internal override async Task<NetworkClient> NewInstance()
        {
            if (single == true) return null;

            FTPClient client = new FTPClient();

            client.serverFeat = serverFeat;
            client.BufferSize = BufferSize;
            client.IsChild = true;

            //InfoMsg("Establishing A New Connection", MessageType.Info);

            client.DisplayEvents = false;
            if (await client.ConnectAsync(false, false))
            {
                client.parent = this;
                client.DisplayEvents = true;
                client.isIPV6 = isIPV6;
                client.isRMDA = isRMDA;
                client.IsUnix = IsUnix;
                /*if (secureConnection) InfoMsg("New secure connection established.", MessageType.Info);
                else InfoMsg("New connection established.", MessageType.Info);*/

                return client;
            }

            client.Dispose();

            single = true;
            InfoMessage("No Multi-Connections", MessageType.Info);
            return null;
        }

        internal override async Task<bool> ConnectAsync(bool reconnect = false, bool tryReconnect = true)
        {
            if (UserName.NullEmpty()) UserName = "anonymous";
            if (Password.NullEmpty()) Password = CryptoHashing.Encrypt("anonymous@anonymous.org");
            if (Port == 0) Port = 21;

            Connecting();
            if (reconnect)
            {
                closeConnection();
                closeDataConnection();
                InfoMessage("Connection Dropped! Reconnecting...", MessageType.Info);
            }
            else InfoMessage("Connecting to " + Host + ":" + Port.ToString(), MessageType.Info);

            secureConnection = (Encryption.Type == EncryptionType.ImplicitType);
            ControlSocket = await createStreamAsync(Host, Port);

            if ((ControlSocket == null) && tryReconnect)
            {
                for (int i = 1; i < RETRY_TIMES; i++)
                {
                    if (IsCanceled) return false;

                    InfoMessage("Reconnecting after " + (5 * i) + " seconds...{" + (RETRY_TIMES - i) + "}", MessageType.Info);
                    await Task.Delay(i * 5000);

                    ControlSocket = await createStreamAsync(Host, Port);

                    if (ControlSocket != null) break;
                }
            }

            if (ControlSocket != null)
            {
                ControlStream = new NetworkStream(ControlSocket);
                if (secureConnection)
                {
                    if (Encryption.Protocol == FTPSslProtocol.TLS)
                        ControlStream = await SslClient.ConnectAsync(ControlStream, Host, SslProtocols.Tls12);
                    else
                        ControlStream = await SslClient.ConnectAsync(ControlStream, Host, SslProtocols.Ssl3);
                }
                IsConnected = (ControlStream != null);
            }

            IsConnected = ((ControlStream != null) && (await getResponseAsync() == 220));

            if (IsConnected && (Encryption.Type == EncryptionType.ExplicitType))
            {
                IsConnected = false;
                string protocol = "TLS";
                if (Encryption.Protocol == FTPSslProtocol.SSL) protocol = "SSL";

                if (await _commandAuthAsync(protocol))
                {
                    if (Encryption.Protocol == FTPSslProtocol.TLS)
                        ControlStream = await SslClient.ConnectAsync(ControlStream, Host, SslProtocols.Tls12);
                    else
                        ControlStream = await SslClient.ConnectAsync(ControlStream, Host, SslProtocols.Ssl3);
                    secureConnection = IsConnected = (ControlStream != null);
                }
            }

            if (!IsConnected || !(await _logInAsync()))
            {
                await _failedToConnectAsync();
                return false;
            }

            if (secureConnection) InfoMessage("Secure Connection Established.", MessageType.Info);
            else InfoMessage("Connection Established.", MessageType.Info);

            await _afterConnectAsync();

            Connected();
            return true;
        }

        internal override async Task DisconnectAsync(bool getResponse = true, bool DisposeIt = true)
        {
            if (IsDisposed) return;

            IsCanceled = true;

            if (IsConnected)
            {
                await _commandQuitAsync(getResponse);
                IsConnected = false;
            }

            if (getResponse) InfoMessage("Disconnected", MessageType.Info);

            Disconnected();
            if (DisposeIt) Dispose();
        }

        internal override async Task<SmartItem[]> GetServerFoldersAsync(string path)
        {
            PathHelper.AddEndningSlash(ref path);
            int code = (IsMLSD) ? await commandMlsdAsync(path) : await commandListAsync(path);

            if ((code == 150) || (code == 125) || (code == 226))
                return await this.ParseFoldersAsync(path, await receiveDataAsync(code != 226));
            else if (await retryAsync()) return await GetServerFoldersAsync(path);

            return null;
        }

        internal override async Task<bool> DeleteItemAsync(SmartItem item)
        {
            SmartItem eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.Deleting;
                TransferEvents.ItemStatusChanged(eitem);
            }

            bool deleted = false;
            if (item.IsFile) { if (await _commandDeleAsync(item.FullName)) { deleted = true; } }
            else if (isRMDA) { if (await _commandRmdaAsync(item.FullName)) { deleted = true; } }
            else if (await _deleteFolderAsync(item.FullName)) { deleted = true; }

            if (deleted)
            {
                AppHistory.Add(item.ItemName, item.ItemFolder, item.ItemFolder, " ", ItemStatus.Deleted);
                eitem = GetServerItem(item.ItemName, item.ItemFolder);
                if (eitem != null) Items.Remove(eitem);

                return true;
            }
            else if (await retryAsync()) { return await DeleteItemAsync(item); }

            AppHistory.Add(item.ItemName, item.ItemFolder, item.ItemFolder, " ", ItemStatus.DeleteError);

            eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.DeleteError;
                TransferEvents.ItemStatusChanged(eitem);
            }

            return false;
        }

        internal override async Task<bool> RenameItemAsync(SmartItem item, string toName)
        {
            string newName = item.ItemFolder + toName;

            SmartItem eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.Renaming;
                TransferEvents.ItemStatusChanged(eitem);
            }

            if (await commandRnfrAsync(item.FullName) && await commandRntoAsync(newName))
            {
                AppHistory.Add(toName, item.ItemFolder, item.ItemName, toName, ItemStatus.Renamed);
                eitem = GetServerItem(item.ItemName, item.ItemFolder, null, true);
                if (eitem != null)
                {
                    eitem.ItemName = toName;
                    eitem.FullName = newName;
                    eitem.Status = ItemStatus.Renamed;
                    TransferEvents.ItemStatusChanged(eitem);
                }
                return true;
            }
            else if (await retryAsync()) return await RenameItemAsync(item, toName);
            AppHistory.Add(item.ItemName, item.ItemFolder, item.ItemName, toName, ItemStatus.RenameError);

            eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.RenameError;
                TransferEvents.ItemStatusChanged(eitem);
            }

            return false;
        }

        internal override async Task<bool> MoveItemAsync(SmartItem item, string toPath)
        {
            SmartItem eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.Moving;
                TransferEvents.ItemStatusChanged(eitem);
            }

            if (await commandRnfrAsync(item.FullName) && await commandRntoAsync(toPath + item.ItemName))
            {
                AppHistory.Add(item.ItemName, toPath, item.ItemFolder, toPath, ItemStatus.Moved);
                eitem = GetServerItem(item.ItemName, item.ItemFolder);
                if (eitem != null) Items.Remove(eitem);
                return true;
            }
            else if (await retryAsync()) return await MoveItemAsync(item, toPath);

            AppHistory.Add(item.ItemName, item.ItemFolder, item.ItemFolder, toPath, ItemStatus.MoveError);

            eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.MoveError;
                TransferEvents.ItemStatusChanged(eitem);
            }

            return false;
        }

        internal override async Task<bool> ChangePermissionsync(SmartItem item, string permission)
        {
            SmartItem eitem;
            if (await _commandChmodAsync(item.FullName, permission))
            {
                eitem = GetServerItem(item.ItemName, item.ItemFolder);
                if (eitem != null)
                {
                    eitem.Status = ItemStatus.PermissionChanged;
                    TransferEvents.ItemStatusChanged(eitem);
                }

                AppHistory.Add(item.ItemName, item.ItemFolder, item.Permissions, permission, ItemStatus.PermissionChanged);
                item.Permissions = permission;
                return true;
            }
            else if (await retryAsync()) return await ChangePermissionsync(item, permission);

            AppHistory.Add(item.ItemName, item.ItemFolder, item.Permissions, permission, ItemStatus.PermissionError);

            eitem = GetServerItem(item.ItemName, item.ItemFolder);
            if (eitem != null)
            {
                eitem.Status = ItemStatus.PermissionError;
                TransferEvents.ItemStatusChanged(eitem);
            }

            return false;
        }

        internal override async Task<bool> CreateFolderAsync(string name, string path, bool replaceSpaces = true)
        {
            if (path.NullEmpty()) return false;

            SmartItem item = GetServerItem(name, path);
            if (item == null) item = InsertItem(new SmartItem(name, path, DateTime.Now.Ticks));

            if (item != null)
            {
                item.Status = ItemStatus.Creating;
                TransferEvents.ItemStatusChanged(item);
            }

            bool created = await _createServerFolderAsync(path + name);
            AppHistory.Add(name, path, " ", path + name, (created) ? ItemStatus.Created : ItemStatus.CreateError);
            item = GetServerItem(name, path);
            if (item != null)
            {
                item.HasError = !created;
                item.Status = (created) ? ItemStatus.Created : ItemStatus.CreateError;
                TransferEvents.ItemStatusChanged(item);
            }

            return created;
        }

        internal override async Task Abort()
        {
            IsCanceled = true;
            await Task.Delay(200);
            await commandAbortAsync();
            IsCanceled = false;
        }

        protected override async void setConnectTicker(int value)
        {
            connectTicker = value;

            if ((connectTicker == noopAfter) && (await executeCommandAsync("NOOP") != 200))
            {
                IsConnected = true;
                AppMessage.Add("Connection dropped. Will reconnect when needed.", MessageType.Info);
            }
        }

        private async Task<Stream> setupDataStreamAsync(bool isRead)
        {
            if (secureConnection)
            {
                if (Encryption.Protocol == FTPSslProtocol.TLS)
                    DataStream = await SslClient.ConnectAsync(DataStream, Host, SslProtocols.Tls12);
                else
                    DataStream = await SslClient.ConnectAsync(DataStream, Host, SslProtocols.Ssl3);
            }

            if (isMODEZ) DataStream = setDeflateStream(DataStream, isRead);

            return DataStream;
        }

        private async Task<bool> setupDataConnectionAsync()
        {
            if (IsCanceled) return false;
            needToAbort = true;

            string host = string.Empty;
            int port = 0;

            if (isIPV6.HasValue && isIPV6.Value)
            {
                if (await _commandEpsvAsync() && FTPDataConnectionParser.EPSV.Parse(tmpResponsed, ref port))
                    DataSocket = await createStreamAsync(Host, port);
            }
            else if (await _commandPasvAsync() && FTPDataConnectionParser.PASV.Parse(tmpResponsed, ref host, ref port))
                DataSocket = await createStreamAsync(host, port);

            tmpResponsed = null;

            if (DataSocket != null)
            {
                DataStream = new NetworkStream(DataSocket);
                return true;
            }

            /*PORT
            Syntax: PORT a1,a2,a3,a4,p1,p2
            Specifies the host and port to which the server should connect for the next file transfer. This is interpreted as IP address a1.a2.a3.a4, port p1*256+p2. */
            //System.Net.IPAddress[] a = System.Net.Dns.GetHostEntry(Environment.MachineName).AddressList;

            await commandAbortAsync();
            return false;
        }

        private async Task<string[]> _receiveMsgAsync(bool force = false)
        {
            List<string> responses = new List<string>(3);
            if (IsCanceled && !force) return responses.ToArray();

            byte[] buffer = new byte[512];
            string response = string.Empty;
            string line = string.Empty;
            int received, index;
            bool done = false;

            await Task.Run(() =>
            {
                do
                {
                    received = 0;
                    if (!IsCanceled || force)
                    {
                        try { received = ControlStream.Read(buffer, 0, buffer.Length); }
                        catch { }
                    }
                    if (received == 0) break;

                    response += _encoding.GetString(buffer, 0, received);

                    if (response.Contains("\n"))
                    {
                        while (response.Length > 0)
                        {
                            index = response.Index("\r");
                            if (index == -1) break;

                            line = response.Substring(0, index);

                            if (response.Length > 1)
                            {
                                if (line.Length > 0) responses.Add(line);
                                response = response.Remove(0, line.Length + 2);
                            }
                        }

                        if ((responses.Count > 0) &&
                            (responses[responses.Count - 1].Length > 3) &&
                            (responses[responses.Count - 1][3] == ' '))
                            done = true;
                    }
                }
                while (!done);
            });

            return responses.ToArray();
        }

        private async Task<string> receiveDataAsync(bool getResponse)
        {
            if (IsCanceled) return string.Empty;

            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[BUFFER_SIZE];
            int received;

            DataStream = await setupDataStreamAsync(true);

            await Task.Run(() =>
            {
                while (true)
                {
                    received = 0;
                    try { if (!IsCanceled) received = DataStream.Read(buffer, 0, BUFFER_SIZE); }
                    catch { }
                    if (received == 0) break;
                    ms.Write(buffer, 0, received);
                }
            });
            closeDataConnection();

            if (IsCanceled) return null;

            string _data = _encoding.GetString(ms.ToArray()).Replace("\r\n", "\n");
            ms.Dispose();

            if (!getResponse || (await getResponseAsync() == 226)) return _data;
            else return null;
        }

        private async Task<bool> _logInAsync()
        {
            int userCode = await _commandUserAsync(UserName);
            return (((userCode == 230) || ((userCode == 331) && (await _commandPassAsync(Password)))) && (await secureDataConnection()) && (await _setHomeDirectoryAsync()));
        }

        private async Task _failedToConnectAsync()
        {
            await DisconnectAsync(IsConnected, false);
            InfoMessage("Unable to Connect.", MessageType.Error);
            FailedToConnect();
        }

        private async Task<bool> _setHomeDirectoryAsync()
        {
            if (IsChild) return true;

            if (!IsCanceled && !(await _commandPwdAsync()))
            {
                InfoMessage("NO directory has been set for " + UserName, MessageType.Warning);
                return false;
            }
            return true;
        }

        private async Task<bool> secureDataConnection()
        {
            if (secureConnection || (Encryption.Type == EncryptionType.ExplicitType))
            {
                if (!(await _commandPbszAsync()) || !(await _commandProtAsync()))
                {
                    InfoMessage("Unable to Establish a Secure Data Connection!", MessageType.Error);
                    await _failedToConnectAsync();
                    return false;
                }
            }
            return true;
        }

        private async Task _afterConnectAsync()
        {
            if (await _commandFeatAsync())
            {
                //await _commandClntAsync();
                await _setTextEncodingAsync(NetworkTextEncoding.UTF8);
                isMODEZ = EnableMODEZ && (await _commandModezAsync());
                IsMLSD = isFeat("MLSD");
                isRMDA = isFeat("RMDA");
            }

            //await executeCommandAsync("HELP");
            // Default Binary transfer Mode
            await _commandTypeAsync(FTPTransferMode.Binary);
        }

        private async Task<bool> _reconnectAsync()
        {
            if (await ConnectAsync(true))
            {
                await _commandCwdAsync(BrowsedPath, true);
                return true;
            }
            return false;
        }

        private async Task<bool> retryAsync()
        {
            return (!IsCanceled && !IsConnected && !IsDisposed && (await _reconnectAsync()));
        }

        private async Task<int> executeCommandAsync(string command, string data = null, bool getResponse = true, bool force = false)
        {
            ConnectTicker = 0;
            if (IsCanceled && !force) { commandIn = false; return 0; }

            int response = 0;
            string fullCommand;
            if (data.NullEmpty()) fullCommand = command;
            else fullCommand = command + ' ' + data;

            if (commandIn) while (commandIn && !IsCanceled) { await Task.Delay(200); }

            commandIn = true;
            byte[] buffer = _encoding.GetBytes(fullCommand + "\r\n");
            try { await ControlStream.WriteAsync(buffer, 0, buffer.Length); }
            catch { commandIn = false; return 0; }

            if (getResponse)
            {
                bool notify = true;
                if ("PASS FEAT CLNT SYST".Contains(command))
                {
                    if (command == "PASS") InfoMessage("PASS *** Hidden ***", MessageType.Sent);
                    else notify = false;
                }
                else InfoMessage(fullCommand, MessageType.Sent);

                response = await getResponseAsync(notify, force);
            }

            commandIn = false;
            return response;
        }

        private async Task<int> getResponseAsync(bool notify = true, bool force = false)
        {
            if (IsCanceled && !force) return 0;

            string[] responses = await _receiveMsgAsync(force);
            int responseCode = 0;

            if (responses.Length == 0)
            {
                IsConnected = false;
                return 0;
            }
            else
            {
                for (int i = 0; i < responses.Length; i++)
                {
                    if (notify) InfoMessage(responses[i], MessageType.Received);

                    responseCode = (responses[i].Length > 2) ? responses[i].Substring(0, 3).Int() : 0;
                    if ((responses[i].Length > 2) && (responseCode > 0))
                    {
                        //421 Too many users are connected, please try again later.
                        //421 Timeout - try typing a little faster next time
                        //421 Server is going offline
                        if (responseCode == 421)
                        {
                            IsConnected = false;
                            return 0;
                        }
                    }
                }
            }

            if (storeResponse) tmpResponsed = string.Join("\n", responses);

            storeResponse = false;
            responses = null;
            return responseCode;
        }

        private bool isFeat(string feat)
        {
            return serverFeat.Contains(feat);
        }

        private async Task _setTextEncodingAsync(NetworkTextEncoding encoding)
        {
            if (IsUTF8 && isFeat("UTF8") && (await _commandOptsAsync("UTF8 " + (encoding == NetworkTextEncoding.UTF8 ? "ON" : "OFF"))))
                _encoding = Encoding.UTF8;
        }

        protected override async Task<SmartItem[]> getServerItemsAsync(string path, bool setCurrentPath = false)
        {
            PathHelper.AddEndningSlash(ref path);
            int code = (IsMLSD) ? await commandMlsdAsync(path) : await commandListAsync(path);

            if ((code == 150) || (code == 125) || (code == 226))
            {
                if (setCurrentPath) BrowsedPath = path;
                return await this.ParseAsync(path, await receiveDataAsync(code != 226));
            }
            else if (await retryAsync()) return await getServerItemsAsync(path, setCurrentPath);

            return null;
        }

        private async Task<bool> _createServerFolderAsync(string dirName)
        {
            if (await _commandMkdAsync(dirName)) return true;
            else if (await retryAsync()) return await _createServerFolderAsync(dirName);

            return false;
        }

        private async Task<bool> _deleteFolderAsync(string path)
        {
            SmartItem[] itemsList = await getServerItemsAsync(path);
            if (itemsList != null)
            {
                for (int i = 0; i < itemsList.Length; i++)
                {
                    if (IsCanceled) return false;
                    if (!IsConnected) return false;
                    await DeleteItemAsync(itemsList[i]);
                }

                return await _commandRmdAsync(path);
            }
            return false;
        }

        protected override async Task<SmartItem[]> _prepareForDownload(SmartItem[] items, string destination)
        {
            List<SmartItem> downloadList = new List<SmartItem>();
            List<SmartItem> folders = new List<SmartItem>();

            await Task.Run(async delegate
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                    if (IsCanceled) break;
                    if (items[i].IsFile)
                    {
                        items[i].Destination = destination;
                        items[i].ParentId = -1;
                        downloadList.Add(items[i]);

                        TransferEvent.TotalSize += items[i].Length;
                        TransferEvent.TotalFiles++;
                    }
                    else folders.Add(items[i]);
                }

                for (int i = 0; i < folders.Count; i++)
                {
                    if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                    if (IsCanceled) break;
                    downloadList.AddRange(await _getServerItems(items[i], destination, -1, downloadList.Count));
                }

                items = null;
                folders = null;
            });

            if (IsCanceled) return new SmartItem[] { };
            return downloadList.ToArray();
        }

        private async Task<SmartItem[]> _getServerItems(SmartItem dir, string destination, int parentID, int count)
        {
            List<SmartItem> downloadList = new List<SmartItem>();

            await Task.Run(async delegate
            {
                dir.Destination = destination;
                dir.ParentId = parentID;
                downloadList.Add(dir);

                TransferEvent.TotalFolders++;
                parentID = parentID + count + 1;
                SmartItem[] items = await getServerItemsAsync(dir.FullName + '/');

                if (items != null)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                        if (IsCanceled) break;
                        if (items[i].IsFile)
                        {
                            items[i].Destination = destination + dir.ItemName + '\\';
                            items[i].ParentId = parentID;
                            downloadList.Add(items[i]);

                            TransferEvent.TotalSize += items[i].Length;
                            TransferEvent.TotalFiles++;
                        }
                        else downloadList.AddRange(await _getServerItems(items[i], destination + dir.ItemName + '\\', parentID, downloadList.Count - 1));
                    }
                }
                items = null;
            });

            if (IsCanceled) return new SmartItem[] { };
            return downloadList.ToArray();
        }
    }
}