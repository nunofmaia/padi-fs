using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public interface IClient
    {
        void Read(string filename);
        void Write(string filename);
        void Open(string filename);
        void Create(string filename, int serversNumber, int readQuorum, int writeQuorum);
        void Close(string filename);
        void Delete(string filename);

        void UpdateServers(Dictionary<string, string> servers);
    }
}
