using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class Metadata
    {
        private string filename;
        private int serversNumber;
        private int readQuorum;
        private int writeQuorum;
        private List<string> dataServers;

        public Metadata(string filename, int serversNumber, int readQuorum, int writeQuorum, List<string> dataServers)
        {
            this.filename = filename;
            this.serversNumber = serversNumber;
            this.readQuorum = readQuorum;
            this.writeQuorum = writeQuorum;
            this.dataServers = dataServers;
        }

        public string FileName
        {
            get { return filename; }
        }

        public int ServersNumber
        {
            get { return serversNumber; }
        }

        public int ReadQuorum
        {
            get { return readQuorum; }
        }

        public int WriteQuorum
        {
            get { return writeQuorum; }
        }

        public List<string> DataServers
        {
            get { return dataServers; }
        }

        public override string ToString()
        {
            return "File: " + filename + " #Servers: " + serversNumber + " RQ: " + readQuorum + " WQ: " + writeQuorum;
        }
    }
}
