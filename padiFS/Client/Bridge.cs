using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class Bridge
    {
        private Dictionary<string, string> metadataServers;
        //private string primary;

        public Dictionary<string, string> Servers
        {
            set
            {
                this.metadataServers = value;
            }
        }

        public Bridge()
        {

        }

        public Metadata Create(string filename, int nServers, int rQuorum, int wQuorum)
        {
            string primary = AskForPrimary();
            if (primary != null)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                if (server != null)
                {
                    Metadata meta = server.Create(filename, nServers, rQuorum, wQuorum);
                    return meta;
                }
            }

            return null;
        }

        public Metadata Open(string filename)
        {
            string primary = AskForPrimary();
            if (primary != null)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                if (server != null)
                {
                    Metadata meta = server.Open(filename);
                    if (meta != null)
                    {
                        Console.WriteLine(meta);
                    }
                    return meta;
                }
            }

            return null;
        }

        public void Close(string filename)
        {
            string primary = AskForPrimary();
            if (primary != null)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                if (server != null)
                {
                    server.Close(filename);
                }
            }
        }

        public void Delete(string filename)
        {
            string primary = AskForPrimary();
            if (primary != null)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                if (server != null)
                {
                    server.Delete(filename);
                }
            }
        }

        private string AskForPrimary()
        {
            foreach (string address in metadataServers.Values)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                if (server != null)
                {
                    if (server.Ping() == 1)
                    {
                        return server.GetPrimary();
                    }
                }
            }

            return null;
        }
    }
}
