using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class File
    {
        private long version;
        private byte[] content;

        public byte[] Content
        {
            get { return content; }
            set { content = value; }
        }

        public long Version
        {
            get { return version; }
            set { version = value; }
        }

        public File()
        {
            this.version = 0;
        }
        public File(long version, byte[] content)
        {
            this.version = version;
            this.content = content;
        }
    }
}
