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

        private bool onFailure;
        private ManualResetEvent freeze;

        public DataServer(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);
            this.onFailure = false;
            this.freeze = new ManualResetEvent(false);
            freeze.Set();

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

                file.Version = DateTime.Now;
                file.Content = new byte[1];

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

        public File Read(string localFile, string semantics)
        {
            if (!onFailure)
            {
                freeze.WaitOne();

                File file = new File();
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
                return file;
            }
            return null;
        }


        public int Write(string localFile, byte[] bytearray)
        {
            if (!onFailure)
            {
                freeze.WaitOne();

                string[] content = Util.SplitArguments(Util.ConvertByteArrayToString(bytearray));
                string date = content[0];
                byte[] bytes = Util.ConvertStringToByteArray(content[1]);

                File file = new File();


                file.Version = Convert.ToDateTime(date);
                file.Content = bytes;

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
                    //success
                    return 0;
                }
                else
                {
                    //isto TEM DE SER MUDADO
                    Console.WriteLine("O ficheiro não existe");
                }
            }
            //failure
            return -1;
        }

        // Puppet Master Commands
        public void Freeze()
        {
            freeze.Reset();
            Console.WriteLine("Freezed!");
        }
        public void Unfreeze()
        {
            freeze.Set();
            Console.WriteLine("Defrosting!");
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
            if (!onFailure)
            {
                // So Dirty
                while (onFreeze)
                { }

                Console.WriteLine("I'm Alive");
                return 1;
            }
            else { return 0; }
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
