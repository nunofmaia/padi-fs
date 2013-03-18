using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class MetadataServer
    {
        private string name;
        private Dictionary<string, string> metadataServers;
        private Dictionary<string, Metadata> files;

        public MetadataServer(string name)
        {
            this.name = name;
            metadataServers = new Dictionary<string, string>();
            this.files = new Dictionary<string, Metadata>();
        }

        static void Main(string[] args)
        {
            MetadataServer ms = new MetadataServer(args[0]);
            // Ficar esperar pedidos de Iurie
        }
    }
}
