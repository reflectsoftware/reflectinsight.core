// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Locks;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ReflectSoftware.Insight.Common
{
    public class TextFileWriter: IDisposable
    {
        /// <summary>   The resource lock. </summary>
        protected ResourceLock FResourceLock;
        public String LogFilePath { get; private set; }
        public Boolean Append { get; private set; }
        public Boolean CreateDirectory { get; private set; }
        public Boolean Disposed { get; private set; }

        public TextFileWriter(String fileName, Boolean append, Boolean forceDirectoryCreation)
        {
            Disposed = false;
            LogFilePath = RIUtils.DetermineParameterPath(fileName);
            Append = append;
            CreateDirectory = forceDirectoryCreation;
            FResourceLock = new ResourceLock(fileName);           
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    if (FResourceLock != null)
                    {
                        FResourceLock.Dispose();
                        FResourceLock = null;
                    }
                }
            }
        }

        private TextWriter OpenFileStream()
        {
            if (CreateDirectory)
            {
                if (!Directory.Exists(Path.GetDirectoryName(LogFilePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));

                CreateDirectory = false;
            }

            Int32 attempts = 5;
            while (true)
            {
                try
                {
                    return new StreamWriter(LogFilePath, Append, Encoding.UTF8);
                }
                catch (IOException ex)
                {
                    // only used in case user opened and saved the file
                    // at the same time we tried to open it
                    attempts--;
                    if (attempts < 0)
                    {
                        throw new IOException(String.Format(CultureInfo.CurrentCulture, "TextFileWriter 'OpenFileStream' was unable to open file: {0}", LogFilePath), ex); 
                    }

                    Thread.Sleep(100);
                }
            }
        }

        public void Write(String msg, params Object[] args)
        {
            FResourceLock.EnterWriteLock();
            try
            {
                using (TextWriter tw = OpenFileStream())
                {
                    tw.WriteLine(msg, args);
                }
            }
            finally
            {
                FResourceLock.ExitWriteLock();
            }
        }
    }
}
