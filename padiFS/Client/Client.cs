﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;

namespace padiFS
{
    public class Client : MarshalByRefObject, IClient
    {
        private string name;
        private int port;
        private Dictionary<string, Metadata> openFiles;

        public Client(string id)
        {
            this.name = "c-" + id;
            this.port = 8099;

            this.openFiles = new Dictionary<string, Metadata>();
        }

        public void Create(string filename, int nServers, int rQuorum, int wQuorum)
        {
            Console.WriteLine("Create file " + filename);
        }

        public void Open(string filename)
        {
            Console.WriteLine("Open file " + filename);
        }

        public void Read(string filename)
        {
            Console.WriteLine("Read file " + filename);
        }

        public void Write(string filename)
        {
            Console.WriteLine("Write file " + filename);
        }

        static void Main(string[] args)
        {
            Client c = new Client(args[0]);
            // Fazer coisas que Iuri mandar
            TcpChannel channel = new TcpChannel(c.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(c, c.name, typeof(Client));
            Console.WriteLine(c.name);
            Console.ReadLine();
        }
    }
}
