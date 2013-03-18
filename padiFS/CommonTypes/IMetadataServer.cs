using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    interface IMetadataServer
    {
        Metadata Open(string filename);
        void Close();
        Metadata Create(string filename, int serversNumber, int readQuorum, int writeQuorum);
        void Delete();
        void Fail();
        void Recover();
    }
}
