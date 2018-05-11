using System.Management;
using Microsoft.Win32;

namespace Hani.Utilities
{
    internal static class PCINFO
    {
        internal static string ProcessorId { get; private set; }

        static PCINFO()
        {
            _set();
        }

        private static void _set()
        {
            //ProcessorId = getProcessorId();
            ProcessorId = "56456456546546";
        }

        private static string getProcessorId()
        {
            string processorId = string.Empty;

            using (ManagementObjectSearcher MOS = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
            {
                foreach (ManagementObject oManagementObject in MOS.Get())
                {
                    processorId = oManagementObject["ProcessorId"].ToString();
                    break;
                }
            }

            return processorId;
        }

        internal static string GetOperatingSystem()
        {
            try { return Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false).GetValue("ProductName").ToString(); }
            catch { return string.Empty; }
        }
    }
}