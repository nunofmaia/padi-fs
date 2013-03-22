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
        private DateTime version;
        private byte[] content;

        public byte[] Content
        {
            get { return content; }
            set { content = value; }
        }

        public DateTime Version
        {
            get { return version; }
            set { version = value; }
        }

        public File() { }
        public File(DateTime version, byte[] content)
        {
            this.version = version;
            this.content = content;
        }
    }
}
