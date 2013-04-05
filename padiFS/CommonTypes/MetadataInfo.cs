using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class MetadataInfo
    {
        private string primary;
        private string address;
        private Dictionary<string, string> replicas;
        private Dictionary<string, string> liveDataServers;
        private Dictionary<string, string> deadDataServers;
        private Dictionary<string, int> serversLoad;
        private Dictionary<string, Metadata> files;
        private Dictionary<string, List<string>> openFiles;

        public MetadataInfo(string primary, string address, Dictionary<string, string>replicas, Dictionary<string, string> liveDataServers, Dictionary<string, string> deadDataServers,
            Dictionary<string, int> serversLoad, Dictionary<string, Metadata> files, Dictionary<string, List<string>> openFiles)
        {
            this.primary = primary;
            this.address = address;
            this.replicas = new Dictionary<string,string>(replicas);
            this.liveDataServers = new Dictionary<string,string>(liveDataServers);
            this.deadDataServers = new Dictionary<string,string>(deadDataServers);
            this.serversLoad = new Dictionary<string,int>(serversLoad);
            this.files = new Dictionary<string,Metadata>(files);
            this.openFiles = new Dictionary<string,List<string>>(openFiles);
        }

        public string Primary
        {
            get
            {
                return primary;
            }
        }

        public string Address
        {
            get
            {
                return address;
            }
        }

        public Dictionary<string, string> Replicas
        {
            get
            {
                return replicas;
            }
        }

        public Dictionary<string, string> LiveDataServers
        {
            get
            {
                return liveDataServers;
            }
        }

        public Dictionary<string, string> DeadDataServers
        {
            get
            {
                return deadDataServers;
            }
        }

        public Dictionary<string, int> ServersLoad
        {
            get
            {
                return serversLoad;
            }
        }

        public Dictionary<string, Metadata> Files
        {
            get
            {
                return files;
            }
        }

        public Dictionary<string, List<string>> OpenFiles
        {
            get
            {
                return openFiles;
            }
        }
    }
}
