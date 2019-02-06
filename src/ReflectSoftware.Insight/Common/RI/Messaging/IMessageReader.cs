using System;


namespace RI.Messaging.ReadWriter
{
    public interface IMessageReader : IMessageReadWriterBase
    {        
        Byte[] Read();
        Byte[] Read(Int32 msecTimeout);
    }
}
