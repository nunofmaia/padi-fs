using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using System.Collections;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

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
            if (s != null)
            {
                return System.Text.Encoding.UTF8.GetString(s);
            }

            return "";
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

        public static string[] SliceArray(string[] args, int index)
        {
            List<string> result = new List<string>();
            result.Capacity = args.Length - index;

            for (int i = index; i < args.Length; index++)
            {
                string arg = args[i];
                result.Add(arg);
            }

            return result.ToArray();
        }

        public static void SerializeObject(Object md, string currentDir, string name)
        {
            var serializer = new DataContractSerializer(md.GetType());


            string path = currentDir + @"\" + name;
            Console.WriteLine(path);
            using (var sw = new StreamWriter(path))
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented; // indent the Xml so it's human readable
                    serializer.WriteObject(writer, md);
                    writer.Flush();
                    //xmlString = sw.ToString();
                    //Console.WriteLine(xmlString);
                    sw.Close();
                }
            }
        }
        public static Object DeserializeObject(Object ob, string currentDir, string name)
        {
            var serializer = new DataContractSerializer(ob.GetType());
            string path = currentDir + @"\" + name;
            using (var tw = new StreamReader(path))
            {
                using (var reader = new XmlTextReader(tw))
                {
                    return serializer.ReadObject(reader);
                }
            }
        }

        public static File DeserializeFile(string path, File file)
        {
             TextReader tr = new StreamReader(path);
             System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
             tr.Close();
             return (File)x.Deserialize(tr); 
        }

        public static void SerializeFile(string path, File file)
        {
            TextWriter tw = new StreamWriter(path);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
            x.Serialize(tw, file);
            Console.WriteLine("object written to file");
            tw.Close();
        }

        public static int GetPortOnAddress(string address)
        {
            int port = -1;
            Match match = Regex.Match(address, @"localhost:([0-9]+)/", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string key = match.Groups[1].Value;
                port = int.Parse(key);
            }

            return port;
        }
    }
}
