﻿using System;
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

        public int getAcesses(string fileName) 
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

        //count total acesses to data server
        public int getTotalAcesses() {
            int total = 0;
            foreach (KeyValuePair<string, int> acesses in numberAcesses)
            {
                total = total + acesses.Value;
            }
            return total;
        }
    }
}