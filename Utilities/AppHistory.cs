using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Hani.Utilities
{
    internal static class AppHistory
    {
        internal static string FilePath;
        internal static string FileAppPath { get; private set; }
        internal static string FileRoamingPath { get; private set; }
        internal static bool IsEnabled { get; set; }

        private static ObservableCollection<HistoryItem> items;
        private static DispatcherTimer timer;
        private static List<HistoryItem> newItems;
        private static bool isSaving;
        private static bool isLoading;

        private const string fileName = @"\history";

        static AppHistory()
        {
            _set();
        }

        private static async void _set()
        {
            isLoading = true;

            items = new ObservableCollection<HistoryItem>();
            newItems = new List<HistoryItem>();

            IsEnabled = AppSettings.Get("App", "SaveHistory", true);
            FileAppPath = DirectoryHelper.CurrentDirectory + fileName;
            FileRoamingPath = AppSettings.RoamingPath + fileName;
            FilePath = FileRoamingPath;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(timer_Tick);

            if (FileHelper.Exists(FileAppPath)) { FilePath = FileAppPath; }

            await load();
            isLoading = false;
        }

        internal static async Task load()
        {
            if (!IsEnabled) return;

            await Task.Run(() =>
            {
                string[] lines = FileHelper.ReadAll(FilePath).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                HistoryItem[] _items = new HistoryItem[lines.Length];

                Regex regex = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);
                MatchCollection match = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    try
                    {
                        match = regex.Matches(lines[i]);
                        if (match.Count == 7)
                        {
                            DateTime dateTime = new DateTime(match[6].Groups[1].Value.Long());
                            _items[i] = new HistoryItem(match[0].Groups[1].Value, match[1].Groups[1].Value, match[2].Groups[1].Value, match[3].Groups[1].Value, match[4].Groups[1].Value, match[5].Groups[1].Value, dateTime);
                        }
                    }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }
                }

                items = new ObservableCollection<HistoryItem>(_items);
            });
        }

        internal static async Task<ObservableCollection<HistoryItem>> GetItems()
        {
            if (isLoading) { while (isLoading) { await Task.Delay(100); } }
            return items;
        }

        internal static async void Add(string name, string path, string oldValue, string newValue, ItemStatus status)
        {
            if (isSaving || isLoading) while (isSaving || isLoading) { await Task.Delay(100); }

            HistoryItem item = new HistoryItem(ClientHelper.Server, name, path, oldValue, newValue, status.ToString(), DateTime.Now);
            items.Insert(0, item);
            newItems.Add(item);

            Save();
        }

        internal static void Clear()
        {
            items.Clear();
        }

        internal static async Task Delete()
        {
            await Task.Run(async delegate
            {
                FileHelper.Delete(FilePath);
                bool exists = true;
                int t = 0;
                while (exists && (t < 10))
                {
                    await Task.Delay(1000);
                    exists = !FileHelper.Delete(FilePath);
                    t++;
                }
            });
        }

        private static async void timer_Tick(object sender, EventArgs e)
        {
            isSaving = true;
            timer.Stop();

            StringBuilder sb = new StringBuilder();
            string line = string.Empty;
            for (int i = 0; i < newItems.Count; i++)
            {
                if (!IsEnabled) return;
                line = "{" + newItems[i].Server + "}" +
                       "{" + newItems[i].ItemName + "}" +
                       "{" + newItems[i].ItemPath + "}" +
                       "{" + newItems[i].OldValue + "}" +
                       "{" + newItems[i].NewValue + "}" +
                       "{" + newItems[i].Status + "}" +
                       "{" + newItems[i].Ticks + "}";

                sb.Append(((sb.Length > 0) ? Environment.NewLine : string.Empty) + line);
            }
            newItems.Clear();

            await FileHelper.AppendTextAsync(FilePath, sb.ToString());
            sb.Clear();
            isSaving = false;
        }

        internal static void Save()
        {
            if (!IsEnabled) return;
            timer.Start();
        }
    }
}