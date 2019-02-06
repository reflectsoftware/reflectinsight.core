// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace RI.Messaging.ReadWriter.Implementation.TCP
{
    public class TCPWriter: TCPReadWriteBase, IMessageWriter
    {
        protected TcpClient FSocket;
        protected Int32 FConnectionTimeout;

        public TCPWriter(String settingsId): base(settingsId)
        {
        }

        public TCPWriter(String name, String hostname, Int32 port): base(name, hostname, port)
        {
        }

        public TCPWriter(TCPConfigSetting settings): base(settings)
        {
        }

        public TCPWriter(NameValueCollection parameters): base(parameters)
        {
        }

        protected override void Initialize(TCPConfigSetting settings)
        {
            base.Initialize(settings);

            if (string.IsNullOrWhiteSpace(settings.HostName))
            {
                throw new Exception(String.Format("TCP setting is missing hostname parameter for writer: '{0}'", Settings.Name));
            }
        }

        public override void Close()
        {
            lock (this)
            {
                if (FSocket != null)
                {                    
                    FSocket.Close();                    
                    FSocket = null;
                }
            }
        }

        public override Boolean IsOpen()
        {
            lock (this)
            {
                return FSocket != null && FSocket.Connected;
            }
        }

        public override void Open()
        {
            lock (this)
            {
                if (FSocket == null)
                    FSocket = new TcpClient();

                // this is done to avoid waiting for a long connection timeout
                try
                {
                    IAsyncResult ar = FSocket.BeginConnect(Settings.HostName, Settings.Port, null, null);                    
                    using (WaitHandle wh = ar.AsyncWaitHandle)
                    {
                        if (!wh.WaitOne(Settings.ConnectionTimeout, false))
                        {
                            throw new IOException(String.Format("TCP connection timed out for writer: '{0}'. Please check connection settings or ensure that the host is available for receiving connections.", Settings.Name));
                        }

                        FSocket.EndConnect(ar);
                    }

                    FSocket.SendBufferSize = 1000000;
                }
                catch (Exception)
                {
                    Close();
                    throw;
                }
            }
        }

        public void Write(Byte[] data)
        {
            lock (this)
            {
                if (!IsOpen())
                    throw new Exception(String.Format("TCP connection is not opened or has been disconnected from its host for writer: '{0}'", Settings.Name));

                Byte[] bSize = BitConverter.GetBytes((Int32)data.Length);

                try
                {
                    FSocket.GetStream().Write(bSize, 0, bSize.Length);
                    FSocket.GetStream().Write(data, 0, data.Length);
                }
                catch (Exception)
                {
                    Close();
                    throw;
                }
            }
        }
    }
}
