using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace padiFS
{
    abstract class MetadataState
    {
        public abstract Metadata Open(MetadataServer md, string filename);
        public abstract void Close(MetadataServer md, string filename);
        public abstract Metadata Create(MetadataServer md, string filename, int serversNumber, int readQuorum, int writeQuorum);
        public abstract void Delete(MetadataServer md, string filename);
        public abstract int Ping();
        public abstract void RegisterMetadataServer(MetadataServer md, string name, string address);
        public abstract void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e);
        public abstract void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e);
    }
}
