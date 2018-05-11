using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class FileHelper
    {
        private static Regex extReg;

        static FileHelper()
        {
            _set();
        }

        private static void _set()
        {
            extReg = new Regex(@".\.([^.]+)$", RegexOptions.Compiled);
        }

        internal static async void AppendText(string path, string text)
        {
            await AppendTextAsync(path, text);
        }

        internal static async Task AppendTextAsync(string path, string text)
        {
            if ((text == null) || (text.Length == 0)) return;

            try
            {
                using (StreamWriter sw = File.AppendText(path))
                    await sw.WriteLineAsync(text);
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
        }

        internal static FileStream Create(string path)
        {
            FileStream fs = null;

            try { fs = File.Create(path); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return fs;
        }

        internal static bool Delete(string path, bool recycle = false)
        {
            if (recycle) return (!Exists(path) || FileOperationAPIWrapper.SendToRecycleBin(path));
            else
            {
                if (Exists(path))
                {
                    try { File.Delete(path); }
                    catch { return false; }
                }

                return true;
            }
        }

        internal static bool Exists(string path)
        {
            return File.Exists(path);
        }

        internal static string GetExtension(string fileName)
        {
            if (!fileName.NullEmpty())
            {
                Match extMatch = extReg.Match(fileName);
                if (extMatch.Success) return extMatch.Groups[1].Value.Lower();
            }

            return string.Empty;
        }

        internal static bool Move(string from, string to)
        {
            if (Exists(from) && !Exists(to))
            {
                /*try { File.Move(from, to); }
                  catch (Exception exp) { ExceptionHelper.Log(exp); return false; }

                  return true;*/

                return FileOperationAPIWrapper.Move(from, to);
            }

            return false;
        }

        internal static bool Rename(string from, string to)
        {
            if (Exists(from) && !Exists(to))
            {
                /*try { File.Move(from, to); }
                  catch (Exception exp) { ExceptionHelper.Log(exp); return false; }

                  return true;*/

                return FileOperationAPIWrapper.Rename(from, to);
            }

            return false;
        }

        internal static FileStream OpenRead(string path)
        {
            FileStream fs = null;

            try { fs = File.OpenRead(path); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return fs;
        }

        internal static FileStream OpenWrite(string path)
        {
            FileStream fs = null;

            try { fs = File.OpenWrite(path); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return fs;
        }

        internal static async Task<string> ReadAllAsync(string path)
        {
            string text = string.Empty;

            if (Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
                        text = await sr.ReadToEndAsync();
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return text;
        }

        internal static string ReadAll(string path)
        {
            string text = string.Empty;

            if (Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
                        text = sr.ReadToEnd();
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return text;
        }

        internal static void WriteAll(string path, string text)
        {
            if (text != null)
            {
                try { File.WriteAllText(path, text, Encoding.UTF8); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }
        }

        internal static IEnumerable<string> ReadLines(string path)
        {
            IEnumerable<string> text = new List<string>(0);

            if (Exists(path))
            {
                try { text = File.ReadLines(path, Encoding.UTF8); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return text;
        }

        internal static bool Seek(FileStream fs, long tbytes)
        {
            if (fs == null) return false;

            try { fs.Seek(tbytes, SeekOrigin.Begin); }
            catch (Exception exp) { ExceptionHelper.Log(exp); return false; }

            return true;
        }
    }
}