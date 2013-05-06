using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class Metadata
    {
        public string Filename { set; get; }
        public int ServersNumber { set; get; }
        public int ReadQuorum { set; get; }
        public int WriteQuorum { set; get; }
        public List<string> DataServers { set; get; }

        public Metadata()
        {
        }

        public Metadata(string filename, int serversNumber, int readQuorum, int writeQuorum, List<string> dataServers)
        {
            this.Filename = filename;
            this.ServersNumber = serversNumber;
            this.ReadQuorum = readQuorum;
            this.WriteQuorum = writeQuorum;
            this.DataServers = dataServers;
        }

        public void AddDataServers(string address)
        {
            this.DataServers.Add(address);
        }

        public override string ToString()
        {
            return "File: " + this.Filename + " #Servers: " + this.ServersNumber + " RQ: " + this.ReadQuorum + " WQ: " + this.WriteQuorum;
        }
    }
}
