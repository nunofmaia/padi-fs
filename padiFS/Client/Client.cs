using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;

namespace padiFS
{
    public class Client : MarshalByRefObject, IClient
    {
        private string name;
        private int port;
        private Bridge bridge;
        private Dictionary<string, Metadata> allFiles;
        private Dictionary<string, Metadata> openFiles;

        public Client(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);
            this.bridge = new Bridge();
            this.allFiles = new Dictionary<string, Metadata>();
            this.openFiles = new Dictionary<string, Metadata>();
        }

        public void Create(string filename, int nServers, int rQuorum, int wQuorum)
        {
            Metadata meta = bridge.Create(filename, nServers, rQuorum, wQuorum);

            if (meta != null)
            {
                Console.WriteLine("vou adicionar isto");
                allFiles.Add(filename, meta);
                openFiles.Add(filename, meta);
                Console.WriteLine("Create file " + filename);
            }
            else
            {
                Console.WriteLine("Could not create the file " + filename);
            }
        }

        public void Open(string filename)
        {
            Metadata meta = bridge.Open(filename);

            if (meta != null)
            {
                openFiles.Add(filename, meta);
                Console.WriteLine("Open file " + filename);
            }
            else
            {
                Console.WriteLine("File already opened.");
            }

        }

        public void Read(string filename)
        {
            Console.WriteLine("Read file " + filename);
        }

        public void Write(string filename)
        {
            Console.WriteLine("Write file " + filename);
        }

        public void Close(string filename)
        {
            bridge.Close(filename);

            if (openFiles.ContainsKey(filename))
            {
                openFiles.Remove(filename);
                Console.WriteLine("Close file " + filename);
            }
            else
            {
                Console.WriteLine("File already closed.");
            }
        }

        public void Delete(string filename)
        {
            if (!openFiles.ContainsKey(filename))
            {
                bridge.Delete(filename);
                Console.WriteLine("Delete file " + filename);
            }
            else
            {
                Console.WriteLine("File is opened.");
            }
        }

        public void UpdateServers(Dictionary<string, string> servers, string primary)
        {
            bridge.Servers = servers;
            bridge.Primary = primary;
        }

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            Client c = new Client(arguments[0], arguments[1]);
            // Fazer coisas que Iuri mandar
            TcpChannel channel = new TcpChannel(c.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(c, c.name, typeof(Client));
            Console.WriteLine(c.name);
            Console.ReadLine();
        }
    }
}
