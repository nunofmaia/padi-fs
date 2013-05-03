﻿using System;
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
        private static TcpChannel channel;
        private MetadataState state;
        private string name;
        private string address;
        private int port;
        private int pingDataServerInterval = 20;
        private int pingMetadataServerInterval = 5;
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
        private Dictionary<string, DataInfo> dataServersInfo;
        private System.Timers.Timer pingDataServersTimer;
        private System.Timers.Timer pingPrimaryReplicaTimer;

        public Log Log { private set; get; }


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
            this.dataServersInfo = new Dictionary<string, DataInfo>();
            this.pingDataServersTimer = new System.Timers.Timer();
            pingDataServersTimer.Elapsed += new System.Timers.ElapsedEventHandler(pingDataServers);
            pingDataServersTimer.Interval = 1000 * pingDataServerInterval;
            this.pingPrimaryReplicaTimer = new System.Timers.Timer();
            pingPrimaryReplicaTimer.Elapsed += new System.Timers.ElapsedEventHandler(PingPrimaryReplica);
            pingPrimaryReplicaTimer.Interval = 1000 * pingMetadataServerInterval;

            string dir = Environment.CurrentDirectory + string.Format(@"\{0}", this.Name);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            this.Log = new padiFS.Log(dir + @"\Log.txt");

            Console.WriteLine("ID: {0}", Util.MetadataServerId(name));
        }

        public Dictionary<string, DataInfo> DataServersInfo
        {
            get { return this.dataServersInfo; }
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
            set { this.pendingFiles = value; }
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
                                //MetadataInfo info = server.GetMetadataInfo();
                                //UpdateReplica(info);
                                Log log = server.GetLog();
                                UpdateLog(log);
                            }
                            else
                            {
                                // To prevent Lightning bolt failures
                                if (replicas.ContainsKey(this.primary))
                                {
                                    IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[this.primary]);
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
            dataServersInfo.Add(name, null);
            serversLoad.Add(name, 0);

            this.Log.Append(string.Format("REGISTER data {0} {1}", name, address));
        }

        public void RegisterClient(string name, string address)
        {
            Console.WriteLine("Client " + name + " : " + address);
            clients.Add(name, address);

            this.Log.Append(string.Format("REGISTER client {0} {1}", name, address));
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
                //if (server.Ping())
                //{
                //    Console.WriteLine(name + ": VIVO");
                //    if (!liveDataServers.ContainsKey(name))
                //    {
                //        liveDataServers.Add(name, address);
                //        deadDataServers.Remove(name);
                //    }
                //}

                dataServersInfo[name] = server.Ping();
                Console.WriteLine(name + ": VIVO");
                if (!liveDataServers.ContainsKey(name))
                {
                    liveDataServers.Add(name, address);
                    deadDataServers.Remove(name);
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
                Console.WriteLine("IOException");
                deadReplicas.Add(replica);
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
            if (this.PendingFiles.Count > 0)
            {
                Dictionary<string, int> updated = new Dictionary<string, int>();
                string command = string.Format("UPDATE {0}", address);

                foreach (string f in pendingFiles.Keys)
                {
                    Metadata meta = files[f];
                    meta.AddDataServers(address);

                    command += string.Format(" {0}", f);

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

        public override object InitializeLifetimeService()
        {
            return null;
        }

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
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
            Console.ReadLine();
        }


        public void AppendToLog(string command)
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
                            Console.WriteLine("ANTES DO SLICE");
                            string[] chosen = Util.SliceArray(args, 6, args.Length);
                            Console.WriteLine("PASSEI O SLICE");

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
                            this.TempOpenFiles.Add(filename, clientsList);
                        } 
                    }
                    break;
                case "OPEN":
                    {
                        string clientName = args[1];
                        string filename = args[2];

                        if (this.TempOpenFiles.ContainsKey(filename))
                        {
                            List<string> clientsList = this.TempOpenFiles[filename];
                            if (!clientsList.Contains(clientName))
                            {
                                this.TempOpenFiles[filename].Add(clientName);
                            }
                        }
                        else
                        {
                            if (this.Files.ContainsKey(filename))
                            {
                                List<string> clientsList = new List<string>();
                                clientsList.Add(clientName);
                                this.TempOpenFiles.Add(filename, clientsList);
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
                            if (this.TempOpenFiles.ContainsKey(filename))
                            {
                                List<string> clientsList = this.TempOpenFiles[filename];

                                if (clientsList.Contains(clientName))
                                {
                                    clientsList.Remove(clientName);

                                    if (clientsList.Count == 0)
                                    {
                                        this.TempOpenFiles.Remove(filename);
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
                            this.TempOpenFiles.Remove(filename);
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
                        Dictionary<string, int> updated = new Dictionary<string, int>();
                        foreach (string f in files)
                        {
                            Metadata meta = this.Files[f];
                            meta.AddDataServers(address);

                            //if (this.TempOpenFiles.ContainsKey(f))
                            //{
                            //    List<string> clients = this.TempOpenFiles[f];

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

                            int n = pendingFiles[f] - 1;

                            if (n > 0)
                            {
                                updated.Add(f, n);
                            }

                        }

                        this.PendingFiles = new Dictionary<string, int>(updated);
                    }
                    break;
            }

            this.Log.Append(command);
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
