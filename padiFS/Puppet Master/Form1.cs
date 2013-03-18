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
        private Dictionary<string, string> dataServers;
        private Dictionary<string, string> metadataServers;

        public Form1()
        {
            ArrayList servers = new ArrayList();
            servers.Add("Metadata");
            servers.Add("Data");
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

            channel = new TcpChannel(8080);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMaster), "PuppetMaster", WellKnownObjectMode.Singleton);

        }

        private void launchMetadataServer(string name)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Metadata Server.exe";
            info.Arguments = name;

            Process.Start(info);
        }

        private void launchDataServer(string name)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            string currentDir = Environment.CurrentDirectory;
            info.FileName = currentDir + @"\Data Server.exe";
            info.Arguments = name;

            Process.Start(info);
        }

        private void launchButton_Click(object sender, EventArgs e)
        {
            switch (serversComboBox.Text)
            {
                case "Metadata":
                    string ms_name = "m-" + mscounter;
                    launchMetadataServer(ms_name);
                    mscounter++;
                    metadataServers.Add(ms_name, @"tcp://localhost:8081/" + ms_name);
                    break;

                case "Data":
                    string ds_name = "d-" + dscounter;
                    launchMetadataServer(ds_name);
                    dscounter++;
                    dataServers.Add(ds_name, @"tcp://localhost:8082/" + ds_name);
                    break;
            }
        }
    }
}
