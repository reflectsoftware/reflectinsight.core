using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace RI.Messaging.ReadWriter.Implementation.TCP
{
    public class TCPReader : TCPReadWriteBase, IMessageReader
    {                
        protected Boolean FTerminate;        
        protected TcpListener FListener;        
        protected AutoResetEvent FQueueReadyEvent;        
        protected Queue<Byte[]> FQueue;        
        protected TimeoutException FTimeoutException; 

        public TCPReader(String settingsId): base(settingsId)
        {
        }

        public TCPReader(String name, String hostname, Int32 port) : base(name, hostname, port)
        {
        }

        public TCPReader(TCPConfigSetting settings): base(settings)
        {
        }

        public TCPReader(NameValueCollection parameters): base(parameters)
        {
        }

        protected override void Initialize(TCPConfigSetting settings)
        {
            base.Initialize(settings);

            FTerminate = true;
            FQueue = new Queue<Byte[]>();
            FQueueReadyEvent = new AutoResetEvent(false);
            FListener = new TcpListener(IPAddress.Any, settings.Port);

            FTimeoutException = new TimeoutException(); // since we will be throwing this exception quite a bit, let's just create it once
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                base.Dispose(disposing);

                if (FQueueReadyEvent != null)
                {
                    FQueueReadyEvent.Close();

                    #if NET40
                    FQueueReadyEvent.Dispose();                    
                    #endif

                    FQueueReadyEvent = null;
                }
            }
        }

        protected void QueueMessage(Byte[] message)
        {
            lock (FQueue)
            {
                FQueue.Enqueue(message);
                FQueueReadyEvent.Set();
            }
        }

        protected Byte[] ReadNextQueuedMessage()
        {
            Byte[] message = null;
            
            lock (FQueue)
            {
                if (FQueue.Count > 0)
                    message = FQueue.Dequeue();

                if (FQueue.Count > 0)
                    FQueueReadyEvent.Set();
            }

            return message;
        }

        protected Byte[] ReadRequestStream(Socket clientSocket)
        {
            Byte[] messageSize = new Byte[Marshal.SizeOf(typeof(Int32))];
            Byte[] message = null;

            Int32 totalBytesToRead = messageSize.Length;

            while (!FTerminate && clientSocket.Connected)
            {
                try
                {
                    if (message == null)
                    {
                        Int32 bytesRead = clientSocket.Receive(messageSize, messageSize.Length-totalBytesToRead, totalBytesToRead, SocketFlags.None);
                        if (bytesRead <= 0)
                            break;

                        totalBytesToRead -= bytesRead;
                        if (totalBytesToRead == 0)
                        {
                            message = new Byte[BitConverter.ToInt32(messageSize, 0)];
                            totalBytesToRead = message.Length;
                        }
                    }
                    else // reading message
                    {
                        Int32 bytesRead = clientSocket.Receive(message, message.Length - totalBytesToRead, totalBytesToRead, SocketFlags.None);
                        if (bytesRead <= 0)
                            break;

                        totalBytesToRead -= bytesRead;
                        if (totalBytesToRead == 0)
                        {
                            // completed reading message
                            return message;
                        }
                    }                        
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionAborted
                    ||  ex.SocketErrorCode == SocketError.ConnectionReset)
                        break;

                    if (ex.SocketErrorCode == SocketError.WouldBlock
                    ||  ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    throw;
                }
            }

            return null; // connection closed
        }

        protected void ClientSocketThread(Object oClientSocket)
        {
            var clientSocket = (oClientSocket as Socket);
            clientSocket.Blocking = false;
            clientSocket.ReceiveTimeout = 1000;            

            try
            {
                while (!FTerminate && clientSocket.Connected)
                {
                    try
                    {
                        Byte[] message = ReadRequestStream(clientSocket);
                        if (message == null)
                            break; // socket was disconnected

                        QueueMessage(message);
                    }
                    catch (ThreadAbortException)
                    {                        
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!FTerminate)
                        {
                            DoOnThreadException(ex);
                            Thread.Sleep(250);
                            break;
                        }
                    }
                }
            }
            finally
            {
                clientSocket.Shutdown(SocketShutdown.Receive);
                clientSocket.Disconnect(false);
                clientSocket.Close();
                clientSocket.Dispose();
            }
        }

        protected void DoAcceptClientCallback(IAsyncResult ar)
        {
            if (!FTerminate)
            {
                try
                {
                    Socket clientSocket = FListener.Server.EndAccept(ar);
                    clientSocket.ReceiveBufferSize = 1000000;

                    Thread socketThread = new Thread(ClientSocketThread);
                    socketThread.IsBackground = true;
                    socketThread.Start(clientSocket);

                    FListener.Server.BeginAccept(DoAcceptClientCallback, null);
                }
                catch (Exception ex)
                {
                    DoOnThreadException(ex);                    
                }
            }
        }

        public override Boolean IsOpen()
        {
            return !FTerminate;
        }

        public override void Open()
        {
            lock (this)
            {
                FTerminate = false;
                FListener.Start();
                FListener.Server.BeginAccept(DoAcceptClientCallback, null);
            }
        }
        
        public override void Close()
        {
            lock (this)
            {
                FTerminate = true;
                FListener.Stop();                
            }
        }

        public Byte[] Read(Int32 msecTimeout)
        {
            if (!FQueueReadyEvent.WaitOne(msecTimeout))
                throw FTimeoutException;
            
            return ReadNextQueuedMessage();
        }

        public Byte[] Read()
        {
            return Read(Timeout.Infinite);
        }
    }
}
