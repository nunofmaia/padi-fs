using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public class Client
    {
        private string name;
        public Client(string name)
        {
            this.name = name;
        }

        static void Main(string[] args)
        {
            Client c = new Client(args[0]);
            // Fazer coisas que Iuri mandar
        }
    }
}
