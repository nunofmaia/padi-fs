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
        public abstract bool Ping();
        public abstract void RegisterMetadataServer(MetadataServer md, string name, string address);
        public abstract void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e);
        public abstract void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e);
    }

    class FailedState : MetadataState
    {
        public override Metadata Open(MetadataServer md, string clientName, string filename)
        {
            throw new ServerNotAvailableException("The server is not available and can't open the file.");
        }
        public override void Close(MetadataServer md, string clientName, string filename)
        {
            throw new ServerNotAvailableException("The server is not available and can't close the file.");
        }
        public override Metadata Create(MetadataServer md, string clientName,
                                string filename,
                                int serversNumber,
                                int readQuorum,
                                int writeQuorum)
        {
            throw new ServerNotAvailableException("The server is not available and can't create the file.");
        }

        public override void Delete(MetadataServer md, string clientName, string filename)
        {
            throw new ServerNotAvailableException("The server is not available and can't delete the file.");
        }

        public override bool Ping()
        {
            throw new ServerNotAvailableException("The server is not available.");
        }

        public override void RegisterMetadataServer(MetadataServer md, string name, string address)
        {
            throw new ServerNotAvailableException("The server is not available and can't register a new server.");
        }

        public override void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e) 
        {
            throw new ServerNotAvailableException("The server is not available and can't ping other replicas.");
        }

        public override void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e)
        {
            throw new ServerNotAvailableException("The server is not available and can't ping the primary replica.");
        }
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
                if (clientsList.Contains(clientName))
                {
                    throw new FileIsOpenedException("File already open.");
                }

                md.TempOpenFiles[filename].Add(clientName);
                ThreadPool.QueueUserWorkItem(UpdateReplicas, md);
                return md.Files[filename];
                //}
                //else
                //{
                //    Console.WriteLine("File already open. It's ok!");
                //    return null;
                //}
            }
            else
            {
                if (!md.Files.ContainsKey(filename))
                {
                    throw new FileNotFoundException("File does not exist.");
                }

                List<string> clientsList = new List<string>();
                clientsList.Add(clientName);
                md.TempOpenFiles.Add(filename, clientsList);
                ThreadPool.QueueUserWorkItem(UpdateReplicas, md);
                return md.Files[filename];
                //}
                //else
                //{
                //    Console.WriteLine("File does not exists.");
                //    return null;
                //}
            }
        }

        public override void Close(MetadataServer md, string clientName, string filename)
        {
            if (!md.Files.ContainsKey(filename))
            {
                throw new FileNotFoundException("File does not exist.");
            }

            if (!md.TempOpenFiles.ContainsKey(filename))
            {
                throw new FileAlreadyClosedException("File already closed.");
            }

            List<string> clientsList = md.TempOpenFiles[filename];

            if (!clientsList.Contains(clientName))
            {
                throw new FileNotOpenException("Client " + clientName + " did not open the file previously.");
            }

            clientsList.Remove(clientName);
            if (clientsList.Count == 0)
            {
                md.TempOpenFiles.Remove(filename);
            }
            ThreadPool.QueueUserWorkItem(UpdateReplicas, md);
                //}
                //else
                //{
                //    Console.WriteLine("This client " + clientName + " did not open this file " + filename + ".");
                //}
            //}
            //else
            //{
            //    Console.WriteLine("File " + filename + " is not open by any client.");
            //}
        }

        public override Metadata Create(MetadataServer md, string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum)
        {
            if (md.Files.ContainsKey(filename))
            {
                throw new FileAlreadyExists("File already exists.");
            }

            if (md.LiveDataServers.Count < serversNumber)
            {
                if (!md.PendingFiles.ContainsKey(filename))
                {
                    md.PendingFiles.Add(filename, serversNumber - md.LiveDataServers.Count);
                }
            }

            List<string> servers = new List<string>();
            List<string> chosen = ChooseBestServers(serversNumber, md);

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
            ThreadPool.QueueUserWorkItem(LoadBalanceServers, md);
            Metadata meta = new Metadata(filename, serversNumber, readQuorum, writeQuorum, servers);
            List<string> clientsList = new List<string>();
            clientsList.Add(clientName);
            md.Files.Add(filename, meta);
            md.TempOpenFiles.Add(filename, clientsList);
            // Update other replicas. CHANGE THIS IN THE FUTURE
            ThreadPool.QueueUserWorkItem(UpdateReplicas, md);

            return meta;
                
            //}
            //else
            //{
            //    // THROW EXCEPTION
            //    Console.WriteLine("File already exists.");
            //}
            //return null;
        }

        public override void Delete(MetadataServer md, string clientName, string filename)
        {
            if (!md.Files.ContainsKey(filename))
            {
                throw new FileNotFoundException("File does not exist.");
            }

            if (md.TempOpenFiles.ContainsKey(filename))
            {
                throw new FileIsOpenedException("Can't delete an open file.");
            }

            md.Files.Remove(filename);
            // Update other replicas. CHANGE THIS IN THE FUTURE
            ThreadPool.QueueUserWorkItem(UpdateReplicas, md);
            Console.WriteLine("File " + filename + " deleted");
                //}
                //else
                //{
                //    // THROW EXCEPTION
                //    Console.WriteLine("File " + filename + " is opened.");
                //}
            //}
            //else
            //{
            //    // THROW EXCEPTION
            //    Console.WriteLine("File " + filename + " does not exists.");
            //}
        }

        public override bool Ping()
        {
            Console.WriteLine("I'm Alive");
            return true;
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

        private void LoadBalanceServers(object threadcontext)
        {
            MetadataServer md = (MetadataServer)threadcontext; 
            md.ServersLoad = Util.SortServerLoad(md.ServersLoad);
        }

        private List<string> ChooseBestServers(int serversNumber, MetadataServer md)
        {
            List<string> chosen = new List<string>();
            int chosen_counter = 0;
            foreach (string s in md.ServersLoad.Keys)
            {
                if (md.LiveDataServers.ContainsKey(s))
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
            MetadataServer md = (MetadataServer)threadcontext; 
            foreach (string r in md.Replicas.Keys)
            {
                IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), md.Replicas[r]);
                if (replica != null)
                {
                    MetadataInfo info = new MetadataInfo(md.Primary, md.Address, md.Replicas, md.LiveDataServers, md.DeadDataServers, md.ServersLoad, md.Files, md.TempOpenFiles);
                    replica.UpdateReplica(info);
                }
            }
        }
    
    
    }
}
