using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;

namespace padiFS
{
    public class MetadataServer : MarshalByRefObject, IMetadataServer
    {
        private static TcpChannel channel;
        private MetadataState state;
        private string name;
        private string address;
        private int port;
        private int pingInterval = 5;
        private string primary;
        private Dictionary<string, string> replicas;
        private Dictionary<string, string> clients;
        private List<string> deadReplicas;
        private Dictionary<string, string> liveDataServers;
        private Dictionary<string, string> deadDataServers;
        private Dictionary<string, int> serversLoad;
        private Dictionary<string, Metadata> files;
        private Dictionary<string, List<string>> tempOpenFiles;
        private Dictionary<string, int> pendingFiles;
        private System.Timers.Timer pingDataServersTimer;
        private System.Timers.Timer pingPrimaryReplicaTimer;


        public MetadataServer(string name, string port)
        {
            this.state = new NormalState();
            this.name = name;
            this.port = int.Parse(port);
            this.address = "tcp://localhost:" + this.port + "/" + this.name;
            this.primary = null;
            this.replicas = new Dictionary<string, string>();
            this.clients = new Dictionary<string, string>();
            this.deadReplicas = new List<string>();
            this.liveDataServers = new Dictionary<string, string>();
            this.deadDataServers = new Dictionary<string, string>();
            this.serversLoad = new Dictionary<string, int>();
            this.files = new Dictionary<string, Metadata>();
            this.tempOpenFiles = new Dictionary<string, List<string>>();
            this.pendingFiles = new Dictionary<string, int>();
            this.pingDataServersTimer = new System.Timers.Timer();
            pingDataServersTimer.Elapsed += new System.Timers.ElapsedEventHandler(pingDataServers);
            pingDataServersTimer.Interval = 1000 * pingInterval;
            this.pingPrimaryReplicaTimer = new System.Timers.Timer();
            pingPrimaryReplicaTimer.Elapsed += new System.Timers.ElapsedEventHandler(PingPrimaryReplica);
            pingPrimaryReplicaTimer.Interval = 1000 * pingInterval;

            Console.WriteLine("ID: {0}", Util.MetadataServerId(name));
        }
        public Dictionary<string, Metadata> Files
        {
            get { return this.files; }
        }

        public Dictionary<string, List<string>> TempOpenFiles
        {
            get { return this.tempOpenFiles; }
        }

        public Dictionary<string, int> PendingFiles
        {
            get { return this.pendingFiles; }
        }

        public Dictionary<string, int> ServersLoad
        {
            get { return this.serversLoad; }
            set { this.serversLoad = value;}
        }
       
        public Dictionary<string, string> LiveDataServers
        {
            get { return this.liveDataServers; }
        }

        public Dictionary<string, string> Replicas
        {
            get { return this.replicas; }
        }

        public Dictionary<string, string> Clients
        {
            get { return this.clients; }
        }

        public Dictionary<string, string> DeadDataServers
        {
            get { return this.deadDataServers; }
        }
        
        public string Name
        {
            get { return this.name; }
        }
       
        public string Primary
        {
            get { return this.primary; }
        }

        public string Address
        {
            get { return this.address; }
        }

        public int Port
        {
            get { return this.port; }
        }

        protected void setStateFail()
        {
            this.state = new FailedState();
        }
        
        protected void setStateNormal()
        {
            this.state = new NormalState();
        }
        
        // Project API
        public Metadata Open(string clientName, string filename)
        {
            return this.state.Open(this, clientName, filename);
        }

        public void Close(string clientName, string filename)
        {
            this.state.Close(this, clientName, filename);
        }

        public Metadata Create(string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            return this.state.Create(this, clientName, filename, serversNumber, readQuorum, writeQuorum); 
        }

        public void Delete(string clientName, string filename)
        {
            this.state.Delete(this, clientName, filename);
        }
        
        // Puppet Master Commands
        public void Fail() {
         
            lock (this)
            {
                this.setStateFail();
                // Should we deactivate the timers!? Maybe we can get rid of if's checking if it's on
                // failure or not. Pretty code! :)
                pingPrimaryReplicaTimer.Enabled = false;
                pingDataServersTimer.Enabled = false;
            }
            Console.WriteLine("On Failure!");
        }

        public void Recover() {
            foreach (string replica in replicas.Keys)
            {
                try
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[replica]);
                    if (server != null)
                    {
                        if (server.Ping())
                        {
                            this.primary = server.GetPrimary();
                            Console.WriteLine("PRIMARY " + this.primary);
                            Console.WriteLine("REPLICA " + replica);
                            if (this.primary == replica)
                            {
                                MetadataInfo info = server.GetMetadataInfo();
                                UpdateReplica(info);
                            }
                            else
                            {
                                // To prevent Lightning bolt failures
                                if (replicas.ContainsKey(this.primary))
                                {
                                    IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[this.primary]);
                                    if (primary != null)
                                    {
                                        MetadataInfo info = primary.GetMetadataInfo();
                                        UpdateReplica(info);
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                catch (ServerNotAvailableException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            foreach (string replica in replicas.Keys)
            {
                if (!deadReplicas.Contains(replica))
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[replica]);
                    if (server != null)
                    {
                        server.Recovered(this.name);
                    }
                }
            }

            lock (this)
            {
                this.setStateNormal();
            }

            Console.WriteLine("Uhf, recovered at last...");
        }

        public void Recovered(string name)
        {
            if (deadReplicas.Contains(name))
            {
                deadReplicas.Remove(name);
            }
        }
        
        
        // Auxiliar API
        public void RegisterDataServer(string name, string address)
        {
            Console.WriteLine("Data Server " + name + " : " + address);
            liveDataServers.Add(name, address);
            serversLoad.Add(name, 0);
        }

        public void RegisterClient(string name, string address)
        {
            Console.WriteLine("Client " + name + " : " + address);
            clients.Add(name, address);
        }


        public void RegisterMetadataServer(string name, string address)
        {
            this.state.RegisterMetadataServer(this, name, address);
        }

        public void UpdateReplica(MetadataInfo info)
        {
            SetPrimary(info.Primary);
            this.replicas = info.Replicas;

            if (replicas.ContainsKey(name))
            {
                replicas.Remove(name);
            }

            if (!replicas.ContainsKey(primary))
            {
                this.replicas.Add(primary, info.Address);
            }
            this.liveDataServers = info.LiveDataServers;
            this.deadDataServers = info.DeadDataServers;
            this.serversLoad = info.ServersLoad;
            this.files = info.Files;
            this.tempOpenFiles = info.OpenFiles;

            Console.WriteLine("Updated metadata info.");
        }

        public MetadataInfo GetMetadataInfo()
        {
            return new MetadataInfo(primary, address, replicas, liveDataServers, deadDataServers, serversLoad, files, tempOpenFiles);
        }


        public void PingDataServer(object threadContext)
        {
            List<string> dataservers = (List<string>)threadContext;
            string name = dataservers[0];
            string address = dataservers[1];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

            try
            {
                if (server.Ping())
                {
                    Console.WriteLine(name + ": VIVO");
                    if (!liveDataServers.ContainsKey(name))
                    {
                        liveDataServers.Add(name, address);
                        deadDataServers.Remove(name);
                    }
                }
                //else
                //{
                //    Console.WriteLine(name + ": MORTO");
                //    if (!deadDataServers.ContainsKey(name))
                //    {
                //        deadDataServers.Add(name, address);
                //        liveDataServers.Remove(name);
                //    }
                //}
            }
            catch (ServerNotAvailableException e)
            {
                Console.WriteLine(e.Message);
                //Console.WriteLine(name + ": MORTO");
                if (!deadDataServers.ContainsKey(name))
                {
                    deadDataServers.Add(name, address);
                    liveDataServers.Remove(name);
                }
            }
            catch (System.IO.IOException)
            {
                //Console.WriteLine(e.Message);
                Console.WriteLine(name + ": Desligado");
                if (!deadDataServers.ContainsKey(name))
                {
                    deadDataServers.Add(name, address);
                    liveDataServers.Remove(name);
                }
            }
        }


        private void pingDataServers(object source, ElapsedEventArgs e)
        {
            this.state.pingDataServers(this, source, e);
        }

        public void PingReplica(object threadContext)
        {
            string replica = (string)threadContext;
            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[replica]);

            try
            {
                if (server.Ping())
                {
                    Console.WriteLine(replica + ": VIVO");
                }
                //else
                //{
                //    deadReplicas.Add(replica);
                //    Console.WriteLine("ELSE: esta é a primary: {0}", replica);
                //    Console.WriteLine(replica + ": MORTO");
                //    NextPrimaryReplica();
                //}
            }
            catch (ServerNotAvailableException e)
            {
                deadReplicas.Add(replica);
                Console.WriteLine(e.Message);
                //Console.WriteLine("EXCEP: esta é a primary: {0}", replica);
                //Console.WriteLine(replica + ": MORTO");
                NextPrimaryReplica();
            }
            catch (System.IO.IOException)
            {
                deadReplicas.Add(replica);
                //Console.WriteLine(e.Message);
                //Console.WriteLine("EXCEP: esta é a primary: {0}", replica);
                //Console.WriteLine(replica + ": MORTO");
                NextPrimaryReplica();
            }
        }


        private void PingPrimaryReplica(object source, ElapsedEventArgs e)
        {
            this.state.PingPrimaryReplica(this, source, e);
        }

        public bool Ping()
        {
            return this.state.Ping();
        }

        public string GetPrimary()
        {
            return this.primary;
        }

        public void SetPrimary(string name)
        {
            this.primary = name;
            if (this.primary == this.name)
            {
                pingDataServersTimer.Enabled = true;
                pingPrimaryReplicaTimer.Enabled = false;
            }
            else
            {
                pingPrimaryReplicaTimer.Enabled = true;
                pingDataServersTimer.Enabled = false;
            }
            
        }

        private void NextPrimaryReplica()
        {
            int id = Util.MetadataServerId(this.name);
            string replica = this.name;

            foreach (string r in replicas.Keys)
            {
                if (r != primary)
                {
                    try
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[r]);
                        if (server != null)
                        {
                            server.Ping();
                            int r_id = Util.MetadataServerId(r);
                            if (r_id < id)
                            {
                                id = r_id;
                                replica = r;
                            }
                        }
                    }
                    catch (ServerNotAvailableException) { }
                    catch (System.IO.IOException) { }
                    catch (System.Net.Sockets.SocketException) { }
                }
            }
            Console.WriteLine("new primary: " + replica);
            SetPrimary(replica);
        }

        public string Dump()
        {
            string s = "Metadata Server " + name + " dump:\r\nFiles:\r\n";

            // files keepped by metadata server
            foreach (string m in files.Keys)
            {
                s += files[m].ToString() + "\r\n";
                foreach(string d in files[m].DataServers)
                {
                    s += "\t" + d + "\r\n";
                }
            }

            // files open in metadata server
            s += "Open Files:\r\n";
            foreach (string m in tempOpenFiles.Keys)
            {
                s += files[m].ToString() + "\r\n";
                foreach (string c in tempOpenFiles[m])
                {
                    s += "\t" + c + "\r\n";
                }
            }
            s += "Pending Files:\r\n";
            foreach (string m in pendingFiles.Keys)
            {
                s += files[m].ToString() + "\r\n";
            }
            // files open in metadata server
            s += "Replicas:\r\n";
            foreach (string m in replicas.Keys)
            {
                s += replicas[m].ToString() + "\r\n";
            }


            return s;
        }

        public void UpdateFileMetada(string address)
        {
            Dictionary<string, int> updated = new Dictionary<string, int>();

            foreach (string f in pendingFiles.Keys)
            {
                Metadata meta = files[f];
                meta.AddDataServers(address);

                if (tempOpenFiles.ContainsKey(f))
                {
                    List<string> clients = tempOpenFiles[f];

                    foreach (string c in clients)
                    {
                        IClient client = (IClient)Activator.GetObject(typeof(IClient), this.clients[c]);

                        if (client != null)
                        {
                            client.UpdateFileMetadata(f, meta);
                        }
                    }
                }

                string input = DateTime.Now.ToString("o") + (char)0x7f + meta.FileName;

                //IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

                //if (server != null)
                //{
                //    server.Create(input);
                //}

                int n = pendingFiles[f] - 1;

                if (n > 0)
                {
                    updated.Add(f, n);
                }

            }

            pendingFiles = new Dictionary<string, int>(updated);
        }

        // TEST AREA
        private static void Exit(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Control+C hit. Shutting down.");
            Environment.Exit(0);
        }

        private static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                channel.StopListening(null);
                Console.WriteLine("Exit");
            }
            return false;
        }
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
        // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        //

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            MetadataServer ms = new MetadataServer(arguments[0], arguments[1]);
            Console.Title = "Iurie's Metadata Server: " + ms.name;
            // Ficar esperar pedidos de Iurie
            channel = new TcpChannel(ms.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(ms, ms.name, typeof(MetadataServer));

            // TEST AREA
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            //SetConsoleCtrlHandler(handler, true);
            //Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);
            //
            Console.ReadLine();
        }
    }
}
