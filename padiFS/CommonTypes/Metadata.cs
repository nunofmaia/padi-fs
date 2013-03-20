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
        private Dictionary<string, string> dataServers;

        public Metadata(string filename, int serversNumber, int readQuorum, int writeQuorum, Dictionary<string, string> dataServers)
        {
            this.filename = filename;
            this.serversNumber = serversNumber;
            this.readQuorum = readQuorum;
            this.writeQuorum = writeQuorum;
            this.dataServers = dataServers;
        }
    }
}
