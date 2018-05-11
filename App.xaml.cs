using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using Hani.Utilities;
using Microsoft.Win32;

namespace PlusFTP
{
    public partial class App : Application
    {
    }

    internal static class Program
    {
        [STAThreadAttribute]
        internal static void Main()
        {
            if (!isNetv45()) return;

            //#if (!DEBUG)
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnResolveAssembly);
            //#endif

            try { App.Main(); }
            catch (Exception exp) { ExceptionHelper.Error(exp); }
        }

        private static bool isNetv45()
        {
            bool notv45 = true;

            try { notv45 = (new Version("4.5.0.0") > new Version(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client").GetValue("Version").ToString())); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            if (notv45 && MessageBox.Show("This application requires .NET Framework v4.5" + Environment.NewLine + Environment.NewLine + "Do you want to download it now?", "PlusFTP could not be start",
                                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.None) == MessageBoxResult.Yes)
            {
                try { System.Diagnostics.Process.Start(@"http://go.microsoft.com/fwlink/p/?LinkId=245484"); }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return !notv45;
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);

                if (!assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture)) return null;

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName.Name + ".dll"))
                {
                    if (stream == null) return null;

                    byte[] assemblyRawBytes = new byte[stream.Length];
                    stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                    return Assembly.Load(assemblyRawBytes);
                }
            }
            catch (Exception exp) { ExceptionHelper.Error(exp); }

            return null;
        }
    }
}