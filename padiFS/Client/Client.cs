﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Threading;
using System.Collections.Concurrent;

namespace padiFS
{
    public class Client : MarshalByRefObject, IClient
    {
        private string name;
        private int port;
        private int requestInterval = 5;
        private Bridge bridge;
        private Dictionary<string, Metadata> myFiles;
        private Dictionary<string, Metadata> openFiles;
        private Dictionary<string, File> historic;
        private ConcurrentBag<File> readFiles;
        private ConcurrentBag<int> writeFiles;
        private byte[][] stringRegister;
        private Metadata[] fileRegister;

        public Client(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);
            this.bridge = new Bridge();
            this.myFiles = new Dictionary<string, Metadata>();
            this.openFiles = new Dictionary<string, Metadata>(10);
            this.historic = new Dictionary<string, File>();
            this.stringRegister = new byte[10][];
            this.fileRegister = new Metadata[10];
        }

        public void Create(string filename, int nServers, int rQuorum, int wQuorum)
        {
            Metadata meta = bridge.Create(filename, nServers, rQuorum, wQuorum);

            if (meta != null)
            {
                myFiles.Add(filename, meta);
                openFiles.Add(filename, meta);
                Console.WriteLine("Create file " + filename);
            }
            else
            {
                Console.WriteLine("Could not create the file " + filename);
            }
        }

        public void Open(string filename)
        {
            Metadata meta = bridge.Open(filename);

            if (meta != null)
            {
                if (!openFiles.ContainsKey(filename))
                {
                    openFiles.Add(filename, meta);
                }
                Console.WriteLine("Open file " + filename);
            }
            else
            {
                Console.WriteLine("Something wrong happened.");
            }

        }

        private void ReadCallback(object threadcontext)
        {
            List<object> args = (List<object>)threadcontext;
            string server = (string)args[0];
            string filename = (string)args[1];
            string semantic = (string)args[2];
            File file = null;

            IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), server);
            if (dataServer != null)
            {
                try
                {
                    while (file == null) {
                        file = dataServer.Read(filename, semantic);
                    
                        if (file != null)
                        {
                            readFiles.Add(file);
                            break;
                        }
                        Thread.Sleep(1000* requestInterval);
                    }                 
                }
                catch (SystemException)
                {
                }
            }
        }

        private void ExecuteRead(string filename, string semantic)
        {
            if (openFiles.ContainsKey(filename))
            {
                Metadata file = openFiles[filename];
                List<string> servers = file.DataServers;
                int readQuorum = file.ReadQuorum;
                readFiles = new ConcurrentBag<File>();

                // Call all the data servers that have the file and wait for a majority
                // Launch threads and wait for it. Compare the answers and return it.
                int i = 0;
                int files = servers.Count;
                foreach (string s in servers)
                {
                    List<object> arguments = new List<object>();
                    arguments.Add(servers[i]);
                    arguments.Add(filename);
                    arguments.Add(semantic);
                    ThreadPool.QueueUserWorkItem(ReadCallback, arguments);
                    i++;
                }
                Dictionary<DateTime, File> received = null;
                Dictionary<DateTime, int> votes = null;
                int winner = -1;

                while (winner < readQuorum)
                {

                    // In the best case, all replies are right
                    // In the worst case, this cycle is useless
                    while (readFiles.Count < readQuorum)
                    {
                    }

                    votes = new Dictionary<DateTime, int>();
                    received = new Dictionary<DateTime, File>();

                    // Count votes
                    foreach (File f in readFiles)
                    {
                        if (!votes.ContainsKey(f.Version))
                        {
                            votes.Add(f.Version, 1);
                            received.Add(f.Version, f);
                        }
                        else
                        {
                            votes[f.Version]++;
                        }
                    }

                    // Sort votes and show the most voted
                    votes = Util.SortVotes(votes);
                    
                    
                    winner = votes.Values.Last();
                    Console.WriteLine("Winner " + winner);
                    Thread.Sleep(1000);
                }
                File selected = received[votes.Keys.Last()];

                if (semantic.Equals("default"))
                {
                    historic.Add(filename, selected);
                    Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
                }
                else
                {
                    if (historic.ContainsKey(filename))
                    {
                        File h = historic[filename];
                        if (selected.Version > h.Version)
                        {
                            Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
                        }
                        else
                        {
                            Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(h.Content));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
                    }
                }
            }
        }

        public void Read(string filename, string semantic)
        {
            Thread t = new Thread(() => ExecuteRead(filename, semantic));
            t.Start();
        }

        private void WriteCallback(object threadcontext)
        {
            List<object> args = (List<object>)threadcontext;
            string server = (string)args[0];
            string filename = (string)args[1];
            byte[] bytearray = (byte[])args[2];

            IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), server);

            if (dataServer != null)
            {
                try
                {
                    int intTest = -1;
                    while (intTest == -1)
                    {
                        intTest = dataServer.Write(filename, bytearray);

                        if (intTest == 0)
                        {
                            writeFiles.Add(intTest);
                            break;
                        }
                        Thread.Sleep(1000 * requestInterval);
                    }
                }
                catch (SystemException)
                {
                }
            }
        }

        private void ExecuteWrite(string filename, byte[] bytearray)
        {
            if (openFiles.ContainsKey(filename))
            {
                Metadata file = openFiles[filename];
                List<string> servers = file.DataServers;
                int writeQuorum = file.WriteQuorum;
                writeFiles = new ConcurrentBag<int>();
                string bytes = Util.ConvertByteArrayToString(bytearray);
                byte[] content = Util.ConvertStringToByteArray(DateTime.Now.ToString("o") + (char)0x7f + bytes);

                foreach (string s in servers)
                {
                    List<object> arguments = new List<object>();
                    arguments.Add(s);
                    arguments.Add(filename);
                    arguments.Add(content);
                    ThreadPool.QueueUserWorkItem(WriteCallback, arguments);
                }
                while (writeFiles.Count < writeQuorum)
                {
                }
                Console.WriteLine("Write file: " + filename);
            }
        }

        public void Write(string filename, byte[] bytearray)
        {
            Thread t = new Thread(() => ExecuteWrite(filename, bytearray));
            t.Start();
        }

        public void Close(string filename)
        {
            bridge.Close(filename);

            if (openFiles.ContainsKey(filename))
            {
                openFiles.Remove(filename);
                Console.WriteLine("Close file " + filename);
            }
            else
            {
                Console.WriteLine("File already closed.");
            }
        }

        public void Delete(string filename)
        {
            if (!openFiles.ContainsKey(filename))
            {
                bridge.Delete(filename);
                Console.WriteLine("Delete file " + filename);
            }
            else
            {
                Console.WriteLine("File is opened.");
            }
        }

        public void UpdateServers(Dictionary<string, string> servers)
        {
            bridge.Servers = servers;
        }

        public string Dump()
        {
            string s = "Client " + name + " dump:\r\nOpen Files:\r\n";

            // Files opened by client
            foreach (string m in openFiles.Keys)
            {
                s += openFiles[m].ToString() + "\r\n";
                foreach (string d in openFiles[m].DataServers)
                {
                    s += "\t" + d + "\r\n";
                }
            }
            s += "String Register:\r\n";
            for(int i = 0; i< 10; i++)
            {
                if (stringRegister[i] != null)
                {
                    s += Util.ConvertByteArrayToString(stringRegister[i]) + "\r\n";
                }
            }
            return s;
        }

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            Client c = new Client(arguments[0], arguments[1]);
            Console.Title = "Iurie's Client: " + c.name;
            // Fazer coisas que Iuri mandar
            TcpChannel channel = new TcpChannel(c.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(c, c.name, typeof(Client));
            Console.ReadLine();
        }
    }
}
