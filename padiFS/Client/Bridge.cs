using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class Bridge
    {
        private Dictionary<string, string> metadataServers;
        private string primary;

        public Dictionary<string, string> Servers
        {
            set
            {
                this.metadataServers = value;
            }
        }

        public string Primary
        {
            set
            {
                this.primary = value;
            }
        }

        public Bridge()
        {

        }

        public Metadata Create(string filename, int nServers, int rQuorum, int wQuorum)
        {
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
            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

            if (server != null)
            {
                Metadata meta = server.Open(filename);
                Console.WriteLine(meta);
                return meta;
            }

            return null;
        }

        public void Close(string filename)
        {
            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

            if (server != null)
            {
                server.Close(filename);
            }
        }

        public void Delete(string filename)
        {
            IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

            if (server != null)
            {
                server.Delete(filename);
            }
        }
    }
}
