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
            return arguments.Split(new char[] {(char)0x7f});
        }

        public static byte[] ConvertStringToByteArray(string s)
        {
            return System.Text.Encoding.UTF8.GetBytes(s);
        }

        public static string ConvertByteArrayToString(byte[] s)
        {
            return System.Text.Encoding.UTF8.GetString(s);
        }

        public static Dictionary<string, int> SortServerLoad(Dictionary<string, int> dic)
        {
            Dictionary<string, int> res = new Dictionary<string,int>();

            foreach (var item in dic.OrderBy(i => i.Value))
            {
                res.Add(item.Key, item.Value);
            }

            return res;
        }

        public static Dictionary<DateTime, int> SortVotes(Dictionary<DateTime, int> dic)
        {
            Dictionary<DateTime, int> res = new Dictionary<DateTime, int>();

            foreach (var item in dic.OrderBy(i => i.Value))
            {
                res.Add(item.Key, item.Value);
                Console.WriteLine(item);
            }

            return res;
        }

        public static int MetadataServerId(string name)
        {
            return int.Parse(name.Substring(2, name.Length - 2));
        }

        public static string MakeStringFromArray(string[] arr, int index)
        {
            string result = "";

            for (int i = index; i < arr.Length; i++)
            {
                result += arr[i] + " ";
            }

            return result;
        }
            
    }
}
