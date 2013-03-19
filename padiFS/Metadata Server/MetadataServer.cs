using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace padiFS
{
    public class MetadataServer : MarshalByRefObject, IMetadataServer
    {
        private string name;
        private int port;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, string> dataServers;
        private Dictionary<string, Metadata> files;


        public MetadataServer(string id)
        {
            this.name = "m-" + id;
            this.port = 8080 + int.Parse(id);
            this.metadataServers = new Dictionary<string, string>();
            this.dataServers = new Dictionary<string, string>();
            this.files = new Dictionary<string, Metadata>();
        }


        // Project API
        public Metadata Open(string filename)
        { 
            return null;
        }
        public void Close() { }
        public Metadata Create(string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            return null;
        }
        public void Delete() { }
        public void Fail() { }
        public void Recover() { }
        
        
        // Auxiliar API
        public void RegisterDataServer(string name, string address)
        {
            Console.WriteLine("Data Server " + name + " : " + address);
            dataServers.Add(name, address);
        }

        void RegisterMetadataServer(string name, string address)
        {
            Console.WriteLine("Metadata Server " + name + " : " + address);
            metadataServers.Add(name, address);
        }

        static void Main(string[] args)
        {
            MetadataServer ms = new MetadataServer(args[0]);
            // Ficar esperar pedidos de Iurie
            TcpChannel channel = new TcpChannel(ms.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(ms, ms.name, typeof(MetadataServer));
            IPuppetMaster master = (IPuppetMaster) Activator.GetObject(typeof(IPuppetMaster), "tcp://localhost:8070/PuppetMaster");
            if (master != null)
            {
                try
                {
                    master.test(ms.name);
                }
                catch (RemotingException e)
                { Console.WriteLine(e.StackTrace); }
            }
            else
            {
                Console.WriteLine(ms.name);
            }
            Console.ReadLine();
        }
    }
}
