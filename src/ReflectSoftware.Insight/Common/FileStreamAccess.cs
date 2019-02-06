// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ReflectSoftware.Insight.Common
{
    public class FileStreamAccess
    {
        private const int WaitTime = 50;// msecs
        
        public static FileStream OpenFileStreamForReading(String path, FileMode mode)
        {
            while (true)
            {
                try
                {
                    return new FileStream(path, mode, FileAccess.Read, FileShare.Read);
                }
                catch (IOException ex)
                {
                    if (ex.GetType() == typeof(IOException))
                    {
                        Thread.Sleep(FileStreamAccess.WaitTime);
                        continue;
                    }

                    throw;
                }
            }
        }
        
        public static FileStream OpenFileStreamForWriting(String path, FileMode mode)
        {
            while (true)
            {
                try
                {
                    return new FileStream(path, mode, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException ex)
                {
                    if (ex.GetType() == typeof(IOException))
                    {
                        Thread.Sleep(FileStreamAccess.WaitTime);
                        continue;
                    }

                    throw;
                }
            }
        }
        
        public static StreamWriter OpenStreamWriter(String path, Boolean append, Encoding encoding)
        {
            StreamWriter fs = new StreamWriter(FileStreamAccess.OpenFileStreamForWriting(path, FileMode.OpenOrCreate), encoding);

            if(append)
            {
                fs.BaseStream.Seek(0, SeekOrigin.End);
            }

            return fs;
        }
    }    
}
