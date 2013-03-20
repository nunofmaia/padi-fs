using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class File
    {
        public DateTime version;
        public byte[] content;

        public File() { }
        public File(DateTime version, byte[] content)
        {
            this.version = version;
            this.content = content;
        }
    }
}
