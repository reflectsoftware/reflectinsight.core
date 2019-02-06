using System;

namespace RI.Messaging.ReadWriter
{
    public delegate void OnThreadExceptionHandler(Exception ex);

    public interface IThreadExceptionEvent
    {
        event OnThreadExceptionHandler OnThreadException;
    }
}
