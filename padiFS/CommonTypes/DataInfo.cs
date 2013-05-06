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

        public void AddFile(string fileName)
        {
            if(!numberAccesses.ContainsKey(fileName)) {
               this.numberAccesses.Add(fileName, 0);
            }
        }

        public int GetAccesses(string fileName) 
        {
            return numberAccesses[fileName]; 
        }

        public Dictionary<string, int> GetNumberAccesses() 
        {
            return this.numberAccesses;
        }

        public void AddAccess(string fileName)
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
        public int GetTotalAccesses() {
            int total = 0;
            foreach (KeyValuePair<string, int> accesses in numberAccesses)
            {
                total = total + accesses.Value;
            }
            return total;
        }

        public void RemoveFile(string file)
        {
            this.numberAccesses.Remove(file);
        }
    }
}