using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO;

namespace padiFS
{
    abstract class DataState
    {
        public abstract void Create(DataServer ds, string fileName);
        public abstract File Read(DataServer ds, string localFile, string semantics);
        public abstract int Write(DataServer ds, string localFile, byte[] bytearray);
        public abstract bool Ping(DataServer ds); 
    }

    class NormalState : DataState
    {
        public override void Create(DataServer ds, string fileName)
        {
            ds.GetFreeze.WaitOne();

            File file = new File();
            string[] args = Util.SplitArguments(fileName);

            file.Version = Convert.ToDateTime(args[0]);
            file.Content = new byte[1];

            ds.CurrentDir = Environment.CurrentDirectory;
            string path = ds.CurrentDir + @"\" + ds.Name + @"\" + args[1] + @".txt";


            Console.WriteLine(path);
            TextWriter tw = new StreamWriter(path);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(file.GetType());
            x.Serialize(tw, file);
            Console.WriteLine("object written to file");
            tw.Close();
        }
        public override File Read(DataServer ds, string localFile, string semantics)
        {
            ds.GetFreeze.WaitOne();

            File file = new File();
            string path = ds.CurrentDir + @"\" + ds.Name + @"\" + localFile + ".txt";
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
        public override int Write(DataServer ds, string localFile, byte[] bytearray)
        {
            ds.GetFreeze.WaitOne();

            string[] content = Util.SplitArguments(Util.ConvertByteArrayToString(bytearray));
            string date = content[0];
            byte[] bytes = Util.ConvertStringToByteArray(content[1]);

            File newFile = new File();
            File oldFile = new File();

            string path = ds.CurrentDir + @"\" + ds.Name + @"\" + localFile + ".txt";
            if (System.IO.File.Exists(path))
            {
                TextReader tr = new StreamReader(path);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(oldFile.GetType());
                oldFile = (File)x.Deserialize(tr);
                tr.Close();
            }

            if (oldFile.Version < Convert.ToDateTime(date))
            {
                newFile.Version = Convert.ToDateTime(date);
                newFile.Content = bytes;
                ds.CurrentDir = Environment.CurrentDirectory;
                string readPath = ds.CurrentDir + @"\" + ds.Name + @"\" + localFile + @".txt";

                if (System.IO.File.Exists(readPath))
                {
                    Console.WriteLine(readPath);
                    TextWriter tw = new StreamWriter(readPath);
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(newFile.GetType());
                    x.Serialize(tw, newFile);
                    Console.WriteLine("object written to file");
                    tw.Close();
                }
                else
                {
                    //isto TEM DE SER MUDADO
                    Console.WriteLine("O ficheiro não existe");
                }
            }
            //success
            return 0;
        }
        public override bool Ping(DataServer ds)
        {
            ds.GetFreeze.WaitOne();

            Console.WriteLine("I'm Alive");
            return true;
        }
    }

    class FailedState : DataState
    {
        public override void Create(DataServer ds, string fileName)
        { }
        public override File Read(DataServer ds, string localFile, string semantics)
        {
            return null;
        }
        public override int Write(DataServer ds, string localFile, byte[] bytearray)
        {
            //failure
            return -1;    
        }
        public override bool Ping(DataServer ds)
        {
            throw new ServerNotAvailableException("The server is on fail mode.");
        }
    }
}
