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

        public static SerializableDictionary<string, int> SortServerLoad(SerializableDictionary<string, int> dic)
        {
            SerializableDictionary<string, int> res = new SerializableDictionary<string,int>();

            foreach (var item in dic.OrderBy(i => i.Value))
            {
                res.Add(item.Key, item.Value);
            }

            return res;
        }

        public static Dictionary<int, long> SortVotes(Dictionary<long, int> dic)
        {
            Dictionary<int, long> res = new Dictionary<int, long>();

            foreach (var item in dic.OrderBy(i => i.Value))
            {
                if (!res.ContainsKey(item.Value))
                {
                    res.Add(item.Value, item.Key);
                }
                else
                {
                    if (item.Key > res[item.Value])
                    {
                        res[item.Value] = item.Key;
                    }
                }
            }

            // Print resulting Dictionary
            Console.WriteLine("SORTED VOTES:");
            foreach (int i in res.Keys)
            {
                Console.WriteLine(i + " => " + res[i]);
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

        public static string[] SliceArray(string[] source, int start, int end)
        {
            if (end < 0)
            {
                end = source.Length + end;
            }

            int len = end - start;

            string[] res = new string[len];

            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }

            return res;
        }

        public static void SerializeObject(Object ob, string currentDir, string name)
        {
            try
            {
                string path = currentDir + @"\" + name;
                Console.WriteLine(path);
                TextWriter tw = new StreamWriter(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(ob.GetType());
                x.Serialize(tw, ob);
                Console.WriteLine("Object written to file");
                tw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException.Message);
            }
        }

        public static Object DeserializeObject(Object ob, string currentDir, string name)
        {
            string path = currentDir + @"\" + name;
            TextReader tr = new StreamReader(path);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(ob.GetType());
            Object obj = x.Deserialize(tr);
            tr.Close();
            return obj;
        }

        public static File DeserializeFile(string path, File file)
        {
            TextReader tr = new StreamReader(path);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
            File f = (File)x.Deserialize(tr);
            tr.Close();
            return f;
        }

        public static void SerializeFile(string path, File file)
        {
            TextWriter tw = new StreamWriter(path);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
            x.Serialize(tw, file);
            Console.WriteLine("Object written to file");
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

        //Calculates the average access to data servers 
        public static int AverageAccesses(Dictionary<string, DataInfo> dataServersInfo)
        {
            int count = 0;
            int total = 0;
            foreach (KeyValuePair<string, DataInfo> accesses in dataServersInfo)
            {
                count = count + 1;
                total = total + accesses.Value.GetTotalAccesses();
            }
            return total / count;
        }

        //calculates the value that alows make the interval
        public static int IntervalAccesses(double alfa, int average) 
        {
            return (int)(alfa * average);
        }

        public static string[] GetFileNamesFromDirectory(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                files[i] = Path.GetFileNameWithoutExtension(file);
            }

            return files;
        }
    }
}
