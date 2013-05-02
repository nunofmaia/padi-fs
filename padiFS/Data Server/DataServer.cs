using System;
using System.IO;
using System.Collections.Generic;using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

namespace padiFS
{
    public class DataServer : MarshalByRefObject, IDataServer
    {
        private static TcpChannel channel;
        private string name;
        private int port;

        private DataState state;

        private string currentDir;
        private List<string> files;
        private DataInfo dataInfo;
        private ManualResetEvent freeze;

        public DataServer(string name, string port)
        {
            this.name = name;
            this.port = int.Parse(port);
            this.state = new NormalState();
            this.freeze = new ManualResetEvent(false);
            this.files = new List<string>();
            freeze.Set();

            //create new directory
            this.currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + this.name;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void AddFile(string s)
        {
            if (!files.Contains(s))
            {
                files.Add(s);
            }
        }

        public void RemoveFile(string s)
        {
            files.Remove(s);
        }
        
        protected void setStateFail()
        {
            this.state = new FailedState();
        }

        protected void setStateNormal()
        {
            this.state = new NormalState();
        }

        public ManualResetEvent GetFreeze 
        {
            get { return this.freeze; }
        }

        public string CurrentDir 
        {
            get { return currentDir;}
            set { currentDir = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public DataInfo DataInfo 
        {
            get { return dataInfo; }
            set { dataInfo = value; }
        }

        //Project API
        public void Create(string fileName)
        {
            this.state.Create(this, fileName);
        }

        public File Read(string localFile, string semantics)
        {
            return this.state.Read(this, localFile, semantics);
        }


        public int Write(string localFile, byte[] bytearray)
        {
            return this.state.Write(this, localFile, bytearray);
        }

        // Puppet Master Commands
        public void Freeze()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
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
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            this.setStateFail();
            
            Console.WriteLine("On Failure!");
        }
        public void Recover()
        {
            this.setStateNormal();
            Console.WriteLine("Uhf, recovered at last...");
        }

        
        // Auxiliar API
        public DataInfo Ping()
        {
            return this.state.Ping(this);
        }

        public string Dump()
        {
            string s = "Data Server " + name + " dump:\r\nFiles:\r\n";
            foreach(string file in files){
                s += "\t" + file + "\r\n";
            }
            return s;
        }

        public override object InitializeLifetimeService()
        {
            return null;
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
            channel = new TcpChannel(ds.port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(ds, ds.name, typeof(DataServer));
            //IPuppetMaster master = (IPuppetMaster)Activator.GetObject(typeof(IPuppetMaster), "tcp://localhost:8070/PuppetMaster");
            //if (master != null)
            //{
            //    try
            //    {
            //        master.test(ds.name);
            //    }
            //    catch (RemotingException e)
            ////    { Console.WriteLine(e.StackTrace); }
            //}
            //else
            //{
            //    Console.WriteLine(ds.name);
            //}
            Console.ReadLine();
        }
    }
}
