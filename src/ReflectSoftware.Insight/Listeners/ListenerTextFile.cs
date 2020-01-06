// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Serializers;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReflectSoftware.Insight
{
    internal class ListenerTextFile : ListenerConsole
	{
        private const Int32 MBYTE = 1048576;
        
        protected String FFilePath;        
        protected String FControlFilePath;        
        protected RIAutoSaveInfo FAutoSave;		        
        protected TextWriter FFileStream;        
        protected FileStream FControlFileStream;        
        protected RIFileHeader FFileHeader;        
        protected Boolean FCreateDirectory;
        protected Boolean FAllowPurge;
        protected Int64 FOnSize;
        
        public override void UpdateParameterVariables(IListenerInfo listener)
        {
            base.UpdateParameterVariables(listener);

            listener.Params["path"] = ListenerFileHelper.DeterminePathParam(listener);
            FAutoSave = ListenerFileHelper.DetermineAutoSaveParam(listener);

            FFilePath = listener.Params["path"];
            if (string.IsNullOrWhiteSpace(FFilePath))
            {
                throw new ReflectInsightException(String.Format("Missing path parameter for listener: '{0}' using details: '{1}'.", listener.Name, listener.Details));
            }
            
            FCreateDirectory = true;
            FControlFilePath = String.Format("{0}.ctrl", FFilePath);
            FOnSize = FAutoSave.SaveOnSize * MBYTE; // MB
            FAllowPurge = listener.Params["allowPurge"] != "false";
        }
        
        private void OpenControlFile()
        {
            CloseFileStream(false);
            FControlFileStream = FileStreamAccess.OpenFileStreamForWriting(FControlFilePath, FileMode.OpenOrCreate);

            Dictionary<Int32, FastSerializerObjectData> onNewExtraData;
            if (FFileHeader != null)
            {
                onNewExtraData = FFileHeader.CloneExtraData();
            }
            else
            {
                onNewExtraData = null;
            }

            FFileHeader = FileHelper.ReadHeader(FControlFileStream, FFilePath, onNewExtraData);
        }
        
        private void CloseControlFile(Boolean bSaveHeader)
        {
            if (FControlFileStream != null)
            {
                if (bSaveHeader)
                    FileHelper.WriteHeader(FControlFileStream, FFileHeader);
                
                FControlFileStream.Close();
                FControlFileStream = null;
            }
        }
        
        private void OpenFileStream()
        {
            if (FCreateDirectory)
            {                
                Directory.CreateDirectory(Path.GetDirectoryName(FFilePath));
                FCreateDirectory = false;
            }

            OpenControlFile();

            FFileStream = FileStreamAccess.OpenStreamWriter(FFilePath, true, Encoding.UTF8);
        }
        
        private void CloseFileStream(Boolean bSaveHeader)
        {
            if (FFileStream != null)
            {                
                FFileStream.Close();
                FFileStream = null;
            }

            CloseControlFile(bSaveHeader);
        }
        
        private void PurgeLogFile()
        {
            if (FAllowPurge)
            {
                CloseFileStream(false);
                FileHelper.DeleteAllLogs(FFilePath);
                FileHelper.DeleteAllLogs(FControlFilePath);                
                OpenFileStream();
                return;
            }
        }
        
        private void ForceAutoSave()
        {
            FControlFileStream.Flush();
            FFileStream.Flush();

            Stream textStream = (FFileStream as StreamWriter).BaseStream;
            String recycleFilePath = FileHelper.RecycleAndGetNextFileName(FFilePath, FAutoSave, FFileHeader);
            
            using (FileStream recycleStream = FileStreamAccess.OpenFileStreamForWriting(recycleFilePath, FileMode.Create))
            {
                RIUtils.CopyFile(textStream, recycleStream);
            }

            // truncate text file
            textStream.SetLength(0);            
            textStream.Seek(0, SeekOrigin.End);
            FFileStream.Flush();
                       
            // trucate control file
            FControlFileStream.SetLength(0);
            FControlFileStream.Seek(0, SeekOrigin.End);
            FControlFileStream.Flush();

            FFileHeader = FileHelper.ReadHeader(FControlFileStream, FControlFilePath, null);
            FControlFileStream.Seek(0, SeekOrigin.End);
        }
        //--------------------------------------------------------------------
        //private void ExampleHowToExtendTheHeader()
        //{
        //    // The code below shows how to extend the header.  
        //    // In this example, the MyExtraData must implement the IFastSerializer. 
        //    // Extending the header in the context of a text, has very little usage
        //    // but nonetheless, is supported for those odd rare cases if needed.
        //    // FFileHeader.FExtraData["MyExtraData"] = new FastSerializerObjectData(new MyExtraData());
        //}
        /// -------------------------------------------------------------------
        private void ProcessMessages(ReflectInsightPackage[] messages)
        {            
            OpenFileStream();

            try
            {
                Stream baseStream = (FFileStream as StreamWriter).BaseStream;
                DateTime dt = DateTime.Now.ToUniversalTime();

                foreach (ReflectInsightPackage message in messages)
                {
                    if (message.FMessageType == MessageType.PurgeLogFile)
                    {
                        PurgeLogFile();
                        continue;
                    }

                    if (message.FMessageType == MessageType.Clear || RIUtils.IsViewerSpecificMessageType(message.FMessageType))
                    {
                        continue;
                    }

                    message.FDateTime = dt;
                    message.FSequenceID = FFileHeader.GetNextSequenceId();
                    String txtMessage = MessageText.Convert(message, FDetails, FMessagePattern, FTimePatterns);

                    if (FileHelper.ShouldAutoSave(FFileHeader, FAutoSave, baseStream, FOnSize, message.FDateTime, txtMessage.Length))
                    {
                        ForceAutoSave();

                        message.FSequenceID = FFileHeader.GetNextSequenceId();
                        txtMessage = MessageText.Convert(message, FDetails, FMessagePattern, FTimePatterns);
                    }

                    if (FFileHeader.FInitDateTime == DateTime.MinValue)
                    {
                        FFileHeader.FInitDateTime  = message.FDateTime;
                        FFileHeader.FFirstDateTime = message.FDateTime;
                        FFileHeader.FLastDateTime  = message.FDateTime;
                    }                        
                                                
                    FFileHeader.FMessageCount++;
                    FFileHeader.FLastDateTime = message.FDateTime;
                    FFileStream.Write(txtMessage);                       
                                        
                    DebugManager.Sleep(0);
                }
            }
            finally
            {
                CloseFileStream(true);
            }
        }

        public override void Receive(ReflectInsightPackage[] messages)
        {
            ProcessMessages(messages);
        }
	}
}
