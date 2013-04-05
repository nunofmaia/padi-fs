using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    public interface ICommand
    {
        object execute(IClient client, string[] args);
        object execute(IMetadataServer metadata, string[] args);
        object execute(IDataServer data, string[] args);
    }

    public interface ICommander
    {
        object execute(ICommand command, string[] args);
    }

    public class DumpCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                return client.Dump();
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            if (metadata != null)
            {
                return metadata.Dump();
            }

            return null;
        }

        public object execute(IDataServer data, string[] args)
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
        public object execute(IClient client, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            if (metadata != null)
            {
                metadata.Fail();
            }

            return null;
        }

        public object execute(IDataServer data, string[] args)
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
        public object execute(IClient client, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            if (metadata != null)
            {
                metadata.Recover();
            }

            return null;
        }

        public object execute(IDataServer data, string[] args)
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
        public object execute(IClient client, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
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
        public object execute(IClient client, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
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
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string filename = args[2];
                int nServers = int.Parse(args[3]);
                int rQuorum = int.Parse(args[4]);
                int wQuorum = int.Parse(args[5]);

                client.Create(filename, nServers, rQuorum, wQuorum); 
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class OpenCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string filename = args[2];

                client.Open(filename);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class ReadCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string fileRegister = args[2];
                string semantics = args[3];
                string register = args[4];

                client.Read(fileRegister, semantics, register);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class WriteCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string fileRegister = args[2];
                string source = Util.MakeStringFromArray(args, 3);

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

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class CloseCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string filename = args[2];

                client.Close(filename);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class DeleteCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string filename = args[2];

                client.Delete(filename);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class CopyCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class ExeScriptCommand : ICommand
    {
        public object execute(IClient client, string[] args)
        {
            if (client != null)
            {
                string filename = args[2];
                string path = Environment.CurrentDirectory + @"\Scripts\" + filename;

                client.ExeScript(path);
            }

            return null;
        }

        public object execute(IMetadataServer metadata, string[] args)
        {
            throw new NotImplementedException();
        }

        public object execute(IDataServer data, string[] args)
        {
            throw new NotImplementedException();
        }
    }
}
