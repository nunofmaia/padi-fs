using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace padiFS
{
    public class MetadataServer
    {
        private string name;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, Metadata> files;

        public MetadataServer(string name)
        {
            this.name = name;
            metadataServers = new Dictionary<string, string>();
            this.files = new Dictionary<string, Metadata>();
        }

        static void Main(string[] args)
        {
            MetadataServer ms = new MetadataServer(args[0]);
            // Ficar esperar pedidos de Iurie
            TcpChannel channel = new TcpChannel(8081);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(MetadataServer), "tcp://localhost:8081/" + ms.name, WellKnownObjectMode.Singleton);
            IPuppetMaster master = (IPuppetMaster) Activator.GetObject(typeof(IPuppetMaster), "tcp://localhost:8080/PuppetMaster");
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
