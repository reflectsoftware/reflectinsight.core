// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Serializers;
using Plato.Serializers.FormatterPools;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace ReflectSoftware.Insight
{
    internal class ListenerBinaryFile : IReflectInsightListener
	{
        private const Int32 MBYTE = 1048576;

        protected String FFilePath;
        protected RIAutoSaveInfo FAutoSave;        
        protected RIFileHeader FFileHeader;
        protected FileStream FFileStream;
        protected Boolean FCreateDirectory;
        protected Boolean FAllowPurge;
        protected Int64 FOnSize;
        
        public void UpdateParameterVariables(IListenerInfo listener)
        {            
            listener.Params["path"] = ListenerFileHelper.DeterminePathParam(listener);
            FAutoSave = ListenerFileHelper.DetermineAutoSaveParam(listener);

            FFilePath = listener.Params["path"];
            if (string.IsNullOrWhiteSpace(FFilePath))
            {
                throw new ReflectInsightException(String.Format("Missing path parameter for listener: '{0}' using details: '{1}'.", listener.Name, listener.Details));
            }
            
            FCreateDirectory = true;
            FOnSize = FAutoSave.SaveOnSize * MBYTE; // MB
            FAllowPurge = listener.Params["allowPurge"] != "false";
        }
        
        private void OpenFileStream()
        {
            CloseFileStream(false);
            FFileStream = FileStreamAccess.OpenFileStreamForWriting(FFilePath, FileMode.OpenOrCreate);

            try
            {
                Dictionary<Int32, FastSerializerObjectData> onNewExtraData;
                if (FFileHeader != null)
                {
                    onNewExtraData = FFileHeader.CloneExtraData();
                }
                else
                {
                    onNewExtraData = null;
                }

                FFileHeader = FileHelper.ReadHeader(FFileStream, FFilePath, onNewExtraData);
                FFileStream.Seek(0, SeekOrigin.End);
            }
            catch (Exception)
            {
                CloseFileStream(false);
                throw;
            }
        }
        
        private void CloseFileStream(Boolean bSaveHeader)
        {
            if (FFileStream != null)
            {
                if (bSaveHeader)
                    FileHelper.WriteHeader(FFileStream, FFileHeader);
                
                FFileStream.Dispose();
                FFileStream = null;
            }
        }
        
        private void DeleteFile(Boolean bReopen)
        {
            CloseFileStream(false);
            File.Delete(FFilePath);

            if (bReopen)
            {
                OpenFileStream();
            }
        }
        
        private void PurgeLogFile()
        {
            if(FAllowPurge)
            {
                CloseFileStream(false);
                FileHelper.DeleteAllLogs(FFilePath);
                OpenFileStream();
                return;
            }
        }
        
        private void RewriteFileIgnoreRequestor(RIFileHeader newHeader, FastBinaryFormatter ff, UInt32 sessionID, UInt32 requestID)
        {
            RIFileHeader tmpHeader = null;
            String tmpFile = String.Format("{0}.tmp", FFilePath);

            using (FileStream tmpStream = FileStreamAccess.OpenFileStreamForWriting(tmpFile, FileMode.Create))
            {
                // original file: set file pointer to top                
                FFileStream.Seek(0, SeekOrigin.Begin);                
                ff.Serialize(FFileStream, FFileHeader);
                
                // original file: move file pointer to the end of the file header
                FFileStream.Seek(0, SeekOrigin.Begin);                
                ff.Deserialize<RIFileHeader>(FFileStream);
                                    
                // temp file: move file pointer to the end of the file header with new header info
                tmpHeader = newHeader;                    
                tmpHeader.FInitDateTime = DateTime.MinValue;
                tmpHeader.FFirstDateTime = DateTime.MinValue;
                tmpHeader.FLastDateTime = DateTime.MinValue;
                tmpHeader.FNextSequenceId = FFileHeader.FNextSequenceId;
                tmpHeader.FMessageCount = 0;
                ff.Serialize(tmpStream, tmpHeader);
                    
                // start copying original messages to temp file
                for (Int32 i = 0; i < FFileHeader.FMessageCount; i++)
                {
                    ReflectInsightPackage message = ff.Deserialize<ReflectInsightPackage>(FFileStream);
                    if (message.FRequestID == requestID && message.FSessionID == sessionID)
                    {
                        continue;
                    }

                    if (tmpHeader.FInitDateTime == DateTime.MinValue)
                    {
                        tmpHeader.FInitDateTime = message.FDateTime;
                        tmpHeader.FFirstDateTime = message.FDateTime;
                        tmpHeader.FLastDateTime = message.FDateTime;
                    }

                    tmpHeader.FMessageCount++;
                    tmpHeader.FLastDateTime = message.FDateTime;
                    ff.Serialize(tmpStream, message);
                }

                // update the FileHeader with the temp file header and write it out
                // with latest info.
                FFileHeader = tmpHeader;
                tmpStream.Seek(0, SeekOrigin.Begin);
                ff.Serialize(tmpStream, FFileHeader);
            }

            if (tmpHeader != null)
            {
                // delete the original file first, then copy temp to original
                DeleteFile(false);
                try
                {
                    File.Move(tmpFile, FFilePath);
                }
                finally
                {
                    OpenFileStream();
                }
            }
        }

        private void ForceAutoSave()
        {
            // write header and flush all buffers
            FileHelper.WriteHeader(FFileStream, FFileHeader);
            FFileStream.Flush();

            String recycleFilePath = FileHelper.RecycleAndGetNextFileName(FFilePath, FAutoSave, FFileHeader);
            
            using(FileStream recycleStream = FileStreamAccess.OpenFileStreamForWriting(recycleFilePath, FileMode.Create))
            {
                RIUtils.CopyFile(FFileStream, recycleStream);
            }

            // trucate the file
            FFileStream.SetLength(0);
            FFileStream.Flush();

            FFileHeader = FileHelper.ReadHeader(FFileStream, FFilePath, null);
            FFileStream.Seek(0, SeekOrigin.End);
        }
        //--------------------------------------------------------------------
        //private void ExampleHowToExtendTheHeader()
        //{
        //    // The code below shows how to extend the header.  
        //    // Keep in mind that when you extend the header, you must rewrite 
        //    // the file. Try to avoid extending the header frequently as this will
        //    // impact performance.
        //    //
        //    // In this example, the MyExtraData must implement the IFastSerializer. 

        //    //RIFileHeader newHeader = (RIFileHeader)FFileHeader.Clone();
        //    //newHeader.FExtraData["MyExtraData"] = new FastSerializerObjectData(new MyExtraData());
        //}
        ///--------------------------------------------------------------------
        private void ProcessMessages(ReflectInsightPackage[] messages)
        {
            if (FCreateDirectory)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FFilePath));                
                FCreateDirectory = false;
            }
            
            OpenFileStream();

            try
            {
                using (var pool = FastFormatterPool.Pool.Container())
                {
                    DateTime dt = DateTime.Now.ToUniversalTime();

                    foreach (ReflectInsightPackage message in messages)
                    {
                        if (message.FMessageType == MessageType.PurgeLogFile)
                        {
                            PurgeLogFile();
                            continue;
                        }
                        
                        if (message.FMessageType != MessageType.Clear)
                        {
                            if (RIUtils.IsViewerSpecificMessageType(message.FMessageType))
                            {
                                continue;
                            }

                            // serialize the message
                            message.FDateTime = dt;
                            message.FSequenceID = FFileHeader.GetNextSequenceId();                            
                            Byte[] bMessage = pool.Instance.Serialize(message);

                            if (FileHelper.ShouldAutoSave(FFileHeader, FAutoSave, FFileStream, FOnSize, message.FDateTime, bMessage.Length))
                            {
                                ForceAutoSave();

                                // because the previous message was serialized with the previous last sequence id
                                // we need to re-serialize the message with the new sequence id

                                message.FSequenceID = FFileHeader.GetNextSequenceId();
                                bMessage = pool.Instance.Serialize(message);
                            }

                            if (FFileHeader.FInitDateTime == DateTime.MinValue)
                            {
                                FFileHeader.FInitDateTime = message.FDateTime;
                                FFileHeader.FFirstDateTime = message.FDateTime;
                                FFileHeader.FLastDateTime = message.FDateTime;
                            }

                            FFileHeader.FMessageCount++;                            
                            FFileHeader.FLastDateTime = message.FDateTime;
                            FFileStream.Write(bMessage, 0, bMessage.Length);
                        }
                        else // clear 
                        {
                            RewriteFileIgnoreRequestor((RIFileHeader)FFileHeader.Clone(), pool.Instance, message.FSessionID, message.FRequestID);
                        }
                                                
                        DebugManager.Sleep(0);
                    }
                }
            }
            finally
            {
                CloseFileStream(true);
            }
        }

        public void Receive(ReflectInsightPackage[] messages)
        {
            ProcessMessages(messages);
        }
	}
}
