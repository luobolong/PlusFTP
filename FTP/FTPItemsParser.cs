using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hani.Utilities;

namespace Hani.FTP
{
    internal static class FTPItemsParser
    {
        internal static async Task<SmartItem[]> ParseAsync(this FTPClient client, string path, string rawItems)
        {
            return client.IsMLSD ? await MLSD.ParseAsync(client, path, rawItems) : await Lst.ParseAsync(client, path, rawItems);
        }

        private static class MLSD
        {
            private static Regex reg;
            private static string[] rawSplit;
            private static string[] lineSplit;

            static MLSD()
            {
                _set();
            }

            private static void _set()
            {
                reg = new Regex("(?<Key>[^=]+)=(?<Value>[^;]+);?", RegexOptions.Compiled);
                //reg2 = new Regex(@"(?:(?<Key>[^=;\r\n]+)=|(?<Key>\s))(?:(?<Value>[^;\r\n]+);?)\r?\n?", RegexOptions.Compiled);
                rawSplit = new string[] { "\n" }; //"\r\n",
                lineSplit = new string[] { "; " };
            }

            internal static async Task<SmartItem[]> ParseAsync(FTPClient client, string path, string rawItems)
            {
                if ((rawItems == null) || client.IsCanceled) return null;

                List<SmartItem> list = new List<SmartItem>(0);

                await Task.Run(() =>
                {
                    string[] lines = rawItems.Split(rawSplit, StringSplitOptions.None);
                    rawItems = null;

                    if (lines.Length != 0)
                    {
                        list.Capacity = lines.Length;
                        Match match;
                        SmartItem item;
                        DateTime date;
                        int x = -1;

                        for (int j = 0; j < lines.Length; j++)
                        {
                            if (client.IsCanceled) break;

                            string[] line = lines[j].Split(lineSplit, StringSplitOptions.None);

                            if (line.Length > 1)
                            {
                                if (line[1] == "." || line[1] == "..") continue;
                                item = new SmartItem(line[1], path);

                                try
                                {
                                    match = reg.Match(line[0]);
                                    while (match.Success)
                                    {
                                        switch (match.Groups["Key"].Value)
                                        {
                                            case "type":
                                                item.IsFile = (match.Groups["Value"].Value == "file");
                                                item.IsLink = (!item.IsFile && (match.Groups["Value"].Value == "OS.unix=slink:"));
                                                break;

                                            case "modify":
                                                if (match.Groups["Value"].Value.DateInvCulture("yyyyMMddHHmmss", out date))
                                                    item.Modified = date.ToLocalTime().Ticks;
                                                break;

                                            case "size":
                                                item.Length = match.Groups["Value"].Value.Long();
                                                break;

                                            case "UNIX.mode":
                                                item.Permissions = match.Groups["Value"].Value.Remove(0, 1);
                                                break;
                                        }
                                        match = match.NextMatch();
                                    }

                                    if (item.IsFile) list.Add(item);
                                    else list.Insert(++x, item);
                                }
                                catch (Exception exp) { ExceptionHelper.Log(exp); }
                            }
                        }

                        if (!client.IsUnix.HasValue && (list.Count > 0)) client.IsUnix = !list[0].Permissions.NullEmpty();
                    }
                    lines = null;
                });

                return client.IsCanceled ? null : list.ToArray();
            }
        }

        private static class Lst
        {
            private static Regex isWinRegex;
            private static Regex win;
            private static Regex unix;
            private static string[] dateFormatsWin;
            private static string[] dateFormatsUnix;

            static Lst()
            {
                _set();
            }

            private static void _set()
            {
                isWinRegex = new Regex("[0-9]{2}-[0-9]{2}-[0-9]{2}", RegexOptions.Compiled);
                win = new Regex(@"(?<Modified>[\d-]+\s{2}[\d:]+\w{2}?)\s+(?<Length>[\d]+|<DIR>)\s+(?<Name>[^\n]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
                unix = new Regex(@"^.(?<Permissions>[rwx\-]{9})\s+\d+ [\d\w]+\s+[\d\w]+\s+(?<Length>\d+)\s(?<Modified>\w+\s{1,2}\d+\s+[\d:]+)\s(?<Name>[^\n]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

                dateFormatsWin = new string[] { "MM-dd-yy  hh:mmtt" };
                dateFormatsUnix = new string[] { "MMM dd  yyyy", "MMM  d  yyyy", "MMM dd yyyy HH:mm", "MMM  d yyyy HH:mm" };
            }

            internal static async Task<SmartItem[]> ParseAsync(FTPClient client, string path, string rawItems)
            {
                if ((rawItems == null) || client.IsCanceled) return null;

                List<SmartItem> list = new List<SmartItem>();
                if (rawItems.Length > 8)
                {
                    await Task.Run(() =>
                    {
                        SmartItem item;
                        Match match;
                        DateTime date;
                        string[] dateFormats;
                        string nowYear = ' ' + DateTime.UtcNow.Year.StringInv();
                        string modified = string.Empty;

                        try
                        {
                            if (!client.IsUnix.HasValue) client.IsUnix = !isWinRegex.IsMatch(rawItems.Substring(0, 8));

                            if (client.IsUnix.Value)
                            {
                                match = unix.Match(rawItems);
                                dateFormats = dateFormatsUnix;
                            }
                            else
                            {
                                match = win.Match(rawItems);
                                dateFormats = dateFormatsWin;
                            }

                            rawItems = null;

                            int x = -1;
                            while (match.Success)
                            {
                                if (client.IsCanceled) break;

                                item = new SmartItem(match.Groups["Name"].Value, path);
                                modified = match.Groups["Modified"].Value;
                                if (client.IsUnix.Value)
                                {
                                    int t = modified.Index(":");
                                    if ((t > 0) && (t > 3)) modified = modified.Insert(6, nowYear);

                                    switch (match.Value[0])
                                    {
                                        case 'd':
                                            if ((match.Groups["Name"].Value.Length < 3) && ((match.Groups["Name"].Value == ".") || (match.Groups["Name"].Value == ".."))) { match = match.NextMatch(); continue; }
                                            break;

                                        case 'l':
                                            item = new SmartItem(match.Groups["Name"].Value.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim(), path);
                                            item.IsLink = true;
                                            break;

                                        default:
                                            item.IsFile = true;
                                            break;
                                    }

                                    item.Permissions = PermParser.ParseText(match.Groups["Permissions"].Value);
                                }
                                else item.IsFile = (match.Groups["Length"].Value != "<DIR>");

                                if (modified.DateInvCulture(dateFormats, out date))
                                    item.Modified = date.ToLocalTime().Ticks;

                                if (item.IsFile)
                                {
                                    item.Length = match.Groups["Length"].Value.Long();
                                    list.Add(item);
                                }
                                else list.Insert(++x, item);

                                match = match.NextMatch();
                            }
                        }
                        catch (Exception exp) { ExceptionHelper.Log(exp); }
                        match = null;
                    });
                }

                return client.IsCanceled ? null : list.ToArray();
            }
        }
    }
}