using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Sockets;

namespace padiFS
{

    public partial class Form1 : Form, ICommander
    {
        private static TcpChannel channel;
        private int mscounter;
        private int dscounter;
        private int ccounter;
        private Dictionary<string, string> dataServers;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, string> clients;
        private List<string> activeClients;
        private List<string> activeDataServers;
        private List<string> activeMetadataServers;
        private Dictionary<string, string> processes;

        private StreamReader script;
        private string scripts_dir;

        private string[] iurieCaptions = { "Iurie will follow you until the end of world.", "Iurie doesn't care.", "STAAAP, Iurie is mad", "Iurie is sad :(" };

        public Form1()
        {
            ArrayList servers = new ArrayList();
            servers.Add("Metadata");
            servers.Add("Data");
            servers.Add("Client");
            ArrayList methods = new ArrayList();
            methods.Add("Freeze");
            methods.Add("Unfreeze");
            methods.Add("Fail");
            methods.Add("Recover");
            ArrayList semantics = new ArrayList();
            semantics.Add("Default");
            semantics.Add("Monotonic");

            mscounter = 0;
            dscounter = 0;

            InitializeComponent();
            serversComboBox.DataSource = servers;
            stopOpComboBox.DataSource = methods;
            semanticsComboBox.DataSource = semantics;

            dataServers = new Dictionary<string, string>();
            metadataServers = new Dictionary<string, string>();
            clients = new Dictionary<string, string>();
            activeClients = new List<string>();
            activeDataServers = new List<string>();
            activeMetadataServers = new List<string>();
            processes = new Dictionary<string, string>();

            script = null;
            scripts_dir = Environment.CurrentDirectory + @"\Scripts";
            if (!Directory.Exists(scripts_dir))
            {
                Directory.CreateDirectory(scripts_dir);
            }

            channel = new TcpChannel(8070);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMaster), "PuppetMaster", WellKnownObjectMode.Singleton);

        }

        // LAUNCHING SITE
        private void LaunchProcess(string name)
        {
            int port;
            if (processes.ContainsKey(name))
            {
                port = Util.GetPortOnAddress(processes[name]);
            }
            else
            {
                port = Util.FreeTcpPort();
            }
            string address = "tcp://localhost:" + port + "/" + name;
            char code = name[0];

            switch (code)
            {
                case 'm':
                    if (!metadataServers.ContainsKey(name))
                    {
                        LaunchMetadataServer(name, port);
                        metadataServers.Add(name, address);
                        mscounter = Util.ProcessID(name) + 1;
                        registerMetadataServer(name, address);
                        activeMetadataServers.Add(name);
                        processes.Add(name, address);
                    }
                    else
                    {
                        try
                        {
                            IMetadataServer m = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), metadataServers[name]);

                            if (m != null)
                            {
                                m.Ping();
                            }
                        }
                        catch (ServerNotAvailableException)
                        {
                        }
                        catch (System.IO.IOException)
                        {
                            LaunchMetadataServer(name, port);
                            registerMetadataServer(name, address);
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            LaunchMetadataServer(name, port);
                            registerMetadataServer(name, address);
                        }
                    }

                    break;

                case 'd':
                    if (!dataServers.ContainsKey(name))
                    {
                        if (mscounter != 0)
                        {
                            LaunchDataServer(name, port);
                            dataServers.Add(name, address);
                            activeDataServers.Add(name);
                            dscounter = Util.ProcessID(name) + 1;
                            registerDataServer(name, address);
                            processes.Add(name, address);
                            UpdateFileMetadata(name, address);

                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("A Data Server should be launched only after a Metadata Server");
                        }
                    }
                    else
                    {
                        try
                        {
                            IDataServer d = (IDataServer)Activator.GetObject(typeof(IDataServer), dataServers[name]);

                            if (d != null)
                            {
                                d.isFreezed();
                            }
                        }
                        catch (ServerNotAvailableException)
                        {
                        }
                        catch (System.IO.IOException)
                        {
                            LaunchDataServer(name, port);
                            registerDataServer(name, address);
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            LaunchDataServer(name, port);
                            registerDataServer(name, address);
                        }
                    }

                    break;

                case 'c':
                    if (!clients.ContainsKey(name))
                    {
                        LaunchClient(name, port);
                        clients.Add(name, address);
                        registerClients(name, address);
                        activeClients.Add(name);
                        UpdateClientServer(name);
                        if (metadataServers.Count > 0 && dataServers.Count > 0)
                        {
                            EnableButtons();
                        }
                        ccounter = Util.ProcessID(name) + 1;
                        processes.Add(name, address);
                    }
                    else
                    {
                        try
                        {
                            IClient c = (IClient)Activator.GetObject(typeof(IClient), clients[name]);

                            if (c != null)
                            {
                                // Need to call a method that does nothing...
                                c.ToString();
                            }
                        }
                        catch (System.IO.IOException)
                        {
                            LaunchClient(name, port);
                            registerClients(name, address);
                            UpdateClientServer(name);
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            LaunchClient(name, port);
                            registerClients(name, address);
                            UpdateClientServer(name);
                        }

                    }

                    break;
            }
        }

        private void LaunchMetadataServer(string name, int port)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Metadata Server.exe";
            info.Arguments = name + (char)0x7f + port.ToString();

            Process.Start(info);

            foreach (string c in clients.Keys)
            {
                UpdateClientServer(c);
            }
        }


        private void UpdateClientServer(string c)
        {
            string address = clients[c];
            IClient client = (IClient)Activator.GetObject(typeof(IClient), address);

            if (client != null)
            {
                try
                {
                    client.UpdateServers(metadataServers);
                }
                catch (SocketException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        private void LaunchDataServer(string name, int port)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Data Server.exe";
            info.Arguments = name + (char)0x7f + port.ToString();

            Process.Start(info);
        }

        private void LaunchClient(string name, int port)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Client.exe";
            info.Arguments = name + (char)0x7f + port.ToString();

            Process.Start(info);
        }


        // REGISTERING SITE
        private void registerDataServer(string name, string address)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary_name = AskForPrimary();
                    if (primary_name != null)
                    {
                        IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary_name]);
                        if (primary != null)
                        {
                            primary.RegisterDataServer(name, address);
                            executed = true;
                        }
                    }
                }
                catch (SystemException) { }
            }

            IDataServer data = (IDataServer)Activator.GetObject(typeof(IDataServer), address);

            if (data != null)
            {
                data.RestoreFiles();
            }
        }

        private void registerClients(string name, string address)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary_name = AskForPrimary();
                    if (primary_name != null)
                    {
                        IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary_name]);
                        if (primary != null)
                        {
                            primary.RegisterClient(name, address);
                            executed = true;
                        }
                    }
                }
                catch (SystemException) { }
            }
        }

        private void registerMetadataServer(string name, string address)
        {

            if (metadataServers.Count > 1)
            {
                bool executed = false;
                while (!executed)
                {
                    try
                    {
                        string primary_name = AskForPrimary(name);
                        if (primary_name != null)
                        {
                            IMetadataServer replica = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[name]);
                            IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary_name]);
                            if (primary != null)
                            {
                                replica.DeserializeServer();
                                string[] log = primary.GetLog(-1);
                                if (replica != null)
                                {
                                    replica.UpdateLog(log);
                                }
                                primary.RegisterMetadataServer(name, address);
                                executed = true;
                            }
                        }
                        else
                        {
                            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                            server.SetPrimary(name);
                            executed = true;
                        }
                    }
                    catch (SystemException) { }
                }
            }
            else
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                server.SetPrimary(name);
            }
        }

        private void UpdateFileMetadata(string name, string address)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary = AskForPrimary();

                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), metadataServers[primary]);

                    if (server != null)
                    {
                        server.UpdateFileMetada(name, address);
                        executed = true;
                    }
                }
                catch (SystemException) { }
            }
        }

        // TODO: use the LaunchProcess method to launch each process instead of doing always the same thing
        private void launchButton_Click(object sender, EventArgs e)
        {
            switch (serversComboBox.Text)
            {
                case "Metadata":
                    string ms_name = "m-" + mscounter;
                    int ms_port = Util.FreeTcpPort();
                    string ms_address = "tcp://localhost:" + ms_port + "/" + ms_name;

                    LaunchMetadataServer(ms_name, ms_port);
                    metadataServers.Add(ms_name, ms_address);
                    mscounter++;
                    registerMetadataServer(ms_name, ms_address);
                    activeMetadataServers.Add(ms_name);
                    processes.Add(ms_name, ms_address);
                    break;

                case "Data":
                    if (mscounter != 0)
                    {
                        string ds_name = "d-" + dscounter;
                        int ds_port = Util.FreeTcpPort();
                        string ds_address = "tcp://localhost:" + ds_port + "/" + ds_name;

                        LaunchDataServer(ds_name, ds_port);
                        dataServers.Add(ds_name, ds_address);
                        activeDataServers.Add(ds_name);
                        dscounter++;
                        registerDataServer(ds_name, ds_address);
                        processes.Add(ds_name, ds_address);
                        UpdateFileMetadata(ds_name, ds_address);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("A Data Server should be launched only after a Metadata Server");
                    }
                    break;

                case "Client":
                    string c_name = "c-" + ccounter;
                    int c_port = Util.FreeTcpPort();
                    string c_address = "tcp://localhost:" + c_port + "/" + c_name;

                    LaunchClient(c_name, c_port);
                    clients.Add(c_name, c_address);
                    activeClients.Add(c_name);
                    UpdateClientServer(c_name);
                    ccounter++;
                    processes.Add(c_name, c_address);
                    break;
            }
            if (metadataServers.Count > 0 && dataServers.Count > 0 && clients.Count > 0)
            {
                EnableButtons();
            }
        }

        private void EnableButtons()
        {
            createButton.Enabled = true;
            openFileButton.Enabled = true;
            closeFileButton.Enabled = true;
            readFileButton.Enabled = true;
            writeFileButton.Enabled = true;
            deleteFileButton.Enabled = true;
            dumpButton.Enabled = true;
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            string c_name = createClientTextBox.Text;
            string filename = createNameTextBox.Text;
            int nServers = int.Parse(serversNumberTextBox.Text);
            int rQuorum = int.Parse(rQuorumTextBox.Text);
            int wQuorum = int.Parse(wQuorumTextBox.Text);

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[c_name]);

            if (client != null)
            {
                client.Create(filename, nServers, rQuorum, wQuorum);
            }

            createClientTextBox.Clear();
            createNameTextBox.Clear();
            serversNumberTextBox.Clear();
            rQuorumTextBox.Clear();
            wQuorumTextBox.Clear();

        }

        private void openFileButton_Click(object sender, EventArgs e)
        {
            string c_name = openClientTextBox.Text;
            string filename = openFileTextBox.Text;

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[c_name]);

            if (client != null)
            {
                client.Open(filename);
            }

            openClientTextBox.Clear();
            openFileTextBox.Clear();
        }

        private void closeFileButton_Click(object sender, EventArgs e)
        {
            string c_name = closeClientTextBox.Text;
            string filename = closeFileTextBox.Text;

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[c_name]);

            if (client != null)
            {
                client.Close(filename);
            }

            closeClientTextBox.Clear();
            closeFileTextBox.Clear();
        }

        private void deleteFileButton_Click(object sender, EventArgs e)
        {
            string c_name = deleteClientTextBox.Text;
            string filename = deleteFileTextBox.Text;

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[c_name]);

            if (client != null)
            {
                client.Delete(filename);
            }
            deleteClientTextBox.Clear();
            deleteFileTextBox.Clear();
        }

        private void executeButton_Click(object sender, EventArgs e)
        {
            string process = stopProcessTextBox.Text;
            string ms_address = null;
            string ds_address = null;
            if (metadataServers.ContainsKey(process))
            {
                ms_address = metadataServers[process];
            }
            else if (dataServers.ContainsKey(process))
            {
                ds_address = dataServers[process];
            }


            IMetadataServer ms_server = null;
            IDataServer ds_server = null;
            if (ms_address != null)
            {
                ms_server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), ms_address);
            }
            else
            {
                ds_server = (IDataServer)Activator.GetObject(typeof(IDataServer), ds_address);
            }


            switch (stopOpComboBox.Text)
            {
                case "Freeze":
                    if (ds_server != null)
                    {
                        ds_server.Freeze();
                    }
                    break;

                case "Unfreeze":
                    if (ds_server != null)
                    {
                        ds_server.Unfreeze();
                    }
                    break;

                case "Fail":
                    if (ds_server != null)
                    {
                        ds_server.Fail();
                    }
                    else if (ms_server != null)
                    {
                        ms_server.Fail();
                        activeMetadataServers.Remove(process);
                    }
                    break;

                case "Recover":
                    if (ds_server != null)
                    {
                        ds_server.Recover();
                    }
                    else if (ms_server != null)
                    {
                        ms_server.Recover();
                        activeMetadataServers.Add(process);
                    }
                    break;
            }
            stopProcessTextBox.Clear();
        }

        private void readFileButton_Click(object sender, EventArgs e)
        {
            string c_name = readClientTextBox.Text;
            string filename = readFileTextBox.Text;
            string semantic = semanticsComboBox.Text;

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[c_name]);

            if (client != null)
            {
                client.Read(filename, semantic);
            }
            readFileTextBox.Clear();
            readClientTextBox.Clear();
        }

        private void writeFileButton_Click(object sender, EventArgs e)
        {
            string command = "WRITE ";
            if (string.IsNullOrEmpty(writeByteRegisterTextBox.Text))
            {
                command += writeClientTextBox.Text + ", " + writeFileRegisterTextBox.Text + ", " + "\"" + writeTextBox.Text + "\"";
            }
            else
            {
                command += writeClientTextBox.Text + ", " + writeFileRegisterTextBox.Text + ", " + writeByteRegisterTextBox.Text;
            }

            HandleCommand(command);
        }

        private void loadScriptButton_Click(object sender, EventArgs e)
        {
            DialogResult result = openScriptDialog.ShowDialog();
            statusTextBox.Clear();
            if (result == DialogResult.OK)
            {
                string file = openScriptDialog.FileName;

                try
                {
                    scriptTextBox.Text = file;
                    script = new StreamReader(file);
                    runScriptButton.Enabled = true;
                    stepScriptButton.Enabled = true;
                }
                catch (IOException)
                {
                }
            }
        }

        private void runScriptButton_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(() => ExecuteRun());
            t.Start();
        }

        private void ExecuteRun()
        {
            string filePath = scriptTextBox.Text;
            scriptTextBox.Clear();


            runScriptButton.Enabled = false;
            stepScriptButton.Enabled = false;
            stopScriptButton.Enabled = true;
            loadScriptButton.Enabled = false;

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
                            if (command.Length > 0 && command[0] == '#')
                            {
                                statusTextBox.Text += command + "\r\n";
                            }
                            else
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
            stopScriptButton.Enabled = false;
            runScriptButton.Enabled = false;
            loadScriptButton.Enabled = true;
        }

        private void nextStepScriptButton_Click(object sender, EventArgs e)
        {
            scriptTextBox.Clear();


            runScriptButton.Enabled = false;
            stepScriptButton.Enabled = true;
            stopScriptButton.Enabled = true;
            loadScriptButton.Enabled = false;

            try
            {
                string command = script.ReadLine();

                while (command != null)
                {
                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        command.Trim();
                        if (command.Length > 0 && command[0] == '#')
                        {
                            statusTextBox.Text += command + "\r\n";
                        }
                        else
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

                if (script.EndOfStream)
                {
                    script.Close();
                    script = null;
                    stopScriptButton.Enabled = false;
                    stepScriptButton.Enabled = false;
                    loadScriptButton.Enabled = true;
                    return;
                }

            }
            catch (IOException)
            {
            }

        }

        private void stopScriptButton_Click(object sender, EventArgs e)
        {
            script.Close();
            script = null;

            loadScriptButton.Enabled = true;
            stepScriptButton.Enabled = false;
            stopScriptButton.Enabled = false;
        }


        private void dumpButton_Click(object sender, EventArgs e)
        {
            string command = "DUMP " + dumpTextBox.Text;
            HandleCommand(command);
            dumpTextBox.Clear();
        }

        private void HandleCommand(string line)
        {
            string command = "null";
            string process = null;

            Match match = Regex.Match(line, @"^(\w+)\s.*$", RegexOptions.IgnoreCase);
            Match user = Regex.Match(line, @"([c|d|m]-\d+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                command = match.Groups[1].Value.ToLower();
                if (user.Success)
                {
                    process = user.Groups[1].Value;
                }
            }

            string[] args = line.Replace(",", "").Split(' ');

            statusTextBox.Text += "> " + line + "\r\n";
            switch (command)
            {
                case "fail":
                    LaunchProcess(process);
                    execute(new FailCommand(), line);
                    break;

                case "recover":
                    LaunchProcess(process);
                    execute(new RecoverCommand(), line);
                    break;

                case "freeze":
                    LaunchProcess(process);
                    execute(new FreezeCommand(), line);
                    break;

                case "unfreeze":
                    LaunchProcess(process);
                    execute(new UnfreezeCommand(), line);
                    break;

                case "create":
                    LaunchProcess(process);
                    execute(new CreateCommand(), line);
                    break;

                case "open":
                    LaunchProcess(process);
                    execute(new OpenCommand(), line);
                    break;

                case "close":
                    LaunchProcess(process);
                    execute(new CloseCommand(), line);
                    break;

                case "read":
                    LaunchProcess(process);
                    execute(new ReadCommand(), line);
                    break;

                case "write":
                    LaunchProcess(process);
                    execute(new WriteCommand(), line);
                    break;

                case "delete":
                    LaunchProcess(process);
                    execute(new DeleteCommand(), line);
                    break;

                case "copy":
                    LaunchProcess(process);
                    execute(new CopyCommand(), line);
                    break;

                case "dump":
                    LaunchProcess(process);
                    statusTextBox.AppendText((string)execute(new DumpCommand(), line));
                    break;

                case "exescript":
                    LaunchProcess(process);
                    new Thread(() => execute(new ExeScriptCommand(), line)).Start();
                    break;

                default:
                    System.Windows.Forms.MessageBox.Show("Invalid command. " + iurieCaptions[new Random().Next(0, 4)]);
                    break;
            }


        }

        // CLIENT
        private void ExecScriptCommand(string process, string filename)
        {
            throw new NotImplementedException();
        }

        // DATA, METADATA, CLIENT
        private void DumpCommand(string process)
        {
            char code = process[0];

            switch (code)
            {
                case 'm':
                    IMetadataServer ms_server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), processes[process]);
                    if (ms_server != null)
                    {
                        statusTextBox.Text += ms_server.Dump() + "\r\n";
                    }
                    break;
                case 'd':
                    IDataServer ds_server = (IDataServer)Activator.GetObject(typeof(IDataServer), processes[process]);
                    if (ds_server != null)
                    {
                        statusTextBox.Text += ds_server.Dump() + "\r\n";
                    }
                    break;
                case 'c':
                    IClient client = (IClient)Activator.GetObject(typeof(IClient), processes[process]);
                    if (client != null)
                    {
                        statusTextBox.Text += client.Dump() + "\r\n";
                    }
                    break;
            }
        }

        private void CopyCommand(string process, string fileRegister1, string semantics, string fileRegister2, string salt)
        {
            throw new NotImplementedException();
        }

        private void WriteCommand(string process, string fileRegister, string source)
        {
            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[process]);
            int register;

            //Needed to do this here, to diferentiate what method to call
            if (Int32.TryParse(source, out register))
            {
                if (register >= 0 || register <= 9)
                {
                    if (client != null)
                    {
                        // Call Write with register
                        client.Write(fileRegister, register);
                    }
                }
            }
            else if (client != null)
            {
                // Call Write with content
                client.Write(fileRegister, source);
            }
        }

        private void ReadCommand(string process, string fileRegister, string semantics, string register)
        {
            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[process]);

            if (client != null)
            {
                client.Read(fileRegister, semantics, register);
            }
        }

        private void CloseCommand(string process, string filename)
        {
            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[process]);

            if (client != null)
            {
                client.Close(filename);
            }
        }

        private void OpenCommand(string process, string filename)
        {
            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[process]);

            if (client != null)
            {
                client.Open(filename);
            }
        }

        private void CreateCommand(string process, string filename, string numberOfServers, string readQuorum, string writeQuorum)
        {
            int nServers = int.Parse(numberOfServers);
            int rQuorum = int.Parse(readQuorum);
            int wQuorum = int.Parse(writeQuorum);

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[process]);

            if (client != null)
            {
                client.Create(filename, nServers, rQuorum, wQuorum);
            }
        }

        // DATA
        private void UnfreezeCommand(string process)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), processes[process]);

            if (server != null)
            {
                server.Unfreeze();
            }
        }

        // DATA
        private void FreezeCommand(string process)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), processes[process]);

            if (server != null)
            {
                server.Freeze();
            }
        }

        // DATA, METADATA
        private void RecoverCommand(string process)
        {
            char code = process[0];

            switch (code)
            {
                case 'm':
                    IMetadataServer ms_server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), processes[process]);
                    if (ms_server != null)
                    {
                        ms_server.Recover();
                        activeMetadataServers.Add(process);
                    }
                    break;

                case 'd':
                    IDataServer ds_server = (IDataServer)Activator.GetObject(typeof(IDataServer), processes[process]);
                    if (ds_server != null)
                    {
                        ds_server.Recover();
                    }
                    break;
            }
        }

        // DATA, METADATA
        private void FailCommand(string process)
        {
            char code = process[0];

            switch (code)
            {
                case 'm':
                    IMetadataServer ms_server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), processes[process]);
                    ms_server.Fail();
                    activeMetadataServers.Remove(process);
                    break;

                case 'd':
                    IDataServer ds_server = (IDataServer)Activator.GetObject(typeof(IDataServer), processes[process]);
                    ds_server.Fail();
                    break;
            }
        }

        public object execute(ICommand command, string line)
        {
            Match match = Regex.Match(line, @"([d|m|c]-\d+)", RegexOptions.IgnoreCase);
            string process = "p";
            if (match.Success)
            {
                process = match.Groups[1].Value;
            }

            char code = process[0];

            object result = null;

            switch (code)
            {
                case 'm':
                    IMetadataServer ms_server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), processes[process]);
                    result = command.execute(ms_server, line);
                    break;

                case 'd':
                    IDataServer ds_server = (IDataServer)Activator.GetObject(typeof(IDataServer), processes[process]);
                    result = command.execute(ds_server, line);
                    break;

                case 'c':
                    IClient client = (IClient)Activator.GetObject(typeof(IClient), processes[process]);
                    result = command.execute(client, line);
                    break;
                default:
                    break;
            }

            return result;
        }

        private string AskForPrimary(string name)
        {
            foreach (string address in metadataServers.Values)
            {
                if (address != metadataServers[name])
                {
                    try
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                        if (server != null)
                        {
                            if (server.Ping())
                            {
                                return server.GetPrimary();
                            }
                        }
                    }
                    catch (ServerNotAvailableException)
                    {
                    }
                    catch (System.IO.IOException)
                    {
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                    }
                }
            }

            return null;
        }

        private string AskForPrimary()
        {
            foreach (string address in metadataServers.Values)
            {
                try
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                    if (server != null)
                    {
                        if (server.Ping())
                        {
                            return server.GetPrimary();
                        }
                    }
                }
                catch (ServerNotAvailableException)
                {
                }
                catch (System.IO.IOException)
                {
                }
                catch (System.Net.Sockets.SocketException)
                {
                }
            }

            return null;
        }

        private void statusTextBox_TextChanged(object sender, EventArgs e)
        {
            statusTextBox.SelectionStart = statusTextBox.Text.Length;
            statusTextBox.ScrollToCaret();
            statusTextBox.Refresh();
        }

        private void Clearbutton_Click(object sender, EventArgs e)
        {
            string path = @"..\Executables\";
            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                subdir.Delete(true);
            }
            System.Windows.Forms.MessageBox.Show("Logs Cleaned");
        }
    }
}
