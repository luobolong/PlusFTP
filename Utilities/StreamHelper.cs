using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class SocketHelper
    {
        internal static IPAddress[] HostIPs;

        internal static async Task<Socket> ConnectAsync(string host, int port)
        {
            HostIPs = null;
            try { HostIPs = await Dns.GetHostAddressesAsync(host); }
            catch { return null; }

            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            bool success = false;

            try { await Task.Run(() => { success = socket.BeginConnect(HostIPs, port, null, null).AsyncWaitHandle.WaitOne(5000, true); }); }
            catch { socket.Close(); return null; }

            if (!success || !socket.Connected) { socket.Close(); return null; }

            return socket;
        }

        internal static bool IsIPV6()
        {
            for (int i = 0; i < HostIPs.Length; i++)
                if (HostIPs[i].AddressFamily == AddressFamily.InterNetworkV6)
                    return true;

            return false;
        }
    }
}