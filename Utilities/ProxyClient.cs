using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal static class ProxyClient
    {
        internal static ProxyInfo Proxy { get; set; }

        internal enum ProxyProtocol : ushort
        {
            Http = 0,
            Socks4 = 1,
            Socks4a = 2,
            Socks5 = 3
        }

        internal class ProxyInfo
        {
            internal string Host { get; set; }
            internal int Port { get; set; }
            internal ProxyProtocol Protocol { get; set; }

            internal ProxyInfo(string server, string port, ProxyProtocol protocol)
            {
                Host = server;
                Port = port.Int();
                Protocol = protocol;
            }
        }

        internal static async Task<Socket> ConnectAsync(string host, int port)
        {
            switch (Proxy.Protocol)
            {
                case ProxyProtocol.Http:
                    return await ConnectHttpAsync(host, port);
                //case ProxyProtocol.Socks4:
                //return await Socks4(server, port);
                //case ProxyProtocol.Socks4a:
                //return await Socks4a(server, port);
                //case ProxyProtocol.Socks5:
                //  return await ConnectSocks5Async(host, port);
            }

            return null;
        }

        private static async Task<Socket> ConnectHttpAsync(string host, int port)
        {
            //TODO: need testing
            Socket socket = null;
            try
            {
                socket = await SocketHelper.ConnectAsync(host, port);
                if (socket == null) return null;

                await Task.Run(() => { socket.Send(Encoding.UTF8.GetBytes("CONNECT " + host + ":" + port + " HTTP/1.0\r\n\r\n")); });

                byte[] recvBuffer = new byte[39];
                string respond = string.Empty;
                int received, tbytes = 0;

                await Task.Run(() =>
                {
                    do
                    {
                        received = 0;
                        received = socket.Receive(recvBuffer);
                        if (received == 0) break;

                        tbytes += received;
                        respond += Encoding.ASCII.GetString(recvBuffer, 0, recvBuffer.Length);
                        //if (respond.Contains("\r\n\r\n")) break;
                    }
                    while ((tbytes > 0) && (tbytes < 39));
                });
                if (respond.Contains("200")) return socket;
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            if (socket != null) socket.Close();

            return null;
        }
    }
}