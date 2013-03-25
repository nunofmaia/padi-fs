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
        private Dictionary<string, string> liveDataServers;
        private Dictionary<string, string> deadDataServers;
        private Dictionary<string, int> serversLoad;
        private Dictionary<string, Metadata> files;
        private Dictionary<string, Metadata> openFiles;

        public MetadataInfo(string primary, Dictionary<string, string> liveDataServers, Dictionary<string, string> deadDataServers,
            Dictionary<string, int> serversLoad, Dictionary<string, Metadata> files, Dictionary<string, Metadata> openFiles)
        {
            this.primary = primary;
            this.liveDataServers = liveDataServers;
            this.deadDataServers = deadDataServers;
            this.serversLoad = serversLoad;
            this.files = files;
            this.openFiles = openFiles;
        }

        public string Primary
        {
            get
            {
                return primary;
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

        public Dictionary<string, Metadata> OpenFiles
        {
            get
            {
                return openFiles;
            }
        }
    }
}
