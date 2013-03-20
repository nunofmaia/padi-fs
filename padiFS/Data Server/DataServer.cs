using System;
using System.IO;
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

            //create new directory
            string folderName = this.name;
            string currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + folderName;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
       
        //cria e adiciona logo ao dicionario
        public void Create(string fileName, byte[] bytearray)
        {
            File file = new File();

            file.version = DateTime.Now;
            file.content = bytearray;

            string folderName = this.name;
            string currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + folderName + @"\" + fileName + @".txt";
            Console.WriteLine(path);
            Console.WriteLine(file.GetType());
            TextWriter tw = new StreamWriter(path);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
            x.Serialize(tw,file);
            Console.WriteLine("object written to file");
            Console.ReadLine();
            tw.Close();
            
            files.Add(fileName, file);
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

        
        // Auxiliar API
        public int ping()
        {
            Console.WriteLine("I'm Alive");
            return 1;
        }

        static void Main(string[] args)
        {
            DataServer ds = new DataServer(args[0]);

           ds.Create("IurieSun",new byte[15]);

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
