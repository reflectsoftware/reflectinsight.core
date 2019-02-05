using System;

namespace RI.Messaging.ReadWriter
{
    public interface IMessageReadWriterBase: IDisposable
    {
        Boolean IsThreadSafe();
        Boolean IsOpen();
        void Open();
        void Close();
    }
}
