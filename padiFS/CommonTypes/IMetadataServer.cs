using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public interface IMetadataServer
    {
        // Project API
        Metadata Open(string filename);
        void Close();
        Metadata Create(string filename, int serversNumber, int readQuorum, int writeQuorum);
        void Delete();
        void Fail();
        void Recover();

        // Auxiliar API
        void RegisterMetadataServer(string name, string address);
        void RegisterDataServer(string name, string address);
    }
}
