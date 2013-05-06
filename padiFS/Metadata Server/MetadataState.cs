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
            if(md.getMigratingList().Contains(filename))
                md.getMigration().WaitOne();

            // If already opened by one client
            if (md.OpenFiles.ContainsKey(filename))
            {
                List<string> clientsList = md.OpenFiles[filename];
                if (clientsList.Contains(clientName))
                {
                    throw new FileIsOpenedException("File already open.");
                }

                md.OpenFiles[filename].Add(clientName);
            }
            else
            {
                if (!md.Files.ContainsKey(filename))
                {
                    throw new FileNotFoundException("File does not exist.");
                }

                List<string> clientsList = new List<string>();
                clientsList.Add(clientName);
                md.OpenFiles.Add(filename, clientsList);

            }

            List<object> context = new List<object>();
            string command = string.Format("OPEN {0} {1}", clientName, filename);
            context.Add(md);
            context.Add(command);
            ThreadPool.QueueUserWorkItem(AppendToLog, context);

            md.Log.Append(command);

            return md.Files[filename];
        }

        public override void Close(MetadataServer md, string clientName, string filename)
        {
            if (!md.Files.ContainsKey(filename))
            {
                throw new FileNotFoundException("File does not exist.");
            }

            if (!md.OpenFiles.ContainsKey(filename))
            {
                throw new FileAlreadyClosedException("File already closed.");
            }

            List<string> clientsList = md.OpenFiles[filename];

            if (!clientsList.Contains(clientName))
            {
                throw new FileNotOpenException("Client " + clientName + " did not open the file previously.");
            }

            clientsList.Remove(clientName);
            if (clientsList.Count == 0)
            {
                md.OpenFiles.Remove(filename);
            }

            List<object> context = new List<object>();
            string command = string.Format("CLOSE {0} {1}", clientName, filename);
            context.Add(md);
            context.Add(command);
            ThreadPool.QueueUserWorkItem(AppendToLog, context);

            md.Log.Append(command);
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
            LoadBalanceServers(md);
            Metadata meta = new Metadata(filename, serversNumber, readQuorum, writeQuorum, servers);
            List<string> clientsList = new List<string>();
            clientsList.Add(clientName);
            md.Files.Add(filename, meta);
            md.OpenFiles.Add(filename, clientsList);
            List<object> context = new List<object>();
            context.Add(md);
            string command = string.Format("CREATE {0} {1} {2} {3} {4}", clientName, filename, serversNumber, readQuorum, writeQuorum);
            foreach (string c in chosen)
            {
                command += string.Format(" {0}", c);
            }
            context.Add(command);
            ThreadPool.QueueUserWorkItem(AppendToLog, context);

            md.Log.Append(command);

            return meta;
        }

        public override void Delete(MetadataServer md, string clientName, string filename)
        {
            if (!md.Files.ContainsKey(filename))
            {
                throw new FileNotFoundException("File does not exist.");
            }

            md.Files.Remove(filename);
            md.OpenFiles.Remove(filename);
            List<object> context = new List<object>();
            string command = string.Format("DELETE {0} {1}", clientName, filename);
            context.Add(md);
            context.Add(command);
            ThreadPool.QueueUserWorkItem(AppendToLog, context);
            Console.WriteLine("File " + filename + " deleted");

            md.Log.Append(command);
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
                md.Log.Append(string.Format("REGISTER metadata {0} {1}", name, address));

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

            Thread.Sleep(5000);
            tryMigrate(md);
        }


        // HUGE REFACTORING NEEDED
        private void tryMigrate(MetadataServer md)
        {
            Dictionary<string, int> averageAccesses = new Dictionary<string, int>();
            int averageAll = 0;

            foreach (string di in md.DataServersInfo.Keys)
            {
                int dataAccess = 0;
                if (md.DataServersInfo[di] != null)
                {
                    foreach (string file in md.DataServersInfo[di].GetNumberAccesses().Keys)
                    {
                        dataAccess += md.DataServersInfo[di].GetNumberAccesses()[file];
                    }
                    if (md.DataServersInfo[di].GetNumberAccesses().Count != 0)
                    {
                        averageAccesses.Add(di, (int)(dataAccess / md.DataServersInfo[di].GetNumberAccesses().Count));
                    }
                    else
                    {
                        averageAccesses.Add(di, 0);
                    }
                }
            }

            int aux = 0;
            foreach (string di in averageAccesses.Keys)
            {
                aux += averageAccesses[di];
            }
            averageAll = (int)(aux / averageAccesses.Count);

            int min = averageAll - Util.IntervalAccesses(md.Percentage, averageAll);
            int max = averageAll + Util.IntervalAccesses(md.Percentage, averageAll);

            List<string> OverloadServers = new List<string>();
            List<string> UnderloadServers = new List<string>();

            foreach (string s in averageAccesses.Keys)
            {
                if (averageAccesses[s] > max)
                {
                    OverloadServers.Add(s);
                }
                else if (averageAccesses[s] < min)
                {
                    UnderloadServers.Add(s);
                }
            }

            if (OverloadServers.Count != 0 && UnderloadServers.Count != 0)
            {
                string mostOverloadedServer = "";
                string mostUnderloadedServer = "";

                long maxFiles = Int64.MinValue;
                long minFiles = Int64.MaxValue;

                foreach (string s in OverloadServers)
                {
                    if (md.DataServersInfo[s].GetTotalAccesses() > maxFiles && md.DataServersInfo[s].GetNumberAccesses().Count > 1)
                    {
                        maxFiles = md.DataServersInfo[s].GetTotalAccesses();
                        mostOverloadedServer = s;
                    }
                }

                string mostAccessedfile = "";
                long maxAccesses = Int64.MinValue;
                foreach (string f in md.DataServersInfo[mostOverloadedServer].GetNumberAccesses().Keys)
                {
                    if (md.DataServersInfo[mostOverloadedServer].GetNumberAccesses()[f] > maxAccesses)
                    {
                        maxAccesses = md.DataServersInfo[mostOverloadedServer].GetNumberAccesses()[f];
                        mostAccessedfile = f;
                    }
                }

                while (UnderloadServers.Count != 0)
                {
                    mostUnderloadedServer = "";
                    minFiles = Int64.MaxValue;
                    foreach (string s in UnderloadServers)
                    {
                        if (md.DataServersInfo[s].GetTotalAccesses() < minFiles)
                        {
                            minFiles = md.DataServersInfo[s].GetTotalAccesses();
                            mostUnderloadedServer = s;
                        }
                    }

                    string secondMostAccessedfile;
                    maxAccesses = Int64.MinValue;

                    List<string> previousFiles = new List<string>();
                    int i = md.DataServersInfo[mostOverloadedServer].GetNumberAccesses().Count;

                    while (i != 0)
                    {
                        secondMostAccessedfile = null;
                        foreach (string f in md.DataServersInfo[mostOverloadedServer].GetNumberAccesses().Keys)
                        {
                            if (f != mostAccessedfile && !previousFiles.Contains(f))
                            {
                                if (md.DataServersInfo[mostOverloadedServer].GetNumberAccesses()[f] > maxAccesses &&
                                    (!md.OpenFiles.ContainsKey(f)))
                                {
                                    maxAccesses = md.DataServersInfo[mostOverloadedServer].GetNumberAccesses()[f];
                                    secondMostAccessedfile = f;
                                }
                            }
                        }

                        if (secondMostAccessedfile != null)
                        {
                            if (!md.DataServersInfo[mostUnderloadedServer].GetNumberAccesses().ContainsKey(secondMostAccessedfile))
                            {
                                Console.WriteLine("Migration");
                                md.getMigratingList().Add(secondMostAccessedfile);
                                md.getMigration().Reset();

                                Metadata meta = md.Files[secondMostAccessedfile];
                                string readServer = md.LiveDataServers[mostOverloadedServer];
                                string writeServer = md.LiveDataServers[mostUnderloadedServer];
                                IDataServer readDataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), readServer);
                                IDataServer writeDataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), writeServer);

                                File file = readDataServer.Read(secondMostAccessedfile, "default");

                                string toWrite = Util.ConvertByteArrayToString(file.Content);
                                byte[] content = Util.ConvertStringToByteArray(file.Version.ToString("o") + (char)0x7f + toWrite);

                                writeDataServer.Write(secondMostAccessedfile, content);
                                readDataServer.RemoveFromDataInfo(secondMostAccessedfile);

                                string command = string.Format("UPDATE {0} {1}", writeServer, secondMostAccessedfile);
                                meta.AddDataServers(writeServer);
                                meta.DataServers.Remove(readServer);
                                md.ServersLoad[mostOverloadedServer]--;

                                md.Log.Append(command);

                                foreach (string s in md.Replicas.Keys)
                                {
                                    IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), md.Replicas[s]);

                                    if (replica != null)
                                    {
                                        replica.AppendToLog(command);
                                    }
                                }
                                md.getMigration().Set();
                                md.getMigratingList().Remove(secondMostAccessedfile);
                                return;
                            }
                            else
                            {
                                previousFiles.Add(secondMostAccessedfile);
                                i--;
                            }
                        }
                        else
                        {
                            UnderloadServers.Remove(mostUnderloadedServer);
                            break;
                        }
                    }
                }
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

        private void LoadBalanceServers(MetadataServer md)
        {
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


        //private void UpdateReplicas(object threadcontext)
        //{
        //    MetadataServer md = (MetadataServer)threadcontext;
        //    foreach (string r in md.Replicas.Keys)
        //    {
        //        try
        //        {
        //            IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), md.Replicas[r]);
        //            if (replica != null)
        //            {
        //                replica.Ping();
        //                MetadataInfo info = new MetadataInfo(md.Primary, md.Address, md.Replicas, md.LiveDataServers, md.DeadDataServers, md.ServersLoad, md.Files, md.OpenFiles);
        //                replica.UpdateReplica(info);
        //            }
        //        }
        //        catch (ServerNotAvailableException) { }
        //    }
        //}

        private void AppendToLog(object threadcontext)
        {
            List<object> context = (List<object>)threadcontext;
            MetadataServer md = (MetadataServer)context[0];
            string command = (string)context[1];

            foreach (string r in md.Replicas.Keys)
            {
                try
                {
                    IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), md.Replicas[r]);
                    if (replica != null)
                    {
                        replica.Ping();
                        replica.AppendToLog(command);
                    }
                }
                catch (ServerNotAvailableException) { }
                catch (System.IO.IOException) { }
                catch (System.Net.Sockets.SocketException) { }
            }
        }
    }
}
