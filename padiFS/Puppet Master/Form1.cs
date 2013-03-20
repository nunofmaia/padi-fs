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
        private Dictionary<string, string> dataServers;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, string> clients;

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

            mscounter = 0;
            dscounter = 0;

            InitializeComponent();
            serversComboBox.DataSource = servers;
            stopOpComboBox.DataSource = methods;

            dataServers = new Dictionary<string, string>();
            metadataServers = new Dictionary<string, string>();
            clients = new Dictionary<string, string>();

            channel = new TcpChannel(8070);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMaster), "PuppetMaster", WellKnownObjectMode.Singleton);

        }

        private void launchMetadataServer(int id)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Metadata Server.exe";
            info.Arguments = id.ToString();

            Process.Start(info);
        }

        private void launchDataServer(int id)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Data Server.exe";
            info.Arguments = id.ToString();

            Process.Start(info);
        }

        private void launchClient(int id)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Client.exe";
            info.Arguments = id.ToString();

            Process.Start(info);
        }

        private void registerDataServer(string name, string address)
        {
            foreach (string key in metadataServers.Keys)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[key]);

                if(server != null)
                {
                    server.RegisterDataServer(name, address);
                }
            }
        }

        private void registerMetadataServer(string name, string address)
        {
            foreach (string key in metadataServers.Keys)
            {
                if (!key.Equals(name))
                {
                    IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[key]);

                    if (server != null)
                    {
                        try
                        {
                            server.RegisterMetadataServer(name, address);
                        }
                        catch (System.Net.Sockets.SocketException e) { }
                        // Ignore it
                    }
                }
            }
        }

        private void launchButton_Click(object sender, EventArgs e)
        {
            switch (serversComboBox.Text)
            {
                case "Metadata":
                    string ms_name = "m-" + mscounter;
                    string ms_address = "tcp://localhost:808" + mscounter + "/" + ms_name;
                    launchMetadataServer(mscounter);
                    metadataServers.Add(ms_name, ms_address);
                    mscounter++;
                    registerMetadataServer(ms_name, ms_address);
                    break;

                case "Data":
                    if (mscounter != 0)
                    {
                        string ds_name = "d-" + dscounter;
                        string ds_address = "tcp://localhost:809" + dscounter + "/" + ds_name;

                        launchDataServer(dscounter);
                        dataServers.Add(ds_name, ds_address);
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
                    string c_address = "tcp://localhost:8099/" + c_name;
                    launchClient(ccounter);
                    clients.Add(c_name, c_address);
                    ccounter++;
                    break;
            }
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

        }
    }
}
