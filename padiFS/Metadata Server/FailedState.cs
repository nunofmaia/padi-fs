using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;


namespace padiFS
{
    class FailedState : MetadataState 
    {
        public override Metadata Open(MetadataServer md, string clientName, string filename)
        { return null; }
        public override void Close(MetadataServer md, string clientName, string filename) { }
        public override Metadata Create(MetadataServer md, string clientName,
                                string filename,
                                int serversNumber,
                                int readQuorum,
                                int writeQuorum)
        {
            return null;
        }

        public override void Delete(MetadataServer md, string clientName, string filename) { }

        public override int Ping()
        {
            return 0;
        }

        public override void RegisterMetadataServer(MetadataServer md, string name, string address) { }

        public override void pingDataServers(MetadataServer md, object source, ElapsedEventArgs e) { }

        public override void PingPrimaryReplica(MetadataServer md, object source, ElapsedEventArgs e) { }
    }
}
