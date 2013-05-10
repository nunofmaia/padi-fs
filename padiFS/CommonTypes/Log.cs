using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace padiFS
{
    [Serializable]
    public class Log
    {
        public int Index { set; get; }
        public string Path { set; get; }

        private Object thisLock = new Object();

        public Log()
        {
        }

        public Log(string path)
        {
            this.Index = -1;
            this.Path = path;
        }

        public void Append(string command)
        {
            lock (thisLock)
            {
                using (StreamWriter sw = System.IO.File.AppendText(this.Path))
                {
                    sw.WriteLine(command);
                    this.Index = this.Index + 1;
                }
            }
        }

        public string[] Read(int offset)
        {
            List<string> commands = new List<string>();

            lock (thisLock)
            {
                using (StreamReader sr = new StreamReader(this.Path))
                {
                    for (int i = 0; i < offset; i++)
                    {
                        sr.ReadLine();
                    }

                    while (!sr.EndOfStream)
                    {
                        commands.Add(sr.ReadLine());
                    }
                }
            }
            return commands.ToArray();
        }
    }
}
