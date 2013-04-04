using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace padiFS
{
    abstract class MetadataState
    {
        public abstract Metadata Open(MetadataServer md, string clientName, string filename);
        public abstract void Close(MetadataServer md, string clientName, string filename);
        public abstract Metadata Create(MetadataServer md, string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum);
        public abstract void Delete(MetadataServer md, string clientName, string filename);
        public abstract int Ping();
        public abstract void RegisterMetadataServer(MetadataServer md, string name, string address);
        public abstract void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e);
        public abstract void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e);
    }

    class FailedState : MetadataState
    {
        public override Metadata Open(MetadataServer md, string clientName, string filename)
        { return null; }
        public override void Close(MetadataServer md, string clientName, string filename) { }
        public override Metadata Create(MetadataServer md, string clientName,
                                string filename,
                                int serversNumber,
                                int readQuorum,
                                int writeQuorum)
        {
            return null;
        }

        public override void Delete(MetadataServer md, string clientName, string filename) { }

        public override int Ping()
        {
            throw new ServerNotAvailableException("The server is on fail mode.");
        }

        public override void RegisterMetadataServer(MetadataServer md, string name, string address) { }

        public override void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e) { }

        public override void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e) { }
    }

    class NormalState : MetadataState
    {

        // Project API
        public override Metadata Open(MetadataServer md, string clientName, string filename)
        {
            // If already opened by one client
            if (md.TempOpenFiles.ContainsKey(filename))
            {
                List<string> clientsList = md.TempOpenFiles[filename];
                if (!clientsList.Contains(clientName))
                {
                    md.TempOpenFiles[filename].Add(clientName);
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
                    return md.Files[filename];
                }
                else
                {
                    Console.WriteLine("File already open. It's ok!");
                    return null;
                }
            }
            else
            {
                if (md.Files.ContainsKey(filename))
                {
                    List<string> clientsList = new List<string>();
                    clientsList.Add(clientName);
                    md.TempOpenFiles.Add(filename, clientsList);
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
                    return md.Files[filename];
                }
                else
                {
                    Console.WriteLine("File does not exists.");
                    return null;
                }
            }
        }

        public override void Close(MetadataServer md, string clientName, string filename)
        {
            if (md.TempOpenFiles.ContainsKey(filename))
            {
                List<string> clientsList = md.TempOpenFiles[filename];

                if (clientsList.Contains(clientName))
                {
                    clientsList.Remove(clientName);
                    if (clientsList.Count == 0)
                    {
                        md.TempOpenFiles.Remove(filename);
                    }
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
                }
                else
                {
                    Console.WriteLine("This client " + clientName + " did not open this file " + filename + ".");
                }
            }
            else
            {
                Console.WriteLine("File " + filename + " is not open by any client.");
            }
        }

        public override Metadata Create(MetadataServer md, string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            if (!md.Files.ContainsKey(filename))
            {
                if (md.LiveDataServers.Count >= serversNumber)
                {
                    List<string> servers = new List<string>();
                    List<string> chosen = md.ChooseBestServers(serversNumber);

                    // Before sending the requests, a time stamp is added to the filename
                    string f = DateTime.Now.ToString("o") + (char)0x7f + filename;
                    foreach (string v in chosen)
                    {
                        List<string> arguments = new List<string>();
                        arguments.Add(md.LiveDataServers[v]);
                        arguments.Add(f);
                        servers.Add(md.LiveDataServers[v]);
                        ThreadPool.QueueUserWorkItem(CreateCallback, arguments);
                        md.ServersLoad[v]++;
                    }
                    ThreadPool.QueueUserWorkItem(md.LoadBalanceServers, null);
                    Metadata meta = new Metadata(filename, serversNumber, readQuorum, writeQuorum, servers);
                    List<string> clientsList = new List<string>();
                    clientsList.Add(clientName);
                    md.Files.Add(filename, meta);
                    md.TempOpenFiles.Add(filename, clientsList);
                    // Update other replicas. CHANGE THIS IN THE FUTURE
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
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
            return null;
        }

        public override void Delete(MetadataServer md, string clientName, string filename)
        {
            if (md.Files.ContainsKey(filename))
            {
                if (!md.TempOpenFiles.ContainsKey(filename))
                {
                    md.Files.Remove(filename);
                    // Update other replicas. CHANGE THIS IN THE FUTURE
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
                    Console.WriteLine("File " + filename + " deleted");
                }
                else
                {
                    Console.WriteLine("File " + filename + " is opened.");
                }
            }
            else
            {
                Console.WriteLine("File " + filename + " does not exists.");
            }
        }

        public override int Ping()
        {
            Console.WriteLine("I'm Alive");
            return 1;
        }

        public override void RegisterMetadataServer(MetadataServer md, string name, string address)
        {
            // If the server doesn't have the new metadata registered,
            // registers it and introduces to it "Hi, I'm Iurie's metadata server"
            if (!md.Replicas.ContainsKey(name))
            {
                Console.WriteLine("Metadata Server " + name + " : " + address);
                md.Replicas.Add(name, address);

                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                if (server != null)
                {
                    try
                    {
                        server.RegisterMetadataServer(md.Name, "tcp://localhost:" + md.Port + "/" + md.Name);
                    }
                    catch (System.Net.Sockets.SocketException) { }
                    // Ignore it
                }
            }
        }

        public override void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e)
        {
            foreach (string key in md.LiveDataServers.Keys)
            {
                List<string> dataservers = new List<string>();
                dataservers.Add(key);
                dataservers.Add(md.LiveDataServers[key]);
                ThreadPool.QueueUserWorkItem(md.PingDataServer, dataservers);
            }

            foreach (string key in md.DeadDataServers.Keys)
            {
                List<string> dataservers = new List<string>();
                dataservers.Add(key);
                dataservers.Add(md.DeadDataServers[key]);
                ThreadPool.QueueUserWorkItem(md.PingDataServer, dataservers);
            }
        }
        public override void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e)
        {
            if (md.Primary != null)
            {
                if (md.Name != md.Primary)
                {
                    ThreadPool.QueueUserWorkItem(md.PingReplica, md.Primary);
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
    }
}
