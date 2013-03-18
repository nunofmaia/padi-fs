using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class File
    {
        private DateTime version;
        private byte[] content;

        public File(DateTime version, byte[] content)
        {
            this.version = version;
            this.content = content;
        }
    }
}
