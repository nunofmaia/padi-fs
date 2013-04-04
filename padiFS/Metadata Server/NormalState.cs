using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace padiFS
{
    class NormalState : MetadataState
    {

        // Project API
        public override Metadata Open(MetadataServer md, string clientName, string filename)
        {
            if (!md.OpenFiles.ContainsKey(filename))
            {
                Console.WriteLine("Before: " + md.OpenFiles.ContainsKey(filename));
                md.OpenFiles.Add(filename, md.Files[filename]);
                Console.WriteLine("After: " + md.OpenFiles.ContainsKey(filename));
                // Update other replicas. CHANGE THIS IN THE FUTURE
                ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
            }
            else
            {
                Console.WriteLine("File already open. It's ok!");
            }

            return md.Files[filename];
        }

        public override void Close(MetadataServer md, string clientName, string filename)
        {
            if (md.Files.ContainsKey(filename))
            {
                if (md.OpenFiles.ContainsKey(filename))
                {
                    Console.WriteLine("Before: " + md.OpenFiles.ContainsKey(filename));
                    md.OpenFiles.Remove(filename);
                    Console.WriteLine("After " + md.OpenFiles.ContainsKey(filename));
                    // Update other replicas. CHANGE THIS IN THE FUTURE
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
                }
                else
                {
                    Console.WriteLine("File already closed.");
                }
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
                    md.Files.Add(filename, meta);
                    md.OpenFiles.Add(filename, meta);
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
                if (!md.OpenFiles.ContainsKey(filename))
                {
                    md.Files.Remove(filename);
                    // Update other replicas. CHANGE THIS IN THE FUTURE
                    ThreadPool.QueueUserWorkItem(md.UpdateReplicas, null);
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
