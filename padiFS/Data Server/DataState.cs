﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO;
using System.Globalization;

namespace padiFS
{
    abstract class DataState
    {
        public abstract void Create(DataServer ds, string fileName);
        public abstract File Read(DataServer ds, string localFile, string semantics);
        public abstract int Write(DataServer ds, string localFile, byte[] bytearray);
        public abstract DataInfo Ping(DataServer ds); 
    }

    class NormalState : DataState
    {
        public override void Create(DataServer ds, string fileName)
        {
            ds.GetFreeze.WaitOne();

            File file = new File();
            string[] args = Util.SplitArguments(fileName);

            file.Version = Convert.ToInt64(args[0]);
            file.Content = new byte[1];

            ds.CurrentDir = Environment.CurrentDirectory;
            string path = ds.CurrentDir + @"\" + ds.Name + @"\" + args[1] + @".txt";


            Console.WriteLine("Create: " + path);

            Util.SerializeFile(path, file);
            ds.AddFile(args[1]);

            //add file to datainfo
            ds.DataInfo.AddFile(args[1]);
        }
        public override File Read(DataServer ds, string localFile, string semantics)
        {
            ds.GetFreeze.WaitOne();

            File file = new File();
            string path = ds.CurrentDir + @"\" + ds.Name + @"\" + localFile + ".txt";
            Console.WriteLine("Read: " + path);

            if (System.IO.File.Exists(path))
            {
                file = Util.DeserializeFile(path, file);
            }
            else
            {
                throw new FileNotFoundException("File: " + localFile + " not found in server " + ds.Name);
            }
            //add access to this file
            ds.DataInfo.AddAccess(localFile);

            return file;
        }
        public override int Write(DataServer ds, string localFile, byte[] bytearray)
        {
            ds.GetFreeze.WaitOne();

            string[] content = Util.SplitArguments(Util.ConvertByteArrayToString(bytearray));
            string version = content[0];
            byte[] bytes = Util.ConvertStringToByteArray(content[1]);

            File newFile = new File();
            File oldFile = new File();

            string path = ds.CurrentDir + @"\" + ds.Name + @"\" + localFile + ".txt";
            if (System.IO.File.Exists(path))
            {
                oldFile = Util.DeserializeFile(path, oldFile);
            }

            if (oldFile.Version < Convert.ToInt64(version))
            {
                newFile.Version = Convert.ToInt64(version);
                newFile.Content = bytes;
                ds.CurrentDir = Environment.CurrentDirectory;
                string readPath = ds.CurrentDir + @"\" + ds.Name + @"\" + localFile + @".txt";

                Console.WriteLine("Write: " + readPath);
                Util.SerializeFile(readPath, newFile);
                ds.AddFile(localFile);

                //add access to this file
                ds.DataInfo.AddAccess(localFile);
            }

            //success
            return 0;
        }

        public override DataInfo Ping(DataServer ds)
        {
            ds.GetFreeze.WaitOne();

            Console.WriteLine("I'm Alive");
            return ds.DataInfo;
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
        public override DataInfo Ping(DataServer ds)
        {
            throw new ServerNotAvailableException("The server " + ds.Name + " is not available.");
        }
    }
}
