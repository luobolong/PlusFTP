using System;
using System.Text.RegularExpressions;
using Hani.Utilities;

namespace Hani.FTP
{
    internal static class FTPDataConnectionParser
    {
        internal static class EPSV
        {
            private static Regex reg;
            private static Match match;

            static EPSV()
            {
                _set();
            }

            private static void _set()
            {
                reg = new Regex(@"\|([0-9]+)\|\)", RegexOptions.Compiled);
            }

            internal static bool Parse(string responsed, ref int port)
            {
                if (responsed.NullEmpty()) return false;

                try
                {
                    match = reg.Match(responsed);
                    if (match.Success) port = match.Groups[1].Value.Int();
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }

                return (port > 0);
            }
        }

        internal static class PASV
        {
            private static Regex reg;
            private static Match match;
            private static string[] hostPort;

            static PASV()
            {
                _set();
            }

            private static void _set()
            {
                reg = new Regex(@"\(([\d,]+)\)");
            }

            internal static bool Parse(string responsed, ref string host, ref int port)
            {
                if (responsed.NullEmpty()) return false;

                try
                {
                    match = reg.Match(responsed);
                    if (match.Success)
                    {
                        hostPort = match.Groups[1].Value.Split(',');

                        host = hostPort[0] + '.' + hostPort[1] + '.' + hostPort[2] + '.' + hostPort[3];
                        port = (hostPort[4].Int() * 256) + hostPort[5].Int();
                    }
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }

                return ((port > 0) && (host.Length > 6));
            }
        }
    }
}