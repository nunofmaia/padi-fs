using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace padiFS
{
    public class Util
    {

        public static int FreeTcpPort()
        {
            while (true)
            {
                TcpListener l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                int port = ((IPEndPoint)l.LocalEndpoint).Port;
                Console.WriteLine(port);
                l.Stop();
                if (!IsBusy(port))
                {
                    return port;
                }
            }
        }
        private static bool IsBusy(int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket,
                    SocketOptionName.ExclusiveAddressUse, true);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Listen(5);
                return false;
            }
            catch { return true; }
            finally { if (socket != null) socket.Close(); }
        }

        public static string[] SplitArguments(string arguments)
        {
            return arguments.Split(new char[] { '|' });
        }

        public static byte[] ConvertStringToByteArray(string s)
        {
            return System.Text.Encoding.UTF8.GetBytes(s);
        }

        public static string ConvertByteArrayToString(byte[] s)
        {
            return System.Text.Encoding.UTF8.GetString(s);
        }
    }
}
