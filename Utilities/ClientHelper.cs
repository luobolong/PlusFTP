using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Hani.FTP;
using PlusFTP.Windows;

namespace Hani.Utilities
{
    internal static partial class ClientHelper
    {
        private static List<NetworkClient> Clients;
        private static NetworkClient mainClient;
        private static NetworkClient secondaryClient;
        private static Regex upRegex;
        private static Stopwatch stopwatch;
        private static DispatcherTimer timer;

        internal static Window Owner { get; set; }
        internal static NetworkClient Client { get { return mainClient; } }
        internal static SmartCount Counts { get; private set; }
        internal static SmartCollection Items { get { return NetworkClient.Items; } }
        internal static string Server { get { return NetworkClient.Host; } }
        internal static string Home { get { return NetworkClient.HomePath; } }
        internal static string CurrentPath { get; private set; }
        internal static string LastPath { get; private set; }
        internal static bool MultiConnection { get; set; }
        internal static bool IsConnected { get { return ((mainClient != null) && !mainClient.IsDisposed && mainClient.IsConnected); } }

        static ClientHelper()
        {
            _set();
        }

        private static void _set()
        {
            SslClient.OnValidateCertificate += new SslClient.ValidateCertificateHandler(OnValidateCertificate);
            TransferEvents.OnItemStatusChanged += new TransferEvents.ItemStatusChangedHandler(OnItemStatusChanged);

            MultiConnection = AppSettings.Get("App", "MultiConnection", true);
            Clients = new List<NetworkClient>();
            Counts = new SmartCount();
            stopwatch = new Stopwatch();
            upRegex = new Regex("(.*/).*/", RegexOptions.Compiled);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(timer_Tick);
        }

        internal static async Task<bool> ConnectAsync()
        {
            mainClient = new FTPClient();
            mainClient.OnConnecting += new NetworkClient.StatusHandler(Connecting);
            mainClient.OnConnected += new NetworkClient.StatusHandler(Connected);
            mainClient.OnDisconnected += new NetworkClient.StatusHandler(Disconnected);

            if (await mainClient.ConnectAsync())
            {
                CurrentPath = Home;
                timer.Start();
                return true;
            }

            return false;
        }

        private static void OnValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //TODO:ValidateCertificate

            //Dispatcher.Invoke((Action)(() => { new VerifyCertificateWindow(this).ShowDialog();}));
        }

        private static void OnItemStatusChanged(SmartItem item)
        {
            string status = item.Status.ToString();

            if (status.Ends("Error")) item.OptColor = SolidColors.DarkRed;
            else if (status.Equal("Ignored")) item.OptColor = SolidColors.DarkOrange;
            else if (status.Ends("ed")) item.OptColor = SolidColors.SolidBlue;
            else item.OptColor = SolidColors.DarkGreen;
            item.Operation = AppLanguage.Get("LangOperation" + status);

            switch (item.Status)
            {
                case ItemStatus.Uploading: Counts.Files++; Counts.Update(); break;
                case ItemStatus.Created: Counts.Folders++; Counts.Update(); break;
            }
        }

        internal static string Up()
        {
            if (!CurrentPath.NullEmpty())
            {
                Match upMatch = upRegex.Match(CurrentPath);
                if (upMatch.Success) return upMatch.Groups[1].Value;
            }

            return string.Empty;
        }

        internal static async void SetSecondaryClient()
        {
            if (!MultiConnection) return;

            secondaryClient = await mainClient.NewInstance();
            if (secondaryClient != null) Clients.Add(secondaryClient);
        }

        private static async Task<NetworkClient> newInstance()
        {
            if (!MultiConnection) return null;

            NetworkClient client = null;
            if (secondaryClient != null)
            {
                client = secondaryClient;
                SetSecondaryClient();
                return client;
            }

            client = await mainClient.NewInstance();
            if (client != null)
            {
                Clients.Add(client);
                SetSecondaryClient();
                return client;
            }

            return null;
        }

        internal static void ClearCachedPath(string path)
        {
            NetworkClient.ClearCachedPath(path);
        }

        internal static void ClearCached()
        {
            NetworkClient.ClearCached();
        }

        private static void ClearItems()
        {
            NetworkClient.Items.Clear();
        }

