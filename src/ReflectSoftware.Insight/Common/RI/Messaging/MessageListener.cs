// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Extensions;
using Plato.Miscellaneous;
using System;
using System.Threading;

namespace RI.Messaging.ReadWriter
{
    public delegate void MessageListenerOnMessageReceivedHandler(IMessageReader reader, Byte[] message);
    public delegate void MessageListenerOnExceptionHandler(Exception ex);
    
    public class MessageListener: IDisposable
    {        
        protected Boolean Terminated;        
        protected Thread DoWorkThread;
        public Boolean Disposed { get; private set; }
        public String ReaderName { get; private set; }
        public IMessageReader Reader { get; private set; }        
        public Int32 ReaderTimeout { get; set; }
        public TimeSpan LogExceptionTimeSpan { get; set; }
        public Boolean UseLogExceptionTimeSpan { get; set; }
        public String EventLogSource { get; set; }
        public Int32 IOExceptionTimeout { get; set; }                
        public event MessageListenerOnMessageReceivedHandler OnMessageReceived;        
        public event MessageListenerOnExceptionHandler OnException;

        protected void Init(String readerName)
        {
            Disposed = false;
            Terminated = true;
            DoWorkThread = null;
            ReaderName = readerName;
            ReaderTimeout = 1000; // 1 seconds default
            LogExceptionTimeSpan = new TimeSpan(0, 20, 0); // 20 minutes default
            UseLogExceptionTimeSpan = true;
            EventLogSource = "Application";
            IOExceptionTimeout = 2000; // 1 seconds default
        }

        public MessageListener(String readerName, IMessageReader reader)
        {
            Init(readerName);

            if (reader == null)
                throw new ArgumentNullException("reader");

            Reader = reader;
            if (Reader is IThreadExceptionEvent)
                (Reader as IThreadExceptionEvent).OnThreadException += DoOnException;
        }

        ~MessageListener()
        {
            Dispose(false);
        }

        protected virtual void Dispose(Boolean bDisposing)
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    Stop();

                    if (Reader != null)
                    {
                        if (Reader is IThreadExceptionEvent)
                            (Reader as IThreadExceptionEvent).OnThreadException -= DoOnException;

                        Reader.Dispose();
                        Reader = null;
                    }
                }
            }
        }

        public void Dispose()
        {            
            Dispose(true);
        }

        protected void LogError(String message)
        {
            // TODO:
        }
        protected void DoOnException(Exception ex)
        {
            if (!UseLogExceptionTimeSpan || TimeEventTracker.CanEvent((Int32)ex.Message.BKDRHash(), LogExceptionTimeSpan))
            {
                try
                {
                    if (OnException != null)
                    {
                        OnException(ex);
                    }
                    else
                    {
                        LogError(ex.StackTrace);
                    }
                }
                catch (Exception ex2)
                {
                    LogError(ex2.StackTrace);
                }
            }
        }

        protected void DoOnMessageReceived(IMessageReader reader, Byte[] data)
        {
            try
            {
                if (OnMessageReceived != null && data != null)
                    OnMessageReceived(reader, data);
            }
            catch (Exception ex)
            {
                DoOnException(new Exception(String.Format("A callback event initiated from DoOnMessageReceived detected the following exception: {0}", ex.Message), ex));
            }
        }

        protected void EnsureOpenReader()
        {
            if (!Reader.IsOpen())
                Reader.Open();
        }

        protected void CloseReader()
        {
            if (Reader.IsOpen())
                Reader.Close();
        }

        protected virtual void DoWork()
        {
            try
            {
                while (!Terminated)
                {
                    try
                    {
                        EnsureOpenReader();

                        Byte[] message = Reader.Read(ReaderTimeout);
                        DoOnMessageReceived(Reader, message);
                    }
                    catch (TimeoutException)
                    {
                        // just swallow
                    }
                    catch (Exception ex)
                    {
                        if (!Terminated)
                        {
                            DoOnException(ex);
                            Thread.Sleep(IOExceptionTimeout);
                        }
                    }
                }

                CloseReader();
            }
            catch (Exception ex)
            {
                DoOnException(ex);                
            }
        }

        public void Start()
        {
            lock (this)
            {
                if (Terminated)
                {
                    Terminated = false;
                    DoWorkThread = new Thread(DoWork);
                    DoWorkThread.Start();
                }
            }
        }

        public void Stop()
        {
            lock (this)
            {
                if (!Terminated)
                {
                    Terminated = true;                    
                    if (DoWorkThread != null)
                    {
                        DoWorkThread.Join();
                        DoWorkThread = null;
                    }
                }
            }
        }
    }
}
