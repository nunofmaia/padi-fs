using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public interface IClient
    {
        void Read(string filename, string semantic);
        void Read(string file, string semantic, string register);
        void Write(string filename, byte[] bytearray);
        void Write(string file, int register);
        void Write(string file, string content);
        void Open(string filename);
        void Create(string filename, int serversNumber, int readQuorum, int writeQuorum);
        void Close(string filename);
        void Delete(string filename);
        string Dump();
        void ExeScript(string path);
        void Copy(int file1, string semantics, int file2, string salt);

        void UpdateServers(Dictionary<string, string> servers);
        //void UpdateFileMetadata(string filename, Metadata metadata);
    }
}
