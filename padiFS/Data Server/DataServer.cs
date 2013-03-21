using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

namespace padiFS
{
    public class DataServer : MarshalByRefObject, IDataServer
    {
        private string name;
        private int port;

        private string currentDir;

        private bool onFreeze = false;
        private bool onFailure = false;

        public DataServer(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);

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
            if (!onFailure)
            {
                File file = new File();

                file.version = DateTime.Now;
                file.content = new byte[1];

                this.currentDir = Environment.CurrentDirectory;
                string path = currentDir + @"\" + this.name + @"\" + fileName + @".txt";


                Console.WriteLine(path);
                Console.WriteLine(file.GetType());
                TextWriter tw = new StreamWriter(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
                x.Serialize(tw, file);
                Console.WriteLine("object written to file");
                tw.Close();
            }
        }

        private void ReadCallback(object threadcontext)
        {
            List<object> args = (List<object>)threadcontext;
            string localFile = (string)args[0];
            string semantics = (string)args[1];
            File file = (File)args[2];
            string path = currentDir + @"\" + this.name + @"\" + localFile + ".txt";
            Console.WriteLine(path);
            if (System.IO.File.Exists(path))
            {
                TextReader tr = new StreamReader(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
                file = (File)x.Deserialize(tr);
                tr.Close();
            }
            else
            {
                //isto TEM DE SER MUDADO
                Console.WriteLine("O ficheiro não existe");
            }

        }
        public File Read(string localFile, string semantics)
        {
            if (!onFailure)
            {

                File file = new File();
                List<object> arguments = new List<object>();
                arguments.Add(localFile);
                arguments.Add(semantics);
                arguments.Add(file);
                ThreadPool.QueueUserWorkItem(ReadCallback, arguments);
                return file;
            }
            return null;
        }
        public int Write(string localFile, byte[] bytearray)
        {
            if (!onFailure)
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
            }
            return 0;
        }

        // Puppet Master Commands
        public void Freeze()
        {
            onFreeze = true;
        }
        public void Unfreeze()
        {
            onFreeze = false;
            // Lançar threads para responder a pedidos
        }
        public void Fail()
        {
            onFailure = true;
            Console.WriteLine("On Failure!");
        }
        public void Recover()
        {
            onFailure = false;
            Console.WriteLine("Uhf, recovered at last...");

        }

        
        // Auxiliar API
        public int ping()
        {
            Console.WriteLine("I'm Alive");
            return 1;
        }

        static void Main(string[] args)
        {
            string[] arguments = Util.SplitArguments(args[0]);
            DataServer ds = new DataServer(arguments[0], arguments[1]);
            Console.Title = "Iurie's Data Server: " + ds.name;

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
