// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common;
using System;

namespace ReflectSoftware.Insight
{
    static internal class DebugTextLoggerManager
    {
        static private DebugTextLogger FDebugTextLogger;

        
        static public void OnStartup()
        {
            FDebugTextLogger = new DebugTextLogger();
        }
        
        static public void OnShutdown()
        {
            if (FDebugTextLogger != null)
            {
                FDebugTextLogger.Dispose();
                FDebugTextLogger = null;
            }
        }
        
        static public void Write(String msg, params Object[] args)
        {
            if (FDebugTextLogger != null)
            {
                FDebugTextLogger.Write(msg, args);
            }
        }
    }


    internal class DebugTextLogger: IDisposable
    {
        private TextFileWriter FTextWriter;
        public Boolean Disposed { get; private set; }

        
        public DebugTextLogger()
        {
            Disposed = false;
            Boolean append = ReflectInsightConfig.Settings.GetDebugWriterAttribute("append", "true").ToLower() == "true";
            String filePath = ReflectInsightConfig.Settings.GetDebugWriterAttribute("path", String.Format(@"{0}RIDebugLog.txt", AppDomain.CurrentDomain.BaseDirectory));

            FTextWriter = new TextFileWriter(filePath, append, true);
        }
        
        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);
                 
                    if (FTextWriter != null)
                    {
                        FTextWriter.Dispose();
                        FTextWriter = null;
                    }
                }
            }
        }
        
        public void Write(String msg, params Object[] args)
        {
            if (FTextWriter != null)
            {
                FTextWriter.Write(msg, args);
            }
        }
    }
}
