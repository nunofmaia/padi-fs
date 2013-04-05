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
    public class FileAlreadyExists : PadiFsException
    {
        public FileAlreadyExists() : base() { }
        public FileAlreadyExists(string message) : base(message) { }
        public FileAlreadyExists(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class FileIsOpenedException : PadiFsException
    {
        public FileIsOpenedException() : base() { }
        public FileIsOpenedException(string message) : base(message) { }
        public FileIsOpenedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class FileAlreadyClosedException : PadiFsException
    {
        public FileAlreadyClosedException() : base() { }
        public FileAlreadyClosedException(string message) : base(message) { }
        public FileAlreadyClosedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
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
    public class NotEnoughServersException : PadiFsException
    {
        public NotEnoughServersException() : base() { }
        public NotEnoughServersException(string message) : base(message) { }
        public NotEnoughServersException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
