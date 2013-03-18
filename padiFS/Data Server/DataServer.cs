using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class DataServer
    {
        private string name;
        private Dictionary<string, File> files;

        public DataServer(string name)
        {
            this.name = name;
            files = new Dictionary<string, File>();
        }
        static void Main(string[] args)
        {
            DataServer ds = new DataServer(args[0]);
            // Ficar esperar pedidos de Iurie
            Console.WriteLine(ds.name);
            Console.ReadLine();
        }
    }
}
