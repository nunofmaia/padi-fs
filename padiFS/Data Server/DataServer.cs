using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace padiFS
{
    public class DataServer : MarshalByRefObject, IDataServer
    {
        private string name;
        private int port;
        private Dictionary<string, File> files;

        public DataServer(string id)
        {
            this.name = "d-" + id;
            this.port = 8090 + int.Parse(id);
            files = new Dictionary<string, File>();
        }

        public File Read(string localFile, string semantics)
        {
            return null;
        }
        public int Write(string localFile, byte[] bytearray)
        {
            return 0;
        }
        public void Freeze() { }
        public void Unfreeze() { }
        public void Fail() { }
        public void Recover() { }


        static void Main(string[] args)
        {
            DataServer ds = new DataServer(args[0]);
            // Ficar esperar pedidos de Iurie
            TcpChannel channel = new TcpChannel(ds.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(ds, ds.name, typeof(DataServer));
            IPuppetMaster master = (IPuppetMaster)Activator.GetObject(typeof(IPuppetMaster), "tcp://localhost:8070/PuppetMaster");
            if (master != null)
            {
                try
                {
                    master.test(ds.name);
                }
                catch (RemotingException e)
                { Console.WriteLine(e.StackTrace); }
            }
            else
            {
                Console.WriteLine(ds.name);
            }
            Console.ReadLine();
        }
    }
}
