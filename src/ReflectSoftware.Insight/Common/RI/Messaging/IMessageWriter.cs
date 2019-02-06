using System;

namespace RI.Messaging.ReadWriter
{
    public interface IMessageWriter : IMessageReadWriterBase
    {
        void Write(Byte[] data);        
    }
}
