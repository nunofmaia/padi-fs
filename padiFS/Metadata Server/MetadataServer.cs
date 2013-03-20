using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;

namespace padiFS
{
    public class MetadataServer : MarshalByRefObject, IMetadataServer
    {
        private string name;
        private int port;
        private int pingInterval = 5;
        private bool primary = false;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, string> liveDataServers;
        private Dictionary<string, string> deadDataServers;
        private Dictionary<string, Metadata> files;
        private System.Timers.Timer pingDataServersTimer;

        public MetadataServer(string id)
        {
            this.name = "m-" + id;
            this.port = 8080 + int.Parse(id);
            this.metadataServers = new Dictionary<string, string>();
            this.liveDataServers = new Dictionary<string, string>();
            this.deadDataServers = new Dictionary<string, string>();
            this.files = new Dictionary<string, Metadata>();
            this.pingDataServersTimer = new System.Timers.Timer();
            pingDataServersTimer.Elapsed += new System.Timers.ElapsedEventHandler(pingDataServers);
            pingDataServersTimer.Interval = 1000 * pingInterval;
        }


        // Project API
        public Metadata Open(string filename)
        { 
            return null;
        }
        public void Close() { }

        private void CreateCallback(object threadcontext)
        {
            List<string> args = (List<string>)threadcontext;
            string v = args[0];
            string filename = args[1];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), v);

            if (server != null)
            {
                server.Create(filename);
            }
        }

        public Metadata Create(string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            if (liveDataServers.Count >= serversNumber)
            {
                List<string> servers = new List<string>();
                foreach (string v in liveDataServers.Values.Take(serversNumber))
                {
                    List<string> arguments = new List<string>();
                    arguments.Add(v);
                    arguments.Add(filename);
                    servers.Add(v);
                    ThreadPool.QueueUserWorkItem(CreateCallback, arguments);
                }

                return new Metadata(filename, serversNumber, readQuorum, writeQuorum, servers);
            }
            else
            {
                Console.WriteLine("Not enough servers.");
            }

            return null;
        }
        public void Delete() { }
        public void Fail() { }
        public void Recover() { }
        
        
        // Auxiliar API
        public void RegisterDataServer(string name, string address)
        {
            Console.WriteLine("Data Server " + name + " : " + address);
            liveDataServers.Add(name, address);
        }

        public void RegisterMetadataServer(string name, string address)
        {
            // If the server doesn't have the new metadata registered,
            // registers it and introduces to it "Hi, I'm Iurie's metadata server"
            if (!metadataServers.ContainsKey(name))
            {
                Console.WriteLine("Metadata Server " + name + " : " + address);
                metadataServers.Add(name, address);
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                if (server != null)
                {
                    try
                    {
                        server.RegisterMetadataServer(this.name, "tcp://localhost:" + this.port + "/" + this.name);
                    }
                    catch (System.Net.Sockets.SocketException) { }
                    // Ignore it
                }
            }
        }


        private void PingLiveDataServer(object threadContext)
        {
            string name = (string)threadContext;
            string address = liveDataServers[name];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

            try
            {
                if (server.ping() == 1)
                {
                    Console.WriteLine("VIVO");
                    if(!liveDataServers.ContainsKey(name))
                    {
                        liveDataServers.Add(name, address);
                        deadDataServers.Remove(name);
                    }
                }
            }
            catch (System.SystemException)
            {
                Console.WriteLine("MORTO");
                if (!deadDataServers.ContainsKey(name))
                {
                    deadDataServers.Add(name, address);
                    liveDataServers.Remove(name);
                }
            }
        }


        private void PingDeadDataServer(object threadContext)
        {
            string name = (string)threadContext;
            string address = deadDataServers[name];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

            try
            {
                if (server.ping() == 1)
                {
                    Console.WriteLine("VIVO");
                    if (!liveDataServers.ContainsKey(name))
                    {
                        liveDataServers.Add(name, address);
                        deadDataServers.Remove(name);
                    }
                }
            }
            catch (System.SystemException)
            {
                Console.WriteLine("MORTO");
                if (!deadDataServers.ContainsKey(name))
                {
                    deadDataServers.Add(name, address);
                    liveDataServers.Remove(name);
                }
            }
        }

        private void pingDataServers(object source, ElapsedEventArgs e)
        {
            foreach (string key in liveDataServers.Keys)
            {
                ThreadPool.QueueUserWorkItem(PingLiveDataServer, key);
            }

            foreach (string key in deadDataServers.Keys)
            {
                ThreadPool.QueueUserWorkItem(PingDeadDataServer, key);
            }
        }

        static void Main(string[] args)
        {
            MetadataServer ms = new MetadataServer(args[0]);
            Console.WriteLine(ms.name);
            if (ms.name == "m-0")
            {
                ms.primary = true;
                ms.pingDataServersTimer.Enabled = true;
            }
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
