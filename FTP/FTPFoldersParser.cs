using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hani.Utilities;

namespace Hani.FTP
{
    internal static class FTPFoldersParser
    {
        internal static async Task<SmartItem[]> ParseFoldersAsync(this FTPClient client, string path, string rawItems)
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
                rawSplit = new string[] { "\r\n", "\n" };
                lineSplit = new string[] { "; " };
            }

            internal static async Task<SmartItem[]> ParseAsync(FTPClient client, string path, string rawItems)
            {
                if ((rawItems == null) || client.IsCanceled) return null;
                List<SmartItem> folders = new List<SmartItem>(0);

                await Task.Run(() =>
                {
                    string[] lines = rawItems.Split(rawSplit, StringSplitOptions.RemoveEmptyEntries);
                    rawItems = null;

                    if (lines.Length != 0)
                    {
                        folders.Capacity = lines.Length;
                        Match match;
                        SmartItem item;

                        for (int j = 0; j < lines.Length; j++)
                        {
                            if (client.IsCanceled) break;

                            string[] line = lines[j].Split(lineSplit, StringSplitOptions.None);

                            if (line.Length > 1)
                            {
                                if (line[1] == "." || line[1] == "..") continue;

                                try
                                {
                                    match = reg.Match(line[0]);
                                    while (match.Success)
                                    {
                                        if ((match.Groups["Key"].Value == "type") && (match.Groups["Value"].Value != "file"))
                                        {
                                            item = new SmartItem(line[1], path);
                                            item.IsLink = (match.Groups["Value"].Value == "OS.unix=slink:");
                                            folders.Add(item);
                                        }
                                        match = match.NextMatch();
                                    }
                                }
                                catch (Exception exp) { ExceptionHelper.Log(exp); }
                            }
                        }
                    }
                    lines = null;
                });

                return client.IsCanceled ? null : folders.ToArray();
            }
        }

        private static class Lst
        {
            private static Regex isWinRegex;
            private static Regex win;
            private static Regex unix;

            static Lst()
            {
                _set();
            }

            private static void _set()
            {
                isWinRegex = new Regex("[0-9]{2}-[0-9]{2}-[0-9]{2}", RegexOptions.Compiled);
                win = new Regex(@"(<DIR>)\s+(?<Name>[^\n]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
                unix = new Regex(@"^[ld].+[\d:]+ (?<Name>[^\n]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            }

            internal static async Task<SmartItem[]> ParseAsync(FTPClient client, string path, string rawItems)
            {
                if (rawItems == null) return null;
                List<SmartItem> folders = new List<SmartItem>();

                await Task.Run(() =>
                {
                    if (rawItems.Length > 8)
                    {
                        Match match;
                        SmartItem item;

                        try
                        {
                            if (!client.IsUnix.HasValue) client.IsUnix = !isWinRegex.IsMatch(rawItems.Substring(0, 8));
                            match = client.IsUnix.Value ? unix.Match(rawItems) : win.Match(rawItems);
                            rawItems = null;

                            while (match.Success)
                            {
                                if (client.IsCanceled) break;

                                if (client.IsUnix.Value && (match.Value[0] == 'l'))
                                {
                                    item = new SmartItem(match.Groups["Name"].Value.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim(), path);
                                    item.IsLink = true;
                                }
                                else
                                {
                                    if ((match.Groups["Name"].Value.Length < 3) && ((match.Groups["Name"].Value == ".") || (match.Groups["Name"].Value == ".."))) { match = match.NextMatch(); continue; }
                                    item = new SmartItem(match.Groups["Name"].Value, path);
                                }

                                folders.Add(item);
                                match = match.NextMatch();
                            }
                            match = null;
                        }
                        catch (Exception exp) { ExceptionHelper.Log(exp); }
                        match = null;
                    }
                });

                return client.IsCanceled ? null : folders.ToArray();
            }
        }
    }
}