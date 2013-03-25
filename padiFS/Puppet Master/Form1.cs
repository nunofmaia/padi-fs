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

namespace padiFS
{
    public partial class Form1 : Form
    {
        TcpChannel channel;
        private int mscounter;
        private int dscounter;
        private int ccounter;
        private string ms_primary;
        private Dictionary<string, string> dataServers;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, string> clients;
        private ArrayList activeClients;
        private ArrayList activeDataServers;
        private ArrayList activeMetadataServers;


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
            activeClients = new ArrayList();
            activeDataServers = new ArrayList();
            activeMetadataServers = new ArrayList();

            channel = new TcpChannel(8070);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMaster), "PuppetMaster", WellKnownObjectMode.Singleton);

        }

        private void launchMetadataServer(string name, int port, string primary)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Metadata Server.exe";
            info.Arguments = name + (char)0x7f + port.ToString() + (char)0x7f + primary;

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
                client.UpdateServers(metadataServers, ms_primary);
            }
        }

        private void launchDataServer(string name, int port)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Data Server.exe";
            info.Arguments = name + (char)0x7f + port.ToString();

            Process.Start(info);
        }

        private void launchClient(string name, int port)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Client.exe";
            info.Arguments = name + (char)0x7f + port.ToString();

            Process.Start(info);
        }

        private void registerDataServer(string name, string address)
        {
            foreach (string key in metadataServers.Keys)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[key]);

                if (server != null)
                {
                    server.RegisterDataServer(name, address);
                }
            }
        }

        private void registerMetadataServer(string name, string address)
        {
            if (activeMetadataServers.Count > 0)
            {
                foreach (string key in activeMetadataServers)
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[key]);
                    if (server != null)
                    {
                        if (!key.Equals(name))
                        {
                            try
                            {
                                server.RegisterMetadataServer(name, address);
                            }
                            catch (System.Net.Sockets.SocketException) { }
                            // Ignore it
                        }

                        ms_primary = server.GetPrimary();
                    }


                    if (ms_primary != null && ms_primary != key && key == name)
                    {
                        IMetadataServer primary = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[ms_primary]);
                        if (primary != null)
                        {
                            MetadataInfo info = primary.GetMetadataInfo();

                            if (server != null)
                            {
                                server.UpdateReplica(info);
                            }
                        }
                    }
                }
            }
            else
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                ms_primary = name;
                server.SetPrimary(name);
            }
        }

        private void launchButton_Click(object sender, EventArgs e)
        {
            switch (serversComboBox.Text)
            {
                case "Metadata":
                    string ms_name = "m-" + mscounter;
                    int ms_port = Util.FreeTcpPort();
                    string ms_address = "tcp://localhost:" + ms_port + "/" + ms_name;
                    if (mscounter == 0)
                    {
                        ms_primary = ms_name;
                    }
                    launchMetadataServer(ms_name, ms_port, ms_primary);
                    metadataServers.Add(ms_name, ms_address);
                    activeMetadataServers.Add(ms_name);
                    mscounter++;
                    registerMetadataServer(ms_name, ms_address);
                    break;

                case "Data":
                    if (mscounter != 0)
                    {
                        string ds_name = "d-" + dscounter;
                        int ds_port = Util.FreeTcpPort();
                        string ds_address = "tcp://localhost:" + ds_port + "/" + ds_name;

                        launchDataServer(ds_name, ds_port);
                        dataServers.Add(ds_name, ds_address);
                        activeDataServers.Add(ds_name);
                        dscounter++;
                        registerDataServer(ds_name, ds_address);
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
                    launchClient(c_name, c_port);
                    clients.Add(c_name, c_address);
                    activeClients.Add(c_name);
                    UpdateClientServer(c_name);
                    EnableButtons();
                    ccounter++;
                    break;
            }
        }

        private void EnableButtons()
        {
            createButton.Enabled = true;
            openFileButton.Enabled = true;
            closeFileButton.Enabled = true;
            readFileButton.Enabled = true;
            writeFileButton.Enabled = true;
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
            string c_name = writeClientTextBox.Text;
            string filename = writeFileTextBox.Text;
            byte[] bytearray = Util.ConvertStringToByteArray(writeTextBox.Text);

            IClient client = (IClient)Activator.GetObject(typeof(IClient), (string)clients[c_name]);

            if (client != null)
            {
                client.Write(filename, bytearray);
            }
            writeFileTextBox.Clear();
            writeClientTextBox.Clear();
            writeTextBox.Clear();
        }
    }
}
