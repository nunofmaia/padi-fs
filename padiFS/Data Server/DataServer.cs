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

        private string currentDir;

        public DataServer(string id)
        {
            this.name = "d-" + id;
            this.port = 8090 + int.Parse(id);

            //create new directory
            this.currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + this.name;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
       
        public void Create(string fileName)
        {
            File file = new File();

            file.version = DateTime.Now;
            file.content = new byte[1];

            this.currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + this.name + @"\" + fileName + @".txt";

            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine(path);
                Console.WriteLine(file.GetType());
                TextWriter tw = new StreamWriter(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
                x.Serialize(tw, file);
                Console.WriteLine("object written to file");
                tw.Close();
            }
            else {
                //isto TEM DE SER MUDADO
                Console.WriteLine("Já existe um ficheiro com esse nome");
            }
        }

        public File Read(string localFile, string semantics)
        {
            File file = new File();
            string path = currentDir + @"\" + this.name + @"\" + localFile + ".txt";
            Console.WriteLine(path);
            if (System.IO.File.Exists(path))
            {
                TextReader tr = new StreamReader(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
                file = (File)x.Deserialize(tr);
                tr.Close();
                return file;
            }
            else {
                //isto TEM DE SER MUDADO
                Console.WriteLine("O ficheiro não existe");
                }
            return null;
            
        }
        public int Write(string localFile, byte[] bytearray)
        {
            File file = new File();

            file.version = DateTime.Now;
            file.content = bytearray;

            this.currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + this.name + @"\" + localFile + @".txt";

            if (System.IO.File.Exists(path))
            {
                Console.WriteLine(path);
                Console.WriteLine(file.GetType());
                TextWriter tw = new StreamWriter(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
                x.Serialize(tw, file);
                Console.WriteLine("object written to file");
                tw.Close();
            }
            else
            {
                //isto TEM DE SER MUDADO
                Console.WriteLine("O ficheiro não existe");
            }
            
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

            //teste
            //ds.Create("Iuriesun");
            //ds.Write("Iuriesun", new byte[4]);
            //Console.WriteLine(ds.Read("iuriesun", "cena").version);
            //Console.WriteLine(ds.Read("iuriesun", "cena").content);
            
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
