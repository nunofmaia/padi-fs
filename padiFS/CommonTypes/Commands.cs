using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace padiFS
{
    public interface ICommand
    {
        object execute(IClient client, string command);
        object execute(IMetadataServer metadata, string command);
        object execute(IDataServer data, string command);
    }

    public interface ICommander
    {
        object execute(ICommand command, string command_string);
    }

    public class DumpCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                return client.Dump();
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            if (metadata != null)
            {
                return metadata.Dump();
            }

            return null;
        }

        public object execute(IDataServer data, string command)
        {
            if (data != null)
            {
                return data.Dump();
            }

            return null;
        }
    }

    public class FailCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string command)
        {
            if (metadata != null)
            {
                metadata.Fail();
            }

            return null;
        }

        public object execute(IDataServer data, string command)
        {
            if (data != null)
            {
                data.Fail();
            }

            return null;
        }
    }

    public class RecoverCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string command)
        {
            if (metadata != null)
            {
                metadata.Recover();
            }

            return null;
        }

        public object execute(IDataServer data, string command)
        {
            if (data != null)
            {
                data.Recover();
            }

            return null;
        }
    }

    public class FreezeCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            if (data != null)
            {
                data.Freeze();
            }

            return null;
        }
    }

    public class UnfreezeCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            if (data != null)
            {
                data.Unfreeze();
            }

            return null;
        }
    }

    public class CreateCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string[] args = command.Replace(" ", "").Split(',');
                string filename = args[1];
                int nServers = int.Parse(args[2]);
                int rQuorum = int.Parse(args[3]);
                int wQuorum = int.Parse(args[4]);

                client.Create(filename, nServers, rQuorum, wQuorum); 
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class OpenCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string[] args = command.Replace(" ", "").Split(',');
                string filename = args[1];

                client.Open(filename);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class ReadCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string[] args = command.Replace(" ", "").Split(',');
                string fileRegister = args[1];
                string semantics = args[2];
                string register = args[3];

                client.Read(fileRegister, semantics, register);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class WriteCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string source = "";
                Match match = Regex.Match(command, "\"(.*)\"", RegexOptions.IgnoreCase);
                string[] args = command.Replace(" ", "").Split(',');
                if (match.Success)
                {
                    source = match.Groups[1].Value;
                }
                else
                {
                    source = args[2];
                }
                string fileRegister = args[1];

                int register = -1;

                if (Int32.TryParse(source, out register))
                {
                    if (register >= 0 || register <= 9)
                    {
                        client.Write(fileRegister, register);
                    }
                }
                else
                {
                    client.Write(fileRegister, source);
                }
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class CloseCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string[] args = command.Replace(" ", "").Split(',');
                string filename = args[1];

                client.Close(filename);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class DeleteCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string[] args = command.Replace(" ", "").Split(',');
                string filename = args[1];

                client.Delete(filename);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class CopyCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string source = "";
                Match match = Regex.Match(command, "\"(.*)\"", RegexOptions.IgnoreCase);
                string[] args = command.Replace(" ", "").Split(',');
                if (match.Success)
                {
                    source = match.Groups[1].Value;
                }
                else
                {
                    source = args[2];
                }
                
                int fileRegister1;
                Int32.TryParse(args[1], out fileRegister1);
                string semantics = args[2];
                int fileRegister2;
                Int32.TryParse(args[3], out fileRegister2);
                string salt = source;

                client.Copy(fileRegister1, semantics, fileRegister2, salt);
            }
            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }

    public class ExeScriptCommand : ICommand
    {
        public object execute(IClient client, string command)
        {
            if (client != null)
            {
                string[] args = command.Split(' ');
                string filename = args[2];
                DirectoryInfo dir = Directory.GetParent(Environment.CurrentDirectory);
                string path = dir.FullName + @"\Scripts\" + filename;

                client.ExeScript(path);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string command)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string command)
        {
            throw new NotImplementedException();
        }
    }
}
