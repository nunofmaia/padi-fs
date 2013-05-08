using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public interface IDataServer
    {
        File Read(string localFile, string semantics);
        int Write(string localFile, byte[] bytearray);
        void Create(string fileName);
        void Freeze();
        void Unfreeze();
        void Fail();
        void Recover();
        string Dump();

        // Auxiliar API
        DataInfo Ping();
        void RemoveFromDataInfo(string file);
        void RestoreFiles();
    }
}
