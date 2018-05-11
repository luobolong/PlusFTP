using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class Users
    {
        internal static List<UserInfo> Items;
        internal static string FilePath;
        internal static string FileAppPath { get; private set; }
        internal static string FileRoamingPath { get; private set; }

        private static bool isLoaded;
        private static bool isSaving;

        private const string fileName = @"\users";

        static Users()
        {
            _set();
        }

        private static void _set()
        {
            FileRoamingPath = AppSettings.RoamingPath + fileName;
            FileAppPath = DirectoryHelper.CurrentDirectory + fileName;
            FilePath = FileRoamingPath;
            isLoaded = false;

            if (FileHelper.Exists(FileAppPath)) { FilePath = FileAppPath; }
        }

        internal static async Task Load()
        {
            if (isLoaded) return;

            await Task.Run(() =>
            {
                string[] lines = FileHelper.ReadAll(FilePath).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                Items = new List<UserInfo>(lines.Length);

                Regex regex = new Regex(@"(?<Variable>[^""]+)""(?<Value>[^""]*)""\s*", RegexOptions.Compiled);

                for (int i = 0; i < lines.Length; i++)
                {
                    UserInfo user = new UserInfo();
                    try
                    {
                        MatchCollection matches = regex.Matches(lines[i]);
                        for (int j = 0; j < matches.Count; j++)
                        {
                            switch (matches[j].Groups["Variable"].Value)
                            {
                                case "Host": user.Host = matches[j].Groups["Value"].Value; break;
                                case "UserName": user.UserName = matches[j].Groups["Value"].Value; break;
                                case "Password": user.Password = matches[j].Groups["Value"].Value; break;
                                case "Port": user.Port = matches[j].Groups["Value"].Value.Int(); break;
                                case "Encryption": user.Encryption = matches[j].Groups["Value"].Value.Int(); break;
                                case "Protocol": user.Protocol = matches[j].Groups["Value"].Value.Int(); break;
                                case "UTF8": user.UTF8 = matches[j].Groups["Value"].Value.Int(); break;
                                case "MODEZ": user.MODEZ = matches[j].Groups["Value"].Value.Int(); break;
                                case "Proxy": user.Proxy = matches[j].Groups["Value"].Value.Int(); break;
                                case "Cache": user.Cache = matches[j].Groups["Value"].Value.Int(); break;
                                case "Selected": user.Selected = matches[j].Groups["Value"].Value.True(); break;
                            }
                        }
                        Items.Add(user);
                    }
                    catch (Exception exp) { ExceptionHelper.Log(exp); }
                }
            });

            isLoaded = true;
        }

        internal static void AddUser(UserInfo userInfo)
        {
            int id = -1;
            if (Items.Count != 0)
            {
                Parallel.For(0, Items.Count, (i, loopState) =>
                {
                    Items[i].Selected = false;

                    if ((userInfo.Host == Items[i].Host) && (userInfo.UserName == Items[i].UserName))
                        id = i;
                });
            }

            if (id != -1) Items[id] = userInfo;
            else Items.Add(userInfo);
        }

        internal static async Task Save()
        {
            if (isSaving) while (isSaving) { await Task.Delay(100); }

            UserInfo[] items = new UserInfo[Items.Count];
            Items.CopyTo(items);

            isSaving = true;

            await Task.Run(() =>
            {
                StringBuilder content = new StringBuilder();

                for (int i = 0; i < items.Length; i++)
                    content.Append(((content.Length > 0) ? Environment.NewLine : string.Empty) + items[i].FullString());

                FileHelper.WriteAll(FilePath, content.ToString());
                content.Clear();
            });

            isSaving = false;
        }
    }
}