using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;

namespace padiFS
{
    public class Client : MarshalByRefObject, IClient, ICommander
    {
        private static TcpChannel channel;
        private string name;
        private int port;
        private Bridge bridge;
        private Dictionary<string, Metadata> openFiles;
        private ConcurrentDictionary<string, File> historic;
        private ConcurrentBag<File> readFiles;
        private ConcurrentBag<int> writeFiles;
        private byte[][] stringRegister;
        private Metadata[] fileRegister;
        private int registersLimit;
        private int nextRegister;

        ManualResetEvent read;
        ManualResetEvent write;

        public Client(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);
            this.bridge = new Bridge();
            this.openFiles = new Dictionary<string, Metadata>(10);
            this.historic = new ConcurrentDictionary<string, File>();
            this.stringRegister = new byte[10][];
            this.fileRegister = new Metadata[10];
            registersLimit = 10;
            nextRegister = 0;
            read = new ManualResetEvent(true);
            write = new ManualResetEvent(true);
        }

        public void Create(string filename, int nServers, int rQuorum, int wQuorum)
        {
            try
            {
                Metadata meta = bridge.Create(this.name, filename, nServers, rQuorum, wQuorum);

                openFiles.Add(filename, meta);

                AddToFileRegister(meta);
                Console.WriteLine("Create file " + filename);
            }
            catch (FileAlreadyExists e)
            {
                Console.WriteLine(e.Message);
            }
            catch (ServerNotAvailableException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Open(string filename)
        {
            try
            {
                Metadata meta = bridge.Open(this.name, filename);

                if (meta != null)
                {
                    if (!openFiles.ContainsKey(filename))
                    {
                        openFiles.Add(filename, meta);
                        AddToFileRegister(meta);
                    }
                    Console.WriteLine("Open file " + filename);
                }
                else
                {
                    Console.WriteLine("Something wrong happened.");
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (FileIsOpenedException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (ServerNotAvailableException e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public void Close(string filename)
        {
            try
            {
                bridge.Close(this.name, filename);
                openFiles.Remove(filename);
                Console.WriteLine("Close file " + filename);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (FileNotOpenException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (FileAlreadyClosedException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (ServerNotAvailableException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Delete(string filename)
        {
            try
            {
                bridge.Delete(this.name, filename);
                Console.WriteLine("Delete file " + filename);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (FileIsOpenedException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (ServerNotAvailableException e)
            {
                Console.WriteLine(e.Message);
            }
        }


        // Method used to add a new/open file to a free file register
        // Limit - 10
        private void AddToFileRegister(Metadata meta)
        {
            fileRegister[nextRegister] = meta;
            nextRegister = (nextRegister + 1) % registersLimit;
        }

        // Method used by one thread to perform the call to the data servers
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
                    file = dataServer.Read(filename, semantic);

                    if (file != null)
                    {
                        lock (this)
                        {
                            readFiles.Add(file);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (SystemException)
                {
                }
            }
        }

        // Method used by one thread to perform the Read operation
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
                ReadCallDataServers(filename, semantic, servers);

                Dictionary<long, File> received = null;
                Dictionary<long, int> votes = null;
                long winner = 0;

                while (!ReadVoting(readQuorum, ref received, ref votes, ref winner))
                {
                    file = openFiles[filename];
                    servers = file.DataServers;
                    received = null;
                    votes = null;
                    readFiles = new ConcurrentBag<File>();
                    ReadCallDataServers(filename, semantic, servers);
                }

                File selected = received[winner];

                if (semantic.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!historic.ContainsKey(filename))
                    {
                        historic.TryAdd(filename, selected);
                    }
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
                        historic.TryAdd(filename, selected);
                        Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
                    }
                }
            }
        }

        // Method that performs the voting count of answers from data servers
        private bool ReadVoting(int readQuorum, ref Dictionary<long, File> received, ref Dictionary<long, int> votes, ref long winner)
        {
            int votingTimer = 0;
            int bestVote = 0;
            while (bestVote < readQuorum)
            {
                // Tries 5 times to reach a quorum before trying to read again
                votingTimer++;
                if (votingTimer > 5)
                {
                    return false;
                }

                // In the best case, all replies are right
                // In the worst case, this cycle is useless
                int countigTimer = 0;
                while (readFiles.Count < readQuorum)
                {
                    countigTimer++;
                    if (countigTimer > 5)
                    {
                        return false;
                    }
                    Thread.Sleep(1000);
                }

                votes = new Dictionary<long, int>();
                received = new Dictionary<long, File>();

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
                Dictionary<int, long> sortedVotes = Util.SortVotes(votes);

                bestVote = sortedVotes.Keys.Last();
                winner = sortedVotes.Values.Last();
                Thread.Sleep(1000);
            }
            return true;
        }

        // Method that perform calls to all data servers that store the file
        private void ReadCallDataServers(string filename, string semantic, List<string> servers)
        {
            foreach (string s in servers)
            {
                List<object> arguments = new List<object>();
                arguments.Add(s);
                arguments.Add(filename);
                arguments.Add(semantic);
                ThreadPool.QueueUserWorkItem(ReadCallback, arguments);
            }
        }

        private void ExecutePMRead(int file, string semantic, int register)
        {
            Metadata m = fileRegister[file];
            string filename = m.Filename;
            List<string> servers = m.DataServers;
            int readQuorum = m.ReadQuorum;
            readFiles = new ConcurrentBag<File>();

            // Call all the data servers that have the file and wait for a majority
            // Launch threads and wait for it. Compare the answers and return it.
            //readsArray = new bool[servers.Count];
            ReadCallDataServers(filename, semantic, servers);

            Dictionary<long, File> received = null;
            Dictionary<long, int> votes = null;
            long winner = 0;

            while (!ReadVoting(readQuorum, ref received, ref votes, ref winner))
            {
                m = fileRegister[file];
                servers = m.DataServers;
                received = null;
                votes = null;
                readFiles = new ConcurrentBag<File>();
                ReadCallDataServers(filename, semantic, servers);
            }

            File selected = received[winner];

            if (semantic.Equals("default", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!historic.ContainsKey(filename))
                {
                    historic.TryAdd(filename, selected);
                }
                stringRegister[register] = selected.Content;
                Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
            }
            else
            {
                if (historic.ContainsKey(filename))
                {
                    File h = historic[filename];
                    if (selected.Version > h.Version)
                    {
                        stringRegister[register] = selected.Content;
                        Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
                    }
                    else
                    {
                        stringRegister[register] = h.Content;
                        Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(h.Content));
                    }
                }
                else
                {
                    historic.TryAdd(filename, selected);
                    stringRegister[register] = selected.Content;
                    Console.WriteLine("Read file " + filename + ": " + Util.ConvertByteArrayToString(selected.Content));
                }
            }
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
                    intTest = dataServer.Write(filename, bytearray);

                    if (intTest == 0)
                    {
                        lock (this)
                        {
                            writeFiles.Add(intTest);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IOException)
                {
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
                byte[] content = Util.ConvertStringToByteArray(Token().ToString() + (char)0x7f + bytes);

                WriteCallDataServers(filename, servers, content);

                // Tries 5 times to reach a quorum before trying to write again
                int timer = 0;
                while (writeFiles.Count < writeQuorum)
                {
                    timer++;
                    if (timer > 5)
                    {
                        file = openFiles[filename];
                        servers = file.DataServers;
                        writeFiles = new ConcurrentBag<int>();
                        WriteCallDataServers(filename, servers, content);
                        timer = 0;

                    }
                    Thread.Sleep(1000);
                }
                Console.WriteLine("Write file: " + filename);
            }
        }

        private void WriteCallDataServers(string filename, List<string> servers, byte[] content)
        {
            foreach (string s in servers)
            {
                List<object> arguments = new List<object>();
                arguments.Add(s);
                arguments.Add(filename);
                arguments.Add(content);
                ThreadPool.QueueUserWorkItem(WriteCallback, arguments);
            }
        }

        // Read for Puppet Master GUI 
        public void Read(string filename, string semantic)
        {
            ExecuteRead(filename, semantic);
        }

        // Read for Puppet Master Scripts
        public void Read(string file, string semantic, string register)
        {

            int f, r;
            if (Int32.TryParse(file, out f) && Int32.TryParse(register, out r))
            {
                ExecutePMRead(f, semantic, r);
            }
        }

        public void Write(string filename, byte[] bytearray)
        {
            ExecuteWrite(filename, bytearray);
        }

        public void Write(string file, int register)
        {
            ExecutePMWriteRegister(file, register);
        }

        public void Write(string file, string content)
        {
            ExecutePMWriteContent(file, content);
        }

        // Method to get the file from the file register and write the content
        // provided in the script
        private void ExecutePMWriteContent(string file, string content)
        {
            int f;
            if (Int32.TryParse(file, out f))
            {
                Metadata m = fileRegister[f];
                string filename = m.Filename;
                byte[] bytearray = Util.ConvertStringToByteArray(content);
                ExecuteWrite(filename, bytearray);
            }
        }

        // Method to get the file from the file register and write the content
        // previously stored in the client's string register
        private void ExecutePMWriteRegister(string file, int register)
        {
            int f;
            if (Int32.TryParse(file, out f))
            {
                Metadata m = fileRegister[f];
                string filename = m.Filename;
                byte[] bytearray = stringRegister[register];
                ExecuteWrite(filename, bytearray);
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
            for (int i = 0; i < registersLimit; i++)
            {
                if (stringRegister[i] != null)
                {
                    s += Util.ConvertByteArrayToString(stringRegister[i]) + "\r\n";
                }
            }
            return s;
        }

        public void ExeScript(string path)
        {
            StreamReader script = new StreamReader(path);

            while (!script.EndOfStream)
            {
                try
                {
                    string command = script.ReadLine();

                    while (command != null)
                    {
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            command.Trim();
                            if (!(command.Length > 0 && command[0] == '#'))
                            {
                                break;
                            }
                        }
                        command = script.ReadLine();
                    }

                    if (command != null)
                    {
                        command.Trim();
                        HandleCommand(command);
                    }

                }
                catch (IOException)
                {
                }
            }

            script.Close();
            script = null;
        }

        public void Copy(int file1, string semantics, int file2, string salt)
        {
            //Read file from file1 with given semantics
            Metadata m = fileRegister[file1];
            string filename = m.Filename;
            List<string> servers = m.DataServers;
            int readQuorum = m.ReadQuorum;
            readFiles = new ConcurrentBag<File>();

            // Call all the data servers that have the file and wait for a majority
            // Launch threads and wait for it. Compare the answers and return it.
            //readsArray = new bool[servers.Count];
            ReadCallDataServers(filename, semantics, servers);

            Dictionary<long, File> received = null;
            Dictionary<long, int> votes = null;
            long winner = 0;

            while (!ReadVoting(readQuorum, ref received, ref votes, ref winner))
            {
                m = fileRegister[file1];
                servers = m.DataServers;
                ReadCallDataServers(filename, semantics, servers);
                readFiles = new ConcurrentBag<File>();
                received = null;
                votes = null;
            }

            File selected = received[winner];
            string fileRead = "";

            if (semantics.Equals("default", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!historic.ContainsKey(filename))
                {
                    historic.TryAdd(filename, selected);
                }
                fileRead = Util.ConvertByteArrayToString(selected.Content);
            }
            else
            {
                if (historic.ContainsKey(filename))
                {
                    File h = historic[filename];
                    if (selected.Version > h.Version)
                    {
                        fileRead = Util.ConvertByteArrayToString(selected.Content);
                    }
                    else
                    {
                        fileRead = Util.ConvertByteArrayToString(h.Content);
                    }
                }
                else
                {
                    historic.TryAdd(filename, selected);
                    fileRead = Util.ConvertByteArrayToString(selected.Content);
                }
            }

            fileRead += salt;
            //Write file to file2 with previous read plus salt
            ExecuteWrite(fileRegister[file2].Filename, Util.ConvertStringToByteArray(fileRead));
        }

        private void HandleCommand(string line)
        {
            string command = "null";

            Match match = Regex.Match(line, @"^(\w+)\s.*$", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                command = match.Groups[1].Value.ToLower();
            }

            switch (command)
            {
                case "create":
                    execute(new CreateCommand(), line);
                    break;

                case "open":
                    execute(new OpenCommand(), line);
                    break;

                case "close":
                    execute(new CloseCommand(), line);
                    break;

                case "read":
                    execute(new ReadCommand(), line);
                    break;

                case "write":
                    execute(new WriteCommand(), line);
                    break;

                case "delete":
                    execute(new DeleteCommand(), line);
                    break;

                case "copy":
                    execute(new CopyCommand(), line);
                    break;

                case "dump":
                    Console.WriteLine((string)execute(new DumpCommand(), line));
                    break;

                default:
                    Console.WriteLine("Invalid command");
                    break;
            }


        }

        public object execute(padiFS.ICommand command, string line)
        {
            object result;

            result = command.execute(this, line);

            return result;
        }

        public void UpdateFileMetadata(string filename, Metadata metadata)
        {
            if (openFiles.ContainsKey(filename))
            {
                openFiles[filename] = metadata;

                for (int i = 0; i < registersLimit; i++)
                {
                    if (fileRegister[i] != null && fileRegister[i].Filename == filename)
                    {
                        fileRegister[i] = metadata;
                        break;
                    }
                }
            }
        }

        private long Token()
        {
            return bridge.GetToken();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            Client c = new Client(arguments[0], arguments[1]);
            Console.Title = "Iurie's Client: " + c.name;
            // Fazer coisas que Iuri mandar
            channel = new TcpChannel(c.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(c, c.name, typeof(Client));

            int origWidth = Console.WindowWidth;
            int origHeight = Console.WindowHeight;

            Console.SetWindowSize(origWidth, origHeight / 2);

            Console.ReadLine();
        }
    }
}
