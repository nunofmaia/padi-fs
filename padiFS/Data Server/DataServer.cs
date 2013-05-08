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
        private static TcpChannel Channel { set; get; }
        public string Name { set; get; }
        public int Port { set; get; }

        private DataState state;

        private string currentDir;
        public List<string> Files { set; get; }
        private DataInfo dataInfo;
        private ManualResetEvent freeze;

        public DataServer(string name, string port)
        {
            this.Name = name;
            this.Port = int.Parse(port);
            this.state = new NormalState();
            this.freeze = new ManualResetEvent(false);
            this.Files = new List<string>();
            this.dataInfo = new DataInfo();
            freeze.Set();

            //create new directory
            this.currentDir = Environment.CurrentDirectory;
            string path = currentDir + @"\" + this.Name;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void AddFile(string s)
        {
            if (!this.Files.Contains(s))
            {
                this.Files.Add(s);
            }
        }

        public void RemoveFile(string s)
        {
            this.Files.Remove(s);
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

        public DataInfo DataInfo 
        {
            get { return dataInfo; }
            set { dataInfo = value; }
        }

        public void RemoveFromDataInfo(string file)
        {
            this.dataInfo.RemoveFile(file);
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
            RestoreFiles();
            Console.WriteLine("Uhf, recovered at last...");
        }

        public void RestoreFiles()
        {
            string path = Environment.CurrentDirectory + string.Format(@"\{0}", this.Name);
            this.Files = Util.GetFileNamesFromDirectory(path).ToList();
        }

        
        // Auxiliar API
        public DataInfo Ping()
        {
            return this.state.Ping(this);
        }

        public string Dump()
        {
            string s = "Data Server " + this.Name + " dump:\r\nFiles:\r\n";
            foreach(string file in this.Files){
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
            Console.Title = "Iurie's Data Server: " + ds.Name;
            
            // Ficar esperar pedidos de Iurie
            Channel = new TcpChannel(ds.Port);
            ChannelServices.RegisterChannel(Channel, true);
            RemotingServices.Marshal(ds, ds.Name, typeof(DataServer));

            Console.ReadLine();
        }
    }
}
