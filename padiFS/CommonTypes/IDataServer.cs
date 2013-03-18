using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    interface IDataServer
    {
        File Read(string localFile, string semantics);
        int Write(string localFile, byte[] bytearray);
        void Freeze();
        void Unfreeze();
        void Fail();
        void Recover();
    }
}
