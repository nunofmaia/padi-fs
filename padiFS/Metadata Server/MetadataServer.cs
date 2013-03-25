﻿using System;
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
        private string primary;
        private Dictionary<string, string> replicas;
        private List<string> deadReplicas;
        private Dictionary<string, string> liveDataServers;
        private Dictionary<string, string> deadDataServers;
        private Dictionary<string, int> serversLoad;
        private Dictionary<string, Metadata> files;
        private Dictionary<string, Metadata> openFiles;
        private System.Timers.Timer pingDataServersTimer;
        private System.Timers.Timer pingPrimaryReplicaTimer;
        private bool onFailure = false;

        public MetadataServer(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);
            this.primary = null;
            this.replicas = new Dictionary<string, string>();
            this.deadReplicas = new List<string>();
            this.liveDataServers = new Dictionary<string, string>();
            this.deadDataServers = new Dictionary<string, string>();
            this.serversLoad = new Dictionary<string, int>();
            this.files = new Dictionary<string, Metadata>();
            this.openFiles = new Dictionary<string, Metadata>();
            this.pingDataServersTimer = new System.Timers.Timer();
            pingDataServersTimer.Elapsed += new System.Timers.ElapsedEventHandler(pingDataServers);
            pingDataServersTimer.Interval = 1000 * pingInterval;
            this.pingPrimaryReplicaTimer = new System.Timers.Timer();
            pingPrimaryReplicaTimer.Elapsed += new System.Timers.ElapsedEventHandler(PingPrimaryReplica);
            pingPrimaryReplicaTimer.Interval = 1000 * pingInterval;

            Console.WriteLine("ID: {0}", Util.MetadataServerId(name));
        }

        // Project API
        public Metadata Open(string filename)
        {
            if (!onFailure)
            {
                if (files.ContainsKey(filename))
                {
                    if (!openFiles.ContainsKey(filename))
                    {
                        openFiles.Add(filename, files[filename]);
                        // Update other replicas. CHANGE THIS IN THE FUTURE
                        ThreadPool.QueueUserWorkItem(UpdateReplicas, null);
                    }
                    else
                    {
                        Console.WriteLine("File already open. It's ok!");
                    }

                    return files[filename];
                }
            }
            return null;
        }
        public void Close(string filename)
        {
            if (!onFailure)
            {
                if (files.ContainsKey(filename))
                {
                    if (openFiles.ContainsKey(filename))
                    {
                        openFiles.Remove(filename);
                        // Update other replicas. CHANGE THIS IN THE FUTURE
                        ThreadPool.QueueUserWorkItem(UpdateReplicas, null);
                    }
                    else
                    {
                        Console.WriteLine("File already closed.");
                    }
                }
            }
        }

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

        private void LoadBalanceServers(object threadcontext)
        {
            serversLoad = Util.SortServerLoad(serversLoad);
        }

        public Metadata Create(string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            if (!onFailure)
            {
                if (!files.ContainsKey(filename))
                {
                    if (liveDataServers.Count >= serversNumber)
                    {
                        List<string> servers = new List<string>();
                        List<string> chosen = ChooseBestServers(serversNumber);
                        foreach (string v in chosen)
                        {
                            List<string> arguments = new List<string>();
                            arguments.Add(liveDataServers[v]);
                            arguments.Add(filename);
                            servers.Add(liveDataServers[v]);
                            ThreadPool.QueueUserWorkItem(CreateCallback, arguments);
                            serversLoad[v]++;
                        }
                        ThreadPool.QueueUserWorkItem(LoadBalanceServers, null);
                        Metadata meta = new Metadata(filename, serversNumber, readQuorum, writeQuorum, servers);
                        files.Add(filename, meta);
                        openFiles.Add(filename, meta);
                        // Update other replicas. CHANGE THIS IN THE FUTURE
                        ThreadPool.QueueUserWorkItem(UpdateReplicas, null);
                        return meta;
                    }
                    else
                    {
                        Console.WriteLine("Not enough servers.");
                    }
                }
                else
                {
                    Console.WriteLine("File already exists.");
                }
            }
            return null;
        }

        private List<string> ChooseBestServers(int serversNumber)
        {
            List<string> chosen = new List<string>();
            int chosen_counter = 0;
            foreach (string s in serversLoad.Keys)
            {
                if (liveDataServers.ContainsKey(s))
                {
                    chosen.Add(s);
                    chosen_counter++;
                }

                if (chosen_counter == serversNumber)
                {
                    break;
                }
            }
            return chosen;
        }

        private void UpdateReplicas(object threadcontext)
        {
            foreach (string r in replicas.Keys)
            {
                IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[r]);
                if (replica != null)
                {
                    MetadataInfo info = new MetadataInfo(primary, liveDataServers, deadDataServers, serversLoad, files, openFiles);
                    replica.UpdateReplica(info);
                }
            }
        }

        public void Delete(string filename) {
            if (!onFailure)
            {
                if (files.ContainsKey(filename))
                {
                    if (!openFiles.ContainsKey(filename))
                    {
                        files.Remove(filename);
                        // Update other replicas. CHANGE THIS IN THE FUTURE
                        ThreadPool.QueueUserWorkItem(UpdateReplicas, null);
                        Console.WriteLine("File " + filename + " deleted");
                    }
                    else
                    {
                        Console.WriteLine("File is opened.");
                    }
                }
                else
                {
                    Console.WriteLine("File does not exists.");
                }
            }
        }

        // Puppet Master Commands
        public void Fail() {
            onFailure = true;
            Console.WriteLine("On Failure!");
        }
        public void Recover() {
            onFailure = false;
            Console.WriteLine("Uhf, recovered at last...");
        }
        
        
        // Auxiliar API
        public void RegisterDataServer(string name, string address)
        {
            Console.WriteLine("Data Server " + name + " : " + address);
            liveDataServers.Add(name, address);
            serversLoad.Add(name, 0);
        }

        public void RegisterMetadataServer(string name, string address)
        {
            // If the server doesn't have the new metadata registered,
            // registers it and introduces to it "Hi, I'm Iurie's metadata server"
            if (!onFailure)
            {
                if (!replicas.ContainsKey(name))
                {
                    Console.WriteLine("Metadata Server " + name + " : " + address);
                    replicas.Add(name, address);

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
        }

        public void UpdateReplica(MetadataInfo info)
        {
            SetPrimary(info.Primary);
            this.liveDataServers = info.LiveDataServers;
            this.deadDataServers = info.DeadDataServers;
            this.serversLoad = info.ServersLoad;
            this.files = info.Files;
            this.openFiles = info.OpenFiles;

            Console.WriteLine("Updated metadata info.");
        }

        public MetadataInfo GetMetadataInfo()
        {
            return new MetadataInfo(primary, liveDataServers, deadDataServers, serversLoad, files, openFiles);
        }


        private void PingDataServer(object threadContext)
        {
            List<string> dataservers = (List<string>)threadContext;
            string name = dataservers[0];
            string address = dataservers[1];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

            try
            {
                if (server.ping() == 1)
                {
                    Console.WriteLine(name + ": VIVO");
                    if (!liveDataServers.ContainsKey(name))
                    {
                        liveDataServers.Add(name, address);
                        deadDataServers.Remove(name);
                    }
                }
                else
                {
                    Console.WriteLine(name + ": MORTO");
                    if (!deadDataServers.ContainsKey(name))
                    {
                        deadDataServers.Add(name, address);
                        liveDataServers.Remove(name);
                    }
                }
            }
            catch (System.SystemException)
            {
                Console.WriteLine(name + ": MORTO");
                if (!deadDataServers.ContainsKey(name))
                {
                    deadDataServers.Add(name, address);
                    liveDataServers.Remove(name);
                }
            }
        }


        private void pingDataServers(object source, ElapsedEventArgs e)
        {
            if (!onFailure)
            {
                foreach (string key in liveDataServers.Keys)
                {
                    List<string> dataservers = new List<string>();
                    dataservers.Add(key);
                    dataservers.Add(liveDataServers[key]);
                    ThreadPool.QueueUserWorkItem(PingDataServer, dataservers);
                }

                foreach (string key in deadDataServers.Keys)
                {
                    List<string> dataservers = new List<string>();
                    dataservers.Add(key);
                    dataservers.Add(deadDataServers[key]);
                    ThreadPool.QueueUserWorkItem(PingDataServer, dataservers);
                }
            }
        }

        private void PingReplica(object threadContext)
        {
            string replica = (string)threadContext;
            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), replicas[replica]);

            try
            {
                if (server.Ping() == 1)
                {
                    Console.WriteLine(replica + ": VIVO");
                }
                else
                {
                    deadReplicas.Add(replica);
                    Console.WriteLine("ELSE: esta é a primary: {0}", replica);
                    Console.WriteLine(replica + ": MORTO");
                    NextPrimaryReplica();
                }
            }
            catch (System.SystemException)
            {
                deadReplicas.Add(replica);
                Console.WriteLine("EXCEP: esta é a primary: {0}", replica);
                Console.WriteLine(replica + ": MORTO");
                NextPrimaryReplica();
            }
        }


        private void PingPrimaryReplica(object source, ElapsedEventArgs e)
        {
            if (primary != null && !onFailure)
            {
                if (name != primary)
                {
                    ThreadPool.QueueUserWorkItem(PingReplica, primary);
                }
            }
        }

        public int Ping()
        {
            if (!onFailure)
            {
                Console.WriteLine("I'm Alive");
                return 1;
            }

            return 0;
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
                if (r != primary && !deadReplicas.Contains(r))
                {
                    int r_id = Util.MetadataServerId(r);
                    if (r_id < id)
                    {
                        id = r_id;
                        replica = r;
                    }
                }
            }

            SetPrimary(replica);
        }

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            MetadataServer ms = new MetadataServer(arguments[0], arguments[1]);
            Console.Title = "Iurie's Metadata Server: " + ms.name;
            //if (ms.name == ms.primary)
            //{
            //    ms.pingDataServersTimer.Enabled = true;
            //}
            //else
            //{
            //    ms.pingPrimaryReplicaTimer.Enabled = true;
            //}
            //ms.pingPrimaryReplicaTimer.Enabled = true;
            // Ficar esperar pedidos de Iurie
            TcpChannel channel = new TcpChannel(ms.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(ms, ms.name, typeof(MetadataServer));
            
            Console.ReadLine();
        }
    }
}