        internal static async Task DisconnectAsync(bool clear = false)
        {
            if (clear) { ClearItems(); ClearCached(); } // Clear Before Disconnect Or it will be 0

            Parallel.For(0, Clients.Count, j => { if (!Clients[j].IsDisposed) Clients[j].Disconnect(false, true); });
            if (mainClient != null) await mainClient.DisconnectAsync(clear);

            if (clear) Clients.Clear();
        }

        internal static bool NameExists(string name)
        {
            if (Items.Count == 0) return false;

            bool exists = false;
            Parallel.For(0, Items.Count, (i, loopState) =>
            {
                if (NetworkClient.Items[i].ItemName == name)
                {
                    exists = true;
                    loopState.Stop();
                }
            });

            return exists;
        }

        internal static async Task<bool> SetItemsAsync(string path)
        {
            if (!IsConnected) return false;

            stopwatch.Reset();
            stopwatch.Start();
            LastPath = CurrentPath;
            Counts.Items = string.Empty;

            if (await mainClient.SetItemsAsync(path))
            {
                CurrentPath = NetworkClient.BrowsedPath;
                Counts.Files = Items.Files;
                Counts.Folders = Items.Folders;
                Counts.Update();

                stopwatch.Stop();
                string seconds = "({0:#0.0#}s)".FormatC((double)stopwatch.ElapsedMilliseconds / 1000);

                if (CurrentPath == "/") AppMessage.Add("/ Listed Successfully " + seconds, MessageType.Info);
                else
                {
                    try { AppMessage.Add(new Regex("/(?<DIR>[^/]+)/?$", RegexOptions.Compiled).Match(CurrentPath).Groups["DIR"].Value + " Listed Successfully " + seconds, MessageType.Info); }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }
                }

                return true;
            }
            else AppMessage.Add("Unable To List Server items.", MessageType.Error);

            stopwatch.Stop();
            return false;
        }

        internal static SmartItem GetItem(string name)
        {
            return FTPClient.GetServerItem(name, CurrentPath);
        }

        internal static async Task<bool> NewFolder(string name, string path)
        {
            Lock();
            bool created = await mainClient.CreateFolderAsync(name, path);
            UnLock();
            return created;
        }

        internal static async void RenameAsync(SmartItem item, string name)
        {
            Lock();
            await mainClient.RenameItemAsync(item, name);
            UnLock();
        }

        internal static async void DeleteAsync(SmartItem[] items)
        {
            NetworkClient client = null;
            bool isNewClient = false;

            if ((items.Length == 1) && items[0].IsFile) client = mainClient;
            else
            {
                client = await newInstance();
                isNewClient = (client != null);
            }

            if (!isNewClient)
            {
                client = mainClient;
                Lock();
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (!IsConnected) return;
                if (await client.DeleteItemAsync(items[i]))
                {
                    if (items[i].IsFile) Counts.Files--;
                    else Counts.Folders--;
                    Counts.Update();
                }
            }

            if (isNewClient) await client.DisconnectAsync(false, true);
            else UnLock();

            items = null;
        }

        internal static async void MoveAsync(SmartItem[] items, string toPath)
        {
            Lock();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IsConnected) return;
                if (await mainClient.MoveItemAsync(items[i], toPath))
                {
                    if (items[i].IsFile) Counts.Files--;
                    else Counts.Folders--;
                    Counts.Update();
                }
            }
            items = null;
            UnLock();
        }

        internal static async Task ChangePermAsync(SmartItem[] items, string permission)
        {
            Lock();
            for (int i = 0; i < items.Length; i++) await mainClient.ChangePermissionsync(items[i], permission);
            UnLock();
        }

        internal static async void TransferItemsAsync(object items, string destination, bool isUpload)
        {
            NetworkClient client = await newInstance();
            bool isNewClient = (client != null);

            if (!isNewClient) { client = mainClient; Lock(); }

            TransferWindow.Initialize(Owner, client);

            await client.TransferItemsAsync(items, destination, isUpload);

            if (isNewClient) client.Dispose();
            else { UnLock(); }

            items = null;
        }

        private static void timer_Tick(object sender, EventArgs e)
        {
            if (!IsConnected) { timer.Stop(); return; }

            mainClient.ConnectTicker++;
            Counts.Time = Counts.Time.Value.AddSeconds(1);
        }
    }
}