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
using System.Diagnostics;
using System.IO;

namespace padiFS
{
    public class MetadataServer : MarshalByRefObject, IMetadataServer
    {
        private int pingDataServerInterval;
        private int pingMetadataServerInterval;
        private int serializationInterval;
        private double percentage;

        private static TcpChannel Channel { set; get; }
        private MetadataState State { set; get; }
        public string Name { set; get; }
        public string Address { set; get; }
        public int Port { set; get; }
        public string Primary { set; get; }
        public SerializableDictionary<string, string> Replicas { set; get; }
        public SerializableDictionary<string, string> Clients { set; get; }
        public List<string> DeadReplicas { set; get; }
        public SerializableDictionary<string, string> LiveDataServers { set; get; }
        public SerializableDictionary<string, string> DeadDataServers { set; get; }
        public SerializableDictionary<string, int> ServersLoad { set; get; }
        public SerializableDictionary<string, Metadata> Files { set; get; }
        public SerializableDictionary<string, List<string>> OpenFiles { set; get; }
        public SerializableDictionary<string, int> PendingFiles { set; get; }
        public SerializableDictionary<string, DataInfo> DataServersInfo { set; get; }
        public Log Log { set; get; }

        private System.Timers.Timer pingDataServersTimer;
        private System.Timers.Timer pingPrimaryReplicaTimer;
        private System.Timers.Timer serializationTimer;

        public MetadataServer()
        {
        }

        public MetadataServer(string name, string port)
        {
            this.State = new NormalState();
            this.Name = name;
            this.Port = int.Parse(port);
            this.Address = "tcp://localhost:" + this.Port + "/" + this.Name;
            this.Primary = null;
            this.Replicas = new SerializableDictionary<string, string>();
            this.Clients = new SerializableDictionary<string, string>();
            this.DeadReplicas = new List<string>();
            this.LiveDataServers = new SerializableDictionary<string, string>();
            this.DeadDataServers = new SerializableDictionary<string, string>();
            this.ServersLoad = new SerializableDictionary<string, int>();
            this.Files = new SerializableDictionary<string, Metadata>();
            this.OpenFiles = new SerializableDictionary<string, List<string>>();
            this.PendingFiles = new SerializableDictionary<string, int>();
            this.DataServersInfo = new SerializableDictionary<string, DataInfo>();

            this.pingDataServerInterval = 25;
            this.pingMetadataServerInterval = 5;
            this.serializationInterval = 15;
            this.percentage = 0.2;


            this.pingDataServersTimer = new System.Timers.Timer();
            pingDataServersTimer.Elapsed += new System.Timers.ElapsedEventHandler(pingDataServers);
            pingDataServersTimer.Interval = 1000 * pingDataServerInterval;
            this.pingPrimaryReplicaTimer = new System.Timers.Timer();
            pingPrimaryReplicaTimer.Elapsed += new System.Timers.ElapsedEventHandler(PingPrimaryReplica);
            pingPrimaryReplicaTimer.Interval = 1000 * pingMetadataServerInterval;

            this.serializationTimer = new System.Timers.Timer();
            serializationTimer.Elapsed += new System.Timers.ElapsedEventHandler(SerializeServer);
            serializationTimer.Interval = 1000 * serializationInterval;

            string dir = Environment.CurrentDirectory + string.Format(@"\{0}", this.Name);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            this.Log = new padiFS.Log(dir + @"\Log.txt");

            serializationTimer.Enabled = true;

            Console.WriteLine("ID: {0}", Util.MetadataServerId(name));
        }

        protected void setStateFail()
        {
            this.State = new FailedState();
        }
        
        protected void setStateNormal()
        {
            this.State = new NormalState();
        }

        public double Percentage
        {
            get
            {
                return this.percentage;
            }
        }
        
        // Project API
        public Metadata Open(string clientName, string filename)
        {
            return this.State.Open(this, clientName, filename);
        }

        public void Close(string clientName, string filename)
        {
            this.State.Close(this, clientName, filename);
        }

        public Metadata Create(string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            return this.State.Create(this, clientName, filename, serversNumber, readQuorum, writeQuorum); 
        }

        public void Delete(string clientName, string filename)
        {
            this.State.Delete(this, clientName, filename);
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
            foreach (string replica in this.Replicas.Keys)
            {
                try
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[replica]);
                    if (server != null)
                    {
                        if (server.Ping())
                        {
                            this.Primary = server.GetPrimary();
                            Console.WriteLine("PRIMARY " + this.Primary);
                            Console.WriteLine("REPLICA " + replica);
                            if (this.Primary == replica)
                            {
                                //MetadataInfo info = server.GetMetadataInfo();
                                //UpdateReplica(info);
                                Log log = server.GetLog();
                                UpdateLog(log);
                            }
                            else
                            {
                                // To prevent Lightning bolt failures
                                if (this.Replicas.ContainsKey(this.Primary))
                                {
                                    IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[this.Primary]);
                                    if (primary != null)
                                    {
                                        //MetadataInfo info = primary.GetMetadataInfo();
                                        //UpdateReplica(info);
                                        Log log = primary.GetLog();
                                        UpdateLog(log);
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

            foreach (string replica in this.Replicas.Keys)
            {
                if (!this.DeadReplicas.Contains(replica))
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[replica]);
                    if (server != null)
                    {
                        server.Recovered(this.Name);
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
            if (this.DeadReplicas.Contains(name))
            {
                this.DeadReplicas.Remove(name);
            }
        }
        
        
        // Auxiliar API
        public void RegisterDataServer(string name, string address)
        {
            Console.WriteLine("Data Server " + name + " : " + address);
            this.LiveDataServers.Add(name, address);
            this.DataServersInfo.Add(name, null);
            this.ServersLoad.Add(name, 0);

            this.Log.Append(string.Format("REGISTER data {0} {1}", name, address));
        }

        public void RegisterClient(string name, string address)
        {
            Console.WriteLine("Client " + name + " : " + address);
            this.Clients.Add(name, address);

            this.Log.Append(string.Format("REGISTER client {0} {1}", name, address));
        }


        public void RegisterMetadataServer(string name, string address)
        {
            this.State.RegisterMetadataServer(this, name, address);
        }

        public void UpdateReplica(MetadataInfo info)
        {
            SetPrimary(info.Primary);
            this.Replicas = info.Replicas;

            if (this.Replicas.ContainsKey(this.Name))
            {
                this.Replicas.Remove(this.Name);
            }

            if (!this.Replicas.ContainsKey(this.Primary))
            {
                this.Replicas.Add(this.Primary, info.Address);
            }
            this.LiveDataServers = info.LiveDataServers;
            this.DeadDataServers = info.DeadDataServers;
            this.ServersLoad = info.ServersLoad;
            this.Files = info.Files;
            this.OpenFiles = info.OpenFiles;

            Console.WriteLine("Updated metadata info.");
        }

        public MetadataInfo GetMetadataInfo()
        {
            return new MetadataInfo(this.Primary, this.Address, this.Replicas, this.LiveDataServers, this.DeadDataServers, this.ServersLoad, this.Files, this.OpenFiles);
        }


        public void PingDataServer(object threadContext)
        {
            List<string> dataservers = (List<string>)threadContext;
            string name = dataservers[0];
            string address = dataservers[1];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

            try
            {
                //if (server.Ping())
                //{
                //    Console.WriteLine(name + ": VIVO");
                //    if (!liveDataServers.ContainsKey(name))
                //    {
                //        liveDataServers.Add(name, address);
                //        deadDataServers.Remove(name);
                //    }
                //}

                this.DataServersInfo[name] = server.Ping();
                Console.WriteLine(name + ": VIVO");
                if (!this.LiveDataServers.ContainsKey(name))
                {
                    this.LiveDataServers.Add(name, address);
                    this.DeadDataServers.Remove(name);
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
                if (!this.DeadDataServers.ContainsKey(name))
                {
                    this.DeadDataServers.Add(name, address);
                    this.LiveDataServers.Remove(name);
                }
            }
            catch (System.IO.IOException)
            {
                //Console.WriteLine(e.Message);
                Console.WriteLine(name + ": Desligado");
                if (!this.DeadDataServers.ContainsKey(name))
                {
                    this.DeadDataServers.Add(name, address);
                    this.LiveDataServers.Remove(name);
                }
            }
        }


        private void pingDataServers(object source, ElapsedEventArgs e)
        {
            this.State.pingDataServers(this, source, e);
        }

        public void PingReplica(object threadContext)
        {
            string replica = (string)threadContext;
            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[replica]);

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
                this.DeadReplicas.Add(replica);
                Console.WriteLine(e.Message);
                //Console.WriteLine("EXCEP: esta é a primary: {0}", replica);
                //Console.WriteLine(replica + ": MORTO");
                NextPrimaryReplica();
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("IOException");
                this.DeadReplicas.Add(replica);
                //Console.WriteLine(e.Message);
                //Console.WriteLine("EXCEP: esta é a primary: {0}", replica);
                //Console.WriteLine(replica + ": MORTO");
                NextPrimaryReplica();
            }
            catch (System.Net.Sockets.SocketException)
            {
            }
        }


        private void PingPrimaryReplica(object source, ElapsedEventArgs e)
        {
            this.State.PingPrimaryReplica(this, source, e);
        }

        private void SerializeServer(object source, ElapsedEventArgs e)
        {
            string directory = Environment.CurrentDirectory + string.Format(@"\{0}", this.Name);
            string filename = "Backup.txt";

            Util.SerializeObject(this, directory, filename);
        }

        public bool Ping()
        {
            return this.State.Ping();
        }

        public string GetPrimary()
        {
            return this.Primary;
        }

        public void SetPrimary(string name)
        {
            this.Primary = name;
            if (this.Primary == this.Name)
            {
                pingDataServersTimer.Enabled = true;
                pingPrimaryReplicaTimer.Enabled = false;
            }
            else
            {
                pingPrimaryReplicaTimer.Enabled = true;
                pingDataServersTimer.Enabled = false;
            }

            this.Log.Append(string.Format("SET-PRIMARY {0}", name));
            
        }

        private void NextPrimaryReplica()
        {
            int id = Util.MetadataServerId(this.Name);
            string replica = this.Name;

            foreach (string r in this.Replicas.Keys)
            {
                if (r != this.Primary)
                {
                    try
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[r]);
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
                    catch (System.IO.IOException) {}
                    catch (System.Net.Sockets.SocketException) { }
                }
            }
            Console.WriteLine("new primary: " + replica);
            SetPrimary(replica);
        }

        public string Dump()
        {
            string s = "Metadata Server " + this.Name + " dump:\r\nFiles:\r\n";

            // files keepped by metadata server
            foreach (string m in this.Files.Keys)
            {
                s += this.Files[m].ToString() + "\r\n";
                foreach(string d in this.Files[m].DataServers)
                {
                    s += "\t" + d + "\r\n";
                }
            }

            // files open in metadata server
            s += "Open Files:\r\n";
            foreach (string m in this.OpenFiles.Keys)
            {
                s += this.Files[m].ToString() + "\r\n";
                foreach (string c in this.OpenFiles[m])
                {
                    s += "\t" + c + "\r\n";
                }
            }
            s += "Pending Files:\r\n";
            foreach (string m in this.PendingFiles.Keys)
            {
                s += this.Files[m].ToString() + "\r\n";
            }
            // files open in metadata server
            s += "Replicas:\r\n";
            foreach (string m in this.Replicas.Keys)
            {
                s += this.Replicas[m].ToString() + "\r\n";
            }


            return s;
        }

        public void UpdateFileMetada(string address)
        {
            if (this.PendingFiles.Count > 0)
            {
                SerializableDictionary<string, int> updated = new SerializableDictionary<string, int>();
                string command = string.Format("UPDATE {0}", address);

                foreach (string f in this.PendingFiles.Keys)
                {
                    Metadata meta = this.Files[f];
                    meta.AddDataServers(address);

                    command += string.Format(" {0}", f);

                    if (this.OpenFiles.ContainsKey(f))
                    {
                        List<string> clients = this.OpenFiles[f];

                        foreach (string c in clients)
                        {
                            IClient client = (IClient)Activator.GetObject(typeof(IClient), this.Clients[c]);

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

                    int n = this.PendingFiles[f] - 1;

                    if (n > 0)
                    {
                        updated.Add(f, n);
                    }

                }

                this.PendingFiles = new SerializableDictionary<string, int>(updated);

                this.Log.Append(command);

                foreach (string s in this.Replicas.Keys)
                {
                    IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[s]);

                    if (replica != null)
                    {
                        replica.AppendToLog(command);
                    }
                }
            }
        }

        public DateTime GetTimestamp()
        {
            return DateTime.Now;
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
                Channel.StopListening(null);
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

        public override object InitializeLifetimeService()
        {
            return null;
        }

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            MetadataServer ms = new MetadataServer(arguments[0], arguments[1]);
            Console.Title = "Iurie's Metadata Server: " + ms.Name;
            // Ficar esperar pedidos de Iurie
            Channel = new TcpChannel(ms.Port);
            ChannelServices.RegisterChannel(Channel, true);
            RemotingServices.Marshal(ms, ms.Name, typeof(MetadataServer));

            // TEST AREA
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            //SetConsoleCtrlHandler(handler, true);
            //Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);
            //
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}


            Console.ReadLine();
        }


        public void AppendToLog(string command)
        {
            lock (this)
            {
                string[] args = command.Split(' ');
                string code = args[0];

                switch (code)
                {
                    case "CREATE":
                        {
                            string clientName = args[1];
                            string filename = args[2];
                            int serversNumber = int.Parse(args[3]);
                            int readQuorum = int.Parse(args[4]);
                            int writeQuorum = int.Parse(args[5]);

                            if (!this.Files.ContainsKey(filename))
                            {
                                if (this.LiveDataServers.Count < serversNumber)
                                {
                                    if (!this.PendingFiles.ContainsKey(filename))
                                    {
                                        this.PendingFiles.Add(filename, serversNumber - this.LiveDataServers.Count);
                                    }
                                }

                                List<string> servers = new List<string>();
                                string[] chosen = Util.SliceArray(args, 6, args.Length);

                                // Before sending the requests, a time stamp is added to the filename
                                string f = DateTime.Now.ToString("o") + (char)0x7f + filename;
                                foreach (string v in chosen)
                                {
                                    servers.Add(this.LiveDataServers[v]);
                                    this.ServersLoad[v]++;
                                }

                                this.ServersLoad = Util.SortServerLoad(this.ServersLoad);
                                Metadata meta = new Metadata(filename, serversNumber, readQuorum, writeQuorum, servers);
                                List<string> clientsList = new List<string>();
                                clientsList.Add(clientName);
                                this.Files.Add(filename, meta);
                                this.OpenFiles.Add(filename, clientsList);
                            }
                        }
                        break;
                    case "OPEN":
                        {
                            string clientName = args[1];
                            string filename = args[2];

                            if (this.OpenFiles.ContainsKey(filename))
                            {
                                List<string> clientsList = this.OpenFiles[filename];
                                if (!clientsList.Contains(clientName))
                                {
                                    this.OpenFiles[filename].Add(clientName);
                                }
                            }
                            else
                            {
                                if (this.Files.ContainsKey(filename))
                                {
                                    List<string> clientsList = new List<string>();
                                    clientsList.Add(clientName);
                                    this.OpenFiles.Add(filename, clientsList);
                                }
                            }
                        }
                        break;
                    case "CLOSE":
                        {
                            string clientName = args[1];
                            string filename = args[2];

                            if (this.Files.ContainsKey(filename))
                            {
                                if (this.OpenFiles.ContainsKey(filename))
                                {
                                    List<string> clientsList = this.OpenFiles[filename];

                                    if (clientsList.Contains(clientName))
                                    {
                                        clientsList.Remove(clientName);

                                        if (clientsList.Count == 0)
                                        {
                                            this.OpenFiles.Remove(filename);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "DELETE":
                        {
                            string clientName = args[1];
                            string filename = args[2];

                            if (this.Files.ContainsKey(filename))
                            {
                                this.Files.Remove(filename);
                                this.OpenFiles.Remove(filename);
                            }
                        }
                        break;
                    case "REGISTER":
                        {
                            string server = args[1];
                            string name = args[2];
                            string address = args[3];

                            switch (server)
                            {
                                case "data":
                                    if (!this.DataServersInfo.ContainsKey(name))
                                    {
                                        this.LiveDataServers.Add(name, address);
                                        this.DataServersInfo.Add(name, null);
                                        this.ServersLoad.Add(name, 0);
                                    }
                                    break;
                                case "metadata":
                                    if (!this.Replicas.ContainsKey(name) && name != this.Name)
                                    {
                                        this.Replicas.Add(name, address);
                                    }
                                    break;
                                case "client":
                                    if (!this.Clients.ContainsKey(name))
                                    {
                                        this.Clients.Add(name, address);
                                    }
                                    break;
                            }
                        }
                        break;
                    case "UPDATE":
                        {
                            string address = args[1];
                            string[] files = Util.SliceArray(args, 2, args.Length);
                            SerializableDictionary<string, int> updated = new SerializableDictionary<string, int>();
                            bool contains = false;
                            foreach (string f in files)
                            {
                                Metadata meta = this.Files[f];
                                meta.AddDataServers(address);

                                //if (this.OpenFiles.ContainsKey(f))
                                //{
                                //    List<string> clients = this.OpenFiles[f];

                                //    foreach (string c in clients)
                                //    {
                                //        IClient client = (IClient)Activator.GetObject(typeof(IClient), this.clients[c]);

                                //        if (client != null)
                                //        {
                                //            client.UpdateFileMetadata(f, meta);
                                //        }
                                //    }
                                //}

                                //string input = DateTime.Now.ToString("o") + (char)0x7f + meta.FileName;

                                //IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

                                //if (server != null)
                                //{
                                //    server.Create(input);
                                //}

                                if (this.PendingFiles.ContainsKey(f))
                                {
                                    int n = this.PendingFiles[f] - 1;

                                    if (n > 0)
                                    {
                                        updated.Add(f, n);
                                    }
                                    contains = true;
                                }
                            }
                            if (contains)
                            {
                                this.PendingFiles = new SerializableDictionary<string, int>(updated);
                            }
                        }
                        break;
                    case "SET-PRIMARY":
                        {
                            string primary = args[1];
                            this.Primary = primary;

                            if (this.Primary == this.Name)
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
                        break;
                }

                this.Log.Append(command);
            }
        }


        public void UpdateLog(Log log)
        {
            int primaryIndex = log.Index;
            int index = this.Log.Index;

            if (primaryIndex > index)
            {
                string[] commands = log.Read(index + 1);

                foreach (string command in commands)
                {
                    AppendToLog(command);
                }
            }
        }

        public Log GetLog()
        {
            return this.Log;
        }
    }
}
