using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Hani.Utilities
{
    internal static class FileOperationAPIWrapper
    {
        /// <summary>
        /// Possible flags for the SHFileOperation method.
        /// </summary>
        [Flags]
        private enum OperationFlags : ushort
        {
            /// <summary>
            /// Do not show a dialog during the process
            /// </summary>
            FOF_SILENT = 0x0004,
            /// <summary>
            /// Do not ask the user to confirm selection
            /// </summary>
            FOF_NOCONFIRMATION = 0x0010,
            /// <summary>
            /// Delete the file to the recycle bin.  (Required flag to send a file to the bin
            /// </summary>
            FOF_ALLOWUNDO = 0x0040,
            /// <summary>
            /// Do not show the names of the files or folders that are being recycled.
            /// </summary>
            FOF_SIMPLEPROGRESS = 0x0100,
            /// <summary>
            /// Surpress errors, if any occur during the process.
            /// </summary>
            FOF_NOERRORUI = 0x0400,
            /// <summary>
            /// Warn if files are too big to fit in the recycle bin and will need
            /// to be deleted completely.
            /// </summary>
            FOF_WANTNUKEWARNING = 0x4000,
        }

        /// <summary>
        /// File Operation Function Type for SHFileOperation
        /// </summary>
        private enum OperationType : uint
        {
            /// <summary>
            /// Move the objects
            /// </summary>
            FO_MOVE = 0x0001,
            /// <summary>
            /// Copy the objects
            /// </summary>
            FO_COPY = 0x0002,
            /// <summary>
            /// Delete (or recycle) the objects
            /// </summary>
            FO_DELETE = 0x0003,
            /// <summary>
            /// Rename the object(s)
            /// </summary>
            FO_RENAME = 0x0004,
        }

        /// <summary>
        /// SHFILEOPSTRUCT for SHFileOperation from COM
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public OperationType wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public OperationFlags fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

        [SuppressUnmanagedCodeSecurityAttribute]
        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            internal static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);
        }

        /// <summary>
        /// Send file to recycle bin
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        internal static bool SendToRecycleBin(string path)
        {
            try
            {
                SHFILEOPSTRUCT fs = new SHFILEOPSTRUCT
                {
                    wFunc = OperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = OperationFlags.FOF_ALLOWUNDO | OperationFlags.FOF_SIMPLEPROGRESS | OperationFlags.FOF_NOCONFIRMATION | OperationFlags.FOF_WANTNUKEWARNING
                };
                int r = NativeMethods.SHFileOperation(ref fs);
                return (r == 0);
            }
            catch (Exception) { return false; }
        }

        internal static bool Move(string from, string to)
        {
            try
            {
                SHFILEOPSTRUCT fs = new SHFILEOPSTRUCT
                {
                    wFunc = OperationType.FO_MOVE,
                    pFrom = from + '\0' + '\0',
                    pTo = to + '\0' + '\0',
                    fFlags = OperationFlags.FOF_ALLOWUNDO | OperationFlags.FOF_SIMPLEPROGRESS | OperationFlags.FOF_NOCONFIRMATION | OperationFlags.FOF_WANTNUKEWARNING
                };
                int r = NativeMethods.SHFileOperation(ref fs);
                return (r == 0);
            }
            catch (Exception) { return false; }
        }

        internal static bool Rename(string from, string to)
        {
            try
            {
                SHFILEOPSTRUCT fs = new SHFILEOPSTRUCT
                {
                    wFunc = OperationType.FO_RENAME,
                    pFrom = from + '\0' + '\0',
                    pTo = to + '\0' + '\0',
                    fFlags = OperationFlags.FOF_ALLOWUNDO | OperationFlags.FOF_SIMPLEPROGRESS | OperationFlags.FOF_NOCONFIRMATION | OperationFlags.FOF_WANTNUKEWARNING
                };
                int r = NativeMethods.SHFileOperation(ref fs);
                return (r == 0);
            }
            catch (Exception) { return false; }
        }
    }
}