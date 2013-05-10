using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class DataInfo
    {
        public SerializableDictionary<string, int> NumberAccesses { set; get; }

        public DataInfo()
        {
            this.NumberAccesses = new SerializableDictionary<string, int>();
        }

        public void AddFile(string fileName)
        {
            if (!this.NumberAccesses.ContainsKey(fileName))
            {
                this.NumberAccesses.Add(fileName, 0);
            }
        }

        public int GetAccesses(string fileName)
        {
            return this.NumberAccesses[fileName];
        }

        public SerializableDictionary<string, int> GetNumberAccesses()
        {
            return this.NumberAccesses;
        }

        public void AddAccess(string fileName)
        {
            if (this.NumberAccesses.ContainsKey(fileName))
            {
                this.NumberAccesses[fileName]++;
            }
            else
            {
                this.NumberAccesses.Add(fileName, 1);
            }
        }

        //count total accesses to data server
        public int GetTotalAccesses()
        {
            int total = 0;
            foreach (KeyValuePair<string, int> accesses in this.NumberAccesses)
            {
                total = total + accesses.Value;
            }
            return total;
        }

        public void RemoveFile(string file)
        {
            this.NumberAccesses.Remove(file);
        }
    }
}