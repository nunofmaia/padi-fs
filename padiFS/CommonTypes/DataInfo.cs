using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class DataInfo
    {
        private Dictionary<string, int> numberAcesses;

        public DataInfo() 
        {
            this.numberAcesses = new Dictionary<string, int>();
        }

        public void addFile(string fileName)
        {
            this.numberAcesses.Add(fileName, 0);   
        }


        public int getNumberAcesses(string fileName) 
        {
            return numberAcesses[fileName]; 
        }

        public Dictionary<string, int> getNumberAcesses() 
        {
            return this.numberAcesses;
        }

        public void addAcess(string fileName)
        {
            this.numberAcesses[fileName]++;
        }
    //acabar os gets e sets

        //get que conta os acessos totais de cada dataserver    
    }
}
