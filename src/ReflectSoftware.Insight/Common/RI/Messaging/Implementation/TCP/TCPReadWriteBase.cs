// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Specialized;

namespace RI.Messaging.ReadWriter.Implementation.TCP
{
    public abstract class TCPReadWriteBase : IMessageReadWriterBase, IThreadExceptionEvent, IDisposable
    {
        public TCPConfigSetting Settings { get; private set; }
        public Boolean Disposed { get; private set; }
        
        public event OnThreadExceptionHandler OnThreadException;

        protected virtual void Initialize(TCPConfigSetting settings)
        {
            Disposed = false;
            Settings = (TCPConfigSetting)settings.Clone();
        }

        public TCPReadWriteBase(String settingsId)
        {
            TCPConfigSetting settings = TCPConfiguration.GetSetting(settingsId);
            if (settings == null)
            {
                String eMsg = String.Format("Unable to obtain TCP settings from settings Id: {0}. Please check configuration settings.", settingsId);
                throw new Exception(eMsg);
            }

            Initialize(settings);
        }

        public TCPReadWriteBase(String name, String address, Int32 port)
        {
            TCPConfigSetting settings = new TCPConfigSetting();
            settings.Name = name;
            settings.HostName = address;
            settings.Port = port;
                
            Initialize(settings);
        }

        public TCPReadWriteBase(TCPConfigSetting settings)
        {
            Initialize(settings);
        }

        public TCPReadWriteBase(NameValueCollection parameters)
        {
            TCPConfigSetting settings = new TCPConfigSetting();

            settings.Name = parameters["name"];
            settings.HostName = parameters["hostname"];

            Int32 number = 8081;
            Int32.TryParse(parameters["port"], out number);
            settings.Port = number;

            number = 2000;
            Int32.TryParse(parameters["connectionTimeout"], out number);
            settings.ConnectionTimeout = number;
            
            Initialize(settings);
        }

        ~TCPReadWriteBase()
        {
            Dispose(false);
        }

        protected void DoOnThreadException(Exception ex)
        {
            try
            {
                if (OnThreadException != null)
                {
                    lock (this)
                    {
                        OnThreadException(ex);
                    }
                }
            }
            catch (Exception)
            {
                // just swallow as there's nothing we can do
            }
        }

        public Boolean IsThreadSafe()
        {
            return true;
        }

        protected virtual void Dispose(Boolean disposing)
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    Close();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual Boolean IsOpen()
        {
            return false;
        }

        public virtual void Open()
        {            
        }

        public virtual void Close()
        {
        }
    }
}
