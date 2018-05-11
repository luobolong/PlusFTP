using System;

namespace Hani.Utilities
{
    internal static class ExceptionHelper
    {
        internal static void Log(Exception exp)
        {
            FileHelper.AppendText(DirectoryHelper.CurrentDirectory + @"\Exceptions.txt",
                DateFormatHelper.GetShortDateTime() + Environment.NewLine + formatException(exp) + Environment.NewLine);
        }

        internal static void Error(Exception exp)
        {
            string error = "System: " + PCINFO.GetOperatingSystem() + (Environment.Is64BitOperatingSystem ? " x64" : string.Empty) + Environment.NewLine;
            error += "PlusFTP: " + VersionHelper.AppVersion + Environment.NewLine;
            error = error + formatException(exp);

            FileHelper.WriteAll(DirectoryHelper.CurrentDirectory + @"\Error-" +
                DateFormatHelper.GetShortDateTimeSafe() + ".txt", error);
        }

        private static string formatException(Exception exp)
        {
            string error = "Message: " + exp.Message + Environment.NewLine;
            error += "InnerException: " + exp.InnerException + Environment.NewLine;
            return (error + exp.StackTrace);
        }
    }
}