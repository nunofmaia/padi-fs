using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class PadiFsException : ApplicationException
    {
    }

    public class FileNotFoundException : PadiFsException
    {
    }

    public class ServerNotAvailable : PadiFsException
    {
    }

    public class FileAlreadyOpen : PadiFsException
    {
    }

    public class FileAlreadyClose : PadiFsException
    {
    }

    public class ServerNotFound : PadiFsException
    {
    }
}
