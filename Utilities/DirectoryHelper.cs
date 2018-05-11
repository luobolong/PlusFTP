using System;
using System.Collections.Generic;
using System.IO;

namespace Hani.Utilities
{
    internal static class DirectoryHelper
    {
        internal static string ApplicationData { get; private set; }
        internal static string CurrentDirectory { get; private set; }
        internal static string DesktopDirectory { get; private set; }
        internal static string Temp { get; private set; }

        static DirectoryHelper()
        {
            _set();
        }

        private static void _set()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
            DesktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Temp = Path.GetTempPath();
        }

        internal static bool Create(string path)
        {
            if (Exists(path)) return true;
            else
            {
                try { Directory.CreateDirectory(path); return true; }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return false;
        }

        internal static bool Delete(string path) //, bool recursive
        {
            return (!Exists(path) || FileOperationAPIWrapper.SendToRecycleBin(path));
            /*if (Exists(path))
            {
                try { Directory.Delete(path, recursive); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }*/
        }

        internal static bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        internal static DriveInfo[] GetDrives()
        {
            DriveInfo[] drives = new DriveInfo[] { };

            try { drives = DriveInfo.GetDrives(); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            if (drives.Length == 0) return drives;

            List<DriveInfo> drivesList = new List<DriveInfo>(drives.Length);
            for (int i = 0; i < drives.Length; i++)
                if (DirectoryHelper.Exists(drives[i].Name))
                    drivesList.Add(drives[i]);

            return drivesList.ToArray();
        }

        internal static string GetParentPath(string path)
        {
            if (Exists(path))
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    if ((dir != null) && (dir.Parent != null))
                        return dir.Parent.FullName;
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return null;
        }

        internal static string GetPath(string path)
        {
            string name = null;

            try { name = Path.GetDirectoryName(path); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return name;
        }

        internal static bool Move(string from, string to)
        {
            if (Exists(from) && !Exists(to))
            {
                try { Directory.Move(from, to); }
                catch (Exception exp) { ExceptionHelper.Log(exp); return false; }

                return true;
            }

            return false;
        }

        internal static bool Rename(string from, string to)
        {
            if (Exists(from) && !Exists(to))
            {
                /*try { Directory.Move(from, to); }
                catch (Exception exp) { ExceptionHelper.Log(exp); return false; }

                return true;*/

                return FileOperationAPIWrapper.Rename(from, to);
            }

            return false;
        }
    }
}