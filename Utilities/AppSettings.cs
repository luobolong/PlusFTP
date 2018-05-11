using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Hani.Utilities
{
    internal static class AppSettings
    {
        internal static string FilePath;
        internal static string RoamingPath { get; private set; }
        internal static string FileRoamingPath { get; private set; }
        internal static string FileAppPath { get; private set; }
        internal static bool SameAppPath { get; private set; }

        private static Dictionary<string, Setting> settings;

        private const string fileName = @"\settings";

        static AppSettings()
        {
            _set();
        }

        private static void _set()
        {
            settings = new Dictionary<string, Setting>(10);
            RoamingPath = DirectoryHelper.ApplicationData + @"\PlusFTP";
            FileRoamingPath = RoamingPath + fileName;
            FileAppPath = DirectoryHelper.CurrentDirectory + fileName;
            FilePath = FileRoamingPath;

            if (FileHelper.Exists(FileAppPath)) { SameAppPath = true; FilePath = FileAppPath; loadConf(FileAppPath); }
            else if (FileHelper.Exists(FileRoamingPath)) loadConf(FileRoamingPath);
            else DirectoryHelper.Create(RoamingPath);
        }

        internal static void Set(string key, string varName, object value)
        {
            if (settings.ContainsKey(key)) settings[key][varName] = value.ToString();
            else settings.Add(key, new Setting(varName, value.ToString()));
        }

        internal static bool Get(string key, string varName, bool value = false)
        {
            return _get(key, varName, value.ToString()).True(value);
        }

        internal static string Get(string key, string varName, string value = null)
        {
            value = _get(key, varName, value);
            if (value == null) return string.Empty;
            return value;
        }

        internal static int Get(string key, string varName, int value = 0)
        {
            return _get(key, varName, value.String()).Int(value);
        }

        internal static double Get(string key, string varName, double value = 0.0)
        {
            return _get(key, varName, value.String()).Double(value);
        }

        internal static void SaveWidthHeight(this Window window, string key)
        {
            if (window.WindowState != WindowState.Normal) return;
            Set(key, "Height", window.ActualHeight);
            Set(key, "Width", window.ActualWidth);
        }

        internal static void SetWidthHeight(this Window window, string key, double width, double height)
        {
            window.Width = Get(key, "Width", width);
            window.Height = Get(key, "Height", height);
        }

        internal static void SaveTopLeft(this Window window, string key)
        {
            if (window.WindowState != WindowState.Normal) return;
            Set(key, "Top", window.Top);
            Set(key, "Left", window.Left);
        }

        internal static void SetTopLeft(this Window window, string key)
        {
            double t = Get(key, "Top", 0.0);
            double l = Get(key, "Left", 0.0);

            if ((t > 0) && (l > 0))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Top = t;
                window.Left = l;
            }
        }

        internal static void SaveGridView(string key, object view)
        {
            if ((view == null) || key.NullEmpty()) return;

            GridViewColumnCollection columns = (view as GridView).Columns;
            for (int i = 0; i < columns.Count; i++) if (columns[i].ActualWidth != 0)
                    Set(key, (columns[i].Header as GridViewColumnHeader).Tag.ToString(), columns[i].ActualWidth);
        }

        internal static void SetGridView(string key, object view)
        {
            if ((view == null) || key.NullEmpty()) return;

            GridViewColumnCollection columns = (view as GridView).Columns;
            for (int i = 0; i < columns.Count; i++)
            {
                string scname = (columns[i].Header as GridViewColumnHeader).Tag.ToString();
                columns[i].Width = Get(key, scname, columns[i].Width);
            }
        }

        internal static async Task Save()
        {
            await Task.Run(() =>
            {
                StringBuilder content = new StringBuilder();
                StringBuilder subContent = new StringBuilder();

                foreach (string key in settings.Keys)
                {
                    subContent.Clear();
                    foreach (string subKey in settings[key].Keys)
                        subContent.Append(((subContent.Length > 0) ? ", " : string.Empty) + parse(subKey, settings[key][subKey]));

                    content.Append(((content.Length > 0) ? Environment.NewLine : string.Empty) + parse(key, "{" + subContent.ToString() + "}"));
                }

                FileHelper.WriteAll(FilePath, content.ToString());
                subContent.Clear();
                content.Clear();
            });
        }

        private static void loadConf(string path)
        {
            Match match = (new Regex(@"(?<Key>[^:\s]+)\s*:\s*\{\s*?(?<Array>[^}]+)\s*\}", RegexOptions.Compiled)).Match(FileHelper.ReadAll(path));
            while (match.Success)
            {
                settings.Add(match.Groups["Key"].Value, new Setting(match.Groups["Array"].Value));
                match = match.NextMatch();
            }
        }

        private static string _get(string key, string varName, string value = null)
        {
            if (settings.ContainsKey(key))
            {
                string keyValue = settings[key][varName];
                if (keyValue != null) value = keyValue;
            }

            return value;
        }

        private static string parse(string key, string value)
        {
            return key + ":" + value;
        }

        private sealed class Setting
        {
            private static Regex regex = new Regex(@"(?<Variable>[^\s,:]+)\s*:\s*(?<Value>[^\s,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            private Dictionary<string, string> _settings = new Dictionary<string, string>(10);

            internal Dictionary<string, string>.KeyCollection Keys { get { return _settings.Keys; } }

            internal Setting(string value)
            {
                Match match = regex.Match(value);
                while (match.Success)
                {
                    _settings.Add(match.Groups["Variable"].Value, match.Groups["Value"].Value);
                    match = match.NextMatch();
                }
            }

            internal Setting(string key, string value)
            {
                _settings.Add(key, value.ToString());
            }

            internal string this[string key]
            {
                get
                {
                    if (_settings.ContainsKey(key))
                        return _settings[key];
                    else return null;
                }
                set
                {
                    if (_settings.ContainsKey(key))
                        _settings[key] = value;
                    else _settings.Add(key, value);
                }
            }
        }
    }
}