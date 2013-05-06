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
        private SerializableDictionary<string, string> replicas;
        private SerializableDictionary<string, string> liveDataServers;
        private SerializableDictionary<string, string> deadDataServers;
        private SerializableDictionary<string, int> serversLoad;
        private SerializableDictionary<string, Metadata> files;
        private SerializableDictionary<string, List<string>> openFiles;

        public MetadataInfo(string primary, string address, SerializableDictionary<string, string>replicas, SerializableDictionary<string, string> liveDataServers, SerializableDictionary<string, string> deadDataServers,
            SerializableDictionary<string, int> serversLoad, SerializableDictionary<string, Metadata> files, SerializableDictionary<string, List<string>> openFiles)
        {
            this.primary = primary;
            this.address = address;
            this.replicas = new SerializableDictionary<string,string>(replicas);
            this.liveDataServers = new SerializableDictionary<string,string>(liveDataServers);
            this.deadDataServers = new SerializableDictionary<string,string>(deadDataServers);
            this.serversLoad = new SerializableDictionary<string,int>(serversLoad);
            this.files = new SerializableDictionary<string,Metadata>(files);
            this.openFiles = new SerializableDictionary<string,List<string>>(openFiles);
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

        public SerializableDictionary<string, string> Replicas
        {
            get
            {
                return replicas;
            }
        }

        public SerializableDictionary<string, string> LiveDataServers
        {
            get
            {
                return liveDataServers;
            }
        }

        public SerializableDictionary<string, string> DeadDataServers
        {
            get
            {
                return deadDataServers;
            }
        }

        public SerializableDictionary<string, int> ServersLoad
        {
            get
            {
                return serversLoad;
            }
        }

        public SerializableDictionary<string, Metadata> Files
        {
            get
            {
                return files;
            }
        }

        public SerializableDictionary<string, List<string>> OpenFiles
        {
            get
            {
                return openFiles;
            }
        }
    }
}
