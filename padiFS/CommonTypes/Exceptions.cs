using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace padiFS
{
    [Serializable]
    public class PadiFsException : ApplicationException
    {

        public PadiFsException()
        {
            
        }

        public PadiFsException(string message) : base(message)
        {

        }

        public PadiFsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        // RETRIEVE SERIALIZED DATA FROM EXCEPTION

        //public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        //{
        //    base.GetObjectData(info, context);
        //    //info.AddValue("campo", campo);
        //    //info.AddValue("mo", mo);
        //}
    }

    [Serializable]
    public class FileNotFoundException : PadiFsException
    {
        public FileNotFoundException() : base() { }
        public FileNotFoundException(string message) : base(message) { }
        public FileNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class ServerNotAvailableException : PadiFsException
    {
        public ServerNotAvailableException() : base() { }
        public ServerNotAvailableException(string message) : base(message) { }
        public ServerNotAvailableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class FileAlreadyOpenException : PadiFsException
    {
        public FileAlreadyOpenException() : base() { }
        public FileAlreadyOpenException(string message) : base(message) { }
        public FileAlreadyOpenException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class FileAlreadyCloseException : PadiFsException
    {
        public FileAlreadyCloseException() : base() { }
        public FileAlreadyCloseException(string message) : base(message) { }
        public FileAlreadyCloseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class FileNotOpenException : PadiFsException
    {
        public FileNotOpenException() : base() { }
        public FileNotOpenException(string message) : base(message) { }
        public FileNotOpenException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class ServerNotFoundException : PadiFsException
    {
        public ServerNotFoundException() : base() { }
        public ServerNotFoundException(string message) : base(message) { }
        public ServerNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
