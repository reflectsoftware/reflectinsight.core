// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Threading;

namespace ReflectSoftware.Insight
{
    static internal class MessageManager
    {
        private const Int32 SEND_CHUNK_SIZE = 50000;
        private const Int32 RunModeSleep = 10;
        private const Int32 DebugModeSleep = 5;

        private readonly static Object FLockObject;
        private readonly static Object FDebugLockObject;
        static private DateTime FLastDateTime;
        static private Int32 FSleep;
        static private Int32 FMaxChunking; 
        
        static public Boolean IsProcessing { get; private set; }
        
        static MessageManager()
        {
            FLockObject = new Object();
            FDebugLockObject = new Object();
            FLastDateTime = DateTime.MinValue;
            IsProcessing = false;
            FSleep = RunModeSleep;            
        }
        
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }
        
        static internal void OnConfigFileChange()
        {
            lock (FDebugLockObject)
            {
                FMaxChunking = ReflectInsightConfig.Settings.GetMessageProcessingMaxValue("dispatchChunkingMax", SEND_CHUNK_SIZE);
            }
        }
        
        static internal void StartDebugProcessThread()
        {
            if (!DebugManager.DebugMessageProcessEnabled)
                return;

            FSleep = DebugModeSleep;
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            Thread processThread = new Thread(ExecuteProcessMessages);
            processThread.Priority = ThreadPriority.Highest;
            processThread.IsBackground = true;
            processThread.Start();
        }
        
        static private void SendPackages(DestinationInfo[] destinations)
        {
            foreach (DestinationInfo dInfo in destinations)
            {
                try
                {
                    ReflectInsightPackage[] messages = dInfo.GetInterimMessages();
                    if (messages.Length > 0)
                    {
                        try
                        {
                            InvokeListeners.Receive(dInfo, dInfo.GetInterimMessages());
                        }
                        finally
                        {
                            dInfo.ClearInterimMessageQueue();
                            DebugManager.Sleep(0);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    if (RIExceptionManager.CanEvent(ex))
                    {
                        RIExceptionManager.Publish(new ReflectInsightException(String.Format("MessageManager.InvokeListeners: unhandled exception was detected in destination message loop for destination: {0}", dInfo.Name), ex));
                    }

                    RIEventManager.DoOnQueueException(ex);
                }
            }
        }
        
        static private void AddToDestinationInterimMessageQueue(DestinationInfo[] destinations, BoundReflectInsightPackage boundPackage)
        {
            foreach (DestinationInfo dInfo in destinations)
            {
                if (dInfo.Enabled 
                && dInfo.Listeners.Length != 0 
                && (dInfo.BindingGroupIds.Count == 0 || dInfo.BindingGroupIds.Contains(boundPackage.BindingGroupId))
                && dInfo.Filter.FilterMessage(boundPackage.Package) != null)
                {
                    dInfo.AddInterimMessageQueue(boundPackage.Package);
                }
            }
        }
        
        static internal void ProcessMessages()
        {
            lock (FDebugLockObject)
            {
                ListenerGroup activeGroup = RIListenerGroupManager.ActiveGroup;

                BoundReflectInsightPackage[] boundPackages = MessageQueue.GetBoundMessages();
                if (activeGroup == null || boundPackages.Length == 0)
                {
                    return;
                }

                DestinationInfo[] destinations = activeGroup.Destinations;
                if (destinations.Length == 0)
                {
                    return;
                }

                // chunk the sending to reduce memory pressure
                Int32 at = 0;
                Int32 remaining = boundPackages.Length;
                Int32 chunk = remaining < FMaxChunking ? remaining : FMaxChunking;

                while (remaining > 0)
                {
                    for (Int32 i = at; i < (at + chunk); i++)
                    {
                        RIUtils.HandleUnknownMessage(boundPackages[i].Package);
                        AddToDestinationInterimMessageQueue(destinations, boundPackages[i]);
                    }

                    SendPackages(destinations);
                    Thread.Sleep(0);

                    at += chunk;
                    remaining -= chunk;
                    chunk = remaining < FMaxChunking ? remaining : FMaxChunking;
                }
            }
        }
        
        static private void ExecuteProcessMessages()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(FSleep);

                    ProcessMessages();

                    if (DebugManager.DebugMessageProcessEnabled)
                    {
                        continue;
                    }

                    lock (FLockObject)
                    {
                        if (!MessageQueue.HasMessages)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(FSleep);
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                RIExceptionManager.PublishIfEvented(new ReflectInsightException("MessageManager.ExecuteProcessMessages detected an unhandled exception. Please see inner exception for more details.", ex));
                RIEventManager.DoOnQueueException(ex);
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        static private void RunModeProcess()
        {
            Thread processThread = null;
            lock (FLockObject)
            {
                if (IsProcessing)
                {
                    return;
                }

                IsProcessing = true;
                processThread = new Thread(ExecuteProcessMessages);
            }

            processThread.Start();            
            Thread.Sleep(0);
        }
        
        static private void DebugModeProcess()
        {
            // NOTE: Changing sleep and message count numbers may cause
            // the library to not perform efficiently during debug/step mode.
            // Try not to change any hard coded values.

            lock (FLockObject)
            {
                if (IsProcessing)
                {
                    return;
                }

                IsProcessing = true;
            }

            if (MessageQueue.MessageCount > 2000)
            {
                FSleep = 0;
                Thread.Sleep(100);
                MessageQueue.WaitUntilNoMessages(100);

                FSleep = DebugModeSleep;
            }
            else if (DateTime.Now.Subtract(FLastDateTime).TotalMilliseconds > 50)
            {
                if (MessageQueue.MessageCount > 0)
                {
                    FSleep = 0;
                    Thread.Sleep(100);
                    MessageQueue.WaitUntilNoMessages(100);

                    FSleep = DebugModeSleep;
                    FLastDateTime = DateTime.Now;
                }

                Thread.Sleep(10);
            }

            IsProcessing = false;
            Thread.Sleep(0);
        }
        
        static public void Process()
        {                        
            if (!DebugManager.DebugMessageProcessEnabled)
            {
                RunModeProcess();
                return;
            }

            DebugModeProcess();
        }
    }
}
