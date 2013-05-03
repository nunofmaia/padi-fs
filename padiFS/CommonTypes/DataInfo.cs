using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class DataInfo
    {
        private Dictionary<string, int> numberAccesses;

        public DataInfo() 
        {
            this.numberAccesses = new Dictionary<string, int>();
        }

        public void addFile(string fileName)
        {
            this.numberAccesses.Add(fileName, 0);   
        }

        public int getAccesses(string fileName) 
        {
            return numberAccesses[fileName]; 
        }

        public Dictionary<string, int> getNumberAccesses() 
        {
            return this.numberAccesses;
        }

        public void addAccess(string fileName)
        {
            if (numberAccesses.ContainsKey(fileName))
            {
                this.numberAccesses[fileName]++;
            }
            else 
            {
                this.numberAccesses.Add(fileName, 1);
            }
        }

        //count total accesses to data server
        public int getTotalAccesses() {
            int total = 0;
            foreach (KeyValuePair<string, int> accesses in numberAccesses)
            {
                total = total + accesses.Value;
            }
            return total;
        }
    }
}