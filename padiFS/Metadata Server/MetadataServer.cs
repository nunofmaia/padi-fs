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
        public long Sequencer { set; get; }
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
        private ManualResetEvent migration;
        private ManualResetEvent gettingLog;
        private List<string> migrating;

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
            this.migration = new ManualResetEvent(true);
            this.gettingLog = new ManualResetEvent(true);
            this.migrating = new List<string>();

            this.pingDataServerInterval = 25;
            this.pingMetadataServerInterval = 5;
            this.serializationInterval = 15;
            this.percentage = 0.2;
            this.Sequencer = 0;


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

            Console.WriteLine("ID: {0}", Util.ProcessID(name));
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

        public ManualResetEvent getMigration()
        {
            return this.migration;
        }

        public List<string> getMigratingList()
        {
            return this.migrating;
        }

        
        // Project API
        public Metadata Open(string clientName, string filename)
        {
            lock (typeof(MetadataServer))
            {
                return this.State.Open(this, clientName, filename);
            }
        }

        public void Close(string clientName, string filename)
        {
            lock (typeof(MetadataServer))
            {
                this.State.Close(this, clientName, filename);
            }
        }

        public Metadata Create(string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            Metadata meta;

            lock (typeof(MetadataServer))
            {
                meta = this.State.Create(this, clientName, filename, serversNumber, readQuorum, writeQuorum);
            }

            return meta;
        }

        public void Delete(string clientName, string filename)
        {
            lock (typeof(MetadataServer))
            {
                this.State.Delete(this, clientName, filename);
            }
        }
        
        // Puppet Master Commands
        public void Fail() {
         
            lock (this)
            {
                this.setStateFail();
                pingPrimaryReplicaTimer.Enabled = false;
                pingDataServersTimer.Enabled = false;
                serializationTimer.Enabled = false;
            }
            Console.WriteLine("On Failure!");
        }

        public void Recover() {

            lock (this)
            {
                this.setStateNormal();
            }
            Console.WriteLine("Uhf, recovered at last...");

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
                                serializationTimer.Enabled = false;
                                gettingLog.Reset();
                                string[] log = server.GetLog(this.Log.Index);
                                UpdateLog(log);
                                gettingLog.Set();
                                serializationTimer.Enabled = true;
                            }
                            else
                            {
                                // To prevent Lightning bolt failures
                                if (this.Replicas.ContainsKey(this.Primary))
                                {
                                    IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[this.Primary]);
                                    if (primary != null)
                                    {
                                        string[] log = server.GetLog(this.Log.Index);
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
                catch (SystemException)
                {
                }
            }

            foreach (string replica in this.Replicas.Keys)
            {
                if (!this.DeadReplicas.Contains(replica))
                {
                    try
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[replica]);
                        if (server != null)
                        {
                            server.Recovered(this.Name);
                        }
                    }
                    catch (ServerNotAvailableException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    catch (SystemException)
                    {
                    }
                }
            }
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
            if (DeadDataServers.ContainsKey(name))
            {
                this.DeadDataServers.Remove(name);
                this.LiveDataServers.Add(name, address);
            }
            else
            {
                if (!this.LiveDataServers.ContainsKey(name))
                {
                    this.LiveDataServers.Add(name, address);
                    this.DataServersInfo.Add(name, null);
                    this.ServersLoad.Add(name, 0);

                    string command = string.Format("REGISTER data {0} {1}", name, address);
                    this.Log.Append(command);
                    ThreadPool.QueueUserWorkItem(AppendToLog, command);
                }
            }

        }

        public void RegisterClient(string name, string address)
        {
            if (!this.Clients.ContainsKey(name))
            {
                Console.WriteLine("Client " + name + " : " + address);
                this.Clients.Add(name, address);
                string command = string.Format("REGISTER client {0} {1}", name, address);

                this.Log.Append(command);
                ThreadPool.QueueUserWorkItem(AppendToLog, command);
            }
        }


        public void RegisterMetadataServer(string name, string address)
        {
            this.State.RegisterMetadataServer(this, name, address);
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
                this.DataServersInfo[name] = server.Ping();
                Console.WriteLine(name + ": Alive");
                if (!this.LiveDataServers.ContainsKey(name))
                {
                    this.LiveDataServers.Add(name, address);
                    this.DeadDataServers.Remove(name);
                }
            }
            catch (ServerNotAvailableException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(name + ": Down");
                if (!this.DeadDataServers.ContainsKey(name))
                {
                    this.DeadDataServers.Add(name, address);
                    this.LiveDataServers.Remove(name);
                }
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine(name + ": Down");
                if (!this.DeadDataServers.ContainsKey(name))
                {
                    this.DeadDataServers.Add(name, address);
                    this.LiveDataServers.Remove(name);
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine(name + ": Down");
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
            }
            catch (ServerNotAvailableException e)
            {
                this.DeadReplicas.Add(replica);
                Console.WriteLine(e.Message);
                NextPrimaryReplica();
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("IOException");
                this.DeadReplicas.Add(replica);
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
            string filename = "BackupTemp.txt";
            string pathTemp = directory + @"\" + filename;
            string path = directory + @"\" + "Backup.txt";

            try
            {
                Util.SerializeObject(this, directory, filename);
                System.IO.File.Copy(pathTemp, path, true);
            }
            catch (Exception) { }
        }

        public void DeserializeServer()
        {
            string directory = Environment.CurrentDirectory + string.Format(@"\{0}", this.Name);
            string filename = "Backup.txt";
            string path = string.Format(@"{0}\{1}", directory, filename);

            if (System.IO.File.Exists(path))
            {
                Console.WriteLine("VOU DESERIALIZAR");
                MetadataServer oldServer = (MetadataServer)Util.DeserializeObject(this, directory, filename);
                this.Primary = oldServer.Primary;
                this.Replicas = oldServer.Replicas;
                this.Clients = oldServer.Clients;
                this.DeadReplicas = oldServer.DeadReplicas;
                this.LiveDataServers = oldServer.LiveDataServers;
                this.DeadDataServers = oldServer.DeadDataServers;
                this.ServersLoad = oldServer.ServersLoad;
                this.Files = oldServer.Files;
                this.OpenFiles = oldServer.OpenFiles;
                this.PendingFiles = oldServer.PendingFiles;
                this.DataServersInfo = oldServer.DataServersInfo;
                this.Log = oldServer.Log;
            }
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
            int id = Util.ProcessID(this.Name);
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
                            int r_id = Util.ProcessID(r);
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

            s += "Sequencer: " + this.Sequencer + "\r\n";


            return s;
        }

        public void UpdateFileMetada(string name, string address)
        {
            if (this.PendingFiles.Count > 0)
            {
                SerializableDictionary<string, int> updated = new SerializableDictionary<string, int>();
                string command = string.Format("UPDATE {0} {1}", name, address);

                foreach (string f in this.PendingFiles.Keys)
                {
                    Metadata meta = this.Files[f];
                    meta.AddDataServers(address);
                    this.ServersLoad[name]++;

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

                    string input = GetToken().ToString() + (char)0x7f + meta.Filename;

                    int n = this.PendingFiles[f] - 1;

                    if (n > 0)
                    {
                        updated.Add(f, n);
                    }

                }

                this.PendingFiles = new SerializableDictionary<string, int>(updated);

                this.Log.Append(command);
                ThreadPool.QueueUserWorkItem(AppendToLog, command);
            }
        }

        public long GetToken()
        {
            lock (this)
            {
                long s = ++this.Sequencer;
                string command = string.Format("TOKEN {0}", s);
                this.Log.Append(command);
                ThreadPool.QueueUserWorkItem(AppendToLog, command);
                return s;
            }
        }

        private void AppendToLog(object threadcontext)
        {
            string command = (string)threadcontext;
            foreach (string r in this.Replicas.Keys)
            {
                try
                {
                    IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), this.Replicas[r]);
                    if (replica != null)
                    {
                        replica.AppendToLog(command);
                    }
                }
                catch (ServerNotAvailableException) { }
                catch (System.IO.IOException) { }
                catch (System.Net.Sockets.SocketException) { }
            }
        }

        // Keeps it from getting garbage collected
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

            int origWidth = Console.WindowWidth;
            int origHeight = Console.WindowHeight;

            Console.SetWindowSize(origWidth, origHeight / 2);

            Console.ReadLine();
        }


        public void AppendToLog(string command)
        {
            gettingLog.WaitOne();
            lock (typeof(MetadataServer))
            {
                this.State.AppendToLog(this, command);
            }
        }


        public void UpdateLog(string[] log)
        {
                foreach (string command in log)
                {
                    AppendToLog(command);
                }
        }

        public string[] GetLog(int logIndex)
        {
            string[] commands = this.Log.Read(logIndex + 1);           
            return commands;
        }

        public void EnablePrimaryTimers()
        {
            pingDataServersTimer.Enabled = true;
            pingPrimaryReplicaTimer.Enabled = false;
        }

        public void EnableReplicaTimers()
        {
            pingPrimaryReplicaTimer.Enabled = true;
            pingDataServersTimer.Enabled = false;
        }
    }
}
