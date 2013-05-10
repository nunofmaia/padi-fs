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

        public Bridge()
        {

        }

        public Metadata Create(string clientName, string filename, int nServers, int rQuorum, int wQuorum)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary = AskForPrimary();
                    if (primary != null)
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                        if (server != null)
                        {
                            Metadata meta = server.Create(clientName, filename, nServers, rQuorum, wQuorum);
                            return meta;
                        }
                    }
                }
                catch (SystemException)
                {
                    primary = null;
                }
            }

            return null;
        }

        public Metadata Open(string clientName, string filename)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary = AskForPrimary();
                    if (primary != null)
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                        if (server != null)
                        {
                            Metadata meta = server.Open(clientName, filename);
                            if (meta != null)
                            {
                                Console.WriteLine(meta);
                            }
                            return meta;
                        }
                    }
                }
                catch (SystemException)
                {
                    primary = null;
                }
            }

            return null;
        }

        public void Close(string clientName, string filename)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary = AskForPrimary();
                    if (primary != null)
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                        if (server != null)
                        {
                            server.Close(clientName, filename);
                            executed = true;
                        }
                    }
                }
                catch (SystemException)
                {
                    primary = null;
                }
            }
        }

        public void Delete(string clientName, string filename)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary = AskForPrimary();
                    if (primary != null)
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                        if (server != null)
                        {
                            server.Delete(clientName, filename);
                            executed = true;
                        }
                    }
                }
                catch (SystemException)
                {
                    primary = null;
                }
            }
        }

        public long GetToken()
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    string primary = AskForPrimary();
                    if (primary != null)
                    {
                        IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), (string)metadataServers[primary]);

                        if (server != null)
                        {
                            return server.GetToken();
                        }
                    }
                }
                catch (SystemException)
                {
                    primary = null;
                }
            }

            return 0;
        }

        private string AskForPrimary()
        {
            if (primary != null)
            {
                return primary;
            }

            foreach (string address in metadataServers.Values)
            {
                IMetadataServer server = (IMetadataServer)Activator.GetObject(typeof(IMetadataServer), address);
                if (server != null)
                {
                    try
                    {
                        primary = server.GetPrimary();
                        return primary;
                    }
                    catch (Exception) { }
                }
            }

            return null;
        }
    }
}
