// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Threading;
using System.Diagnostics;

namespace ReflectSoftware.Insight
{
    internal static class DebugManager
    {
        private readonly static Object DebugTimerLock;
        static private Boolean DebugTimerBusy;
        static private Timer DebugTimer;

        static public Boolean DebugMessageProcessEnabled { get; internal set; }
        
        static DebugManager()
        {
            DebugTimerLock = new Object();
            DebugTimerBusy = false;
            DebugTimer = null;
                        
            DebugMessageProcessEnabled = Debugger.IsAttached;
        }
        
        static internal void OnStartup()
        {
            if (DebugMessageProcessEnabled)
            {
                StartDebugProcessThread();
                DebugTimer = new Timer(DebugTimerCallback, null, 0, 500);
            }
        }
        
        static internal void OnShutdown()
        {
            if (DebugMessageProcessEnabled)
            {
                MessageQueue.WaitUntilNoMessages(100);

                if (DebugTimer != null)
                {
                    DebugTimer.Dispose();
                    DebugTimer = null;
                }

                Thread.Sleep(100);
            }
        }
        
        static private void DebugTimerCallback(Object data)
        {
            lock (DebugTimerLock)
            {
                if (DebugTimerBusy)
                    return;

                DebugTimerBusy = true;
            }

            MessageManager.ProcessMessages();
            DebugTimerBusy = false;
        }
        
        static private void StartDebugProcessThread()
        {
            MessageManager.StartDebugProcessThread();
        }
        
        static internal void Sleep(Int32 msec)
        {
            if (DebugMessageProcessEnabled)
                return;

            Thread.Sleep(msec);
        }
    }
}
