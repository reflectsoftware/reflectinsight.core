// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ReflectSoftware.Insight
{
    internal static class MessageQueue
    {
        private const Int32 THROTTLE_MSG_COUNT_THRESHOLD = 500000;     

        static private readonly List<BoundReflectInsightPackage> Messages;
        static private readonly Object ThrottleLock;
        static private Int32 MaxThrottleValue; 
        
        static MessageQueue()
        {
            ThrottleLock = new Object();            
            Messages = new List<BoundReflectInsightPackage>();
        }
        
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }
        
        static internal void OnConfigFileChange()
        {
            MaxThrottleValue = ReflectInsightConfig.Settings.GetMessageProcessingMaxValue("queueThrottleMaxLimit", THROTTLE_MSG_COUNT_THRESHOLD);
        }
        
        static private void AddAndProcessMessages(Action addCallback)
        {
            // The throttle logic was designed to prevent the Message Queue
            // from overflowing with too many messages. This usually happens if the 
            // MessageManager processing the messages becomes too busy and
            // cannot keep up with the increase of incoming demand, causing 
            // an overflow of unprocessed Messages, which inadvertently, caused
            // a high usage in memory.

            lock (ThrottleLock)
            {
                Int32 messageCount;

                lock (Messages)
                {
                    addCallback();
                    messageCount = Messages.Count;
                }

                if (messageCount < MaxThrottleValue)
                {
                    MessageManager.Process();
                    return;
                }

                RIUtils.GCCollect();

                MessageManager.Process();

                while (MessageManager.IsProcessing)
                {
                    Thread.Sleep(100);
                }

                RIUtils.GCCollect();
            }
        }
        
        static public void SendMessages(IEnumerable<BoundReflectInsightPackage> boundPackages)
        {
            AddAndProcessMessages(() => Messages.AddRange(boundPackages));
        }
        
        static public void SendMessage(BoundReflectInsightPackage boundPackage)
        {
            AddAndProcessMessages(() => Messages.Add(boundPackage));
        }
        
        static public Int32 MessageCount
        {
            get { lock (Messages) return Messages.Count; }
        }
        
        static public Boolean HasMessages
        {
            get { return MessageCount > 0; }
        }
        
        static public BoundReflectInsightPackage[] GetBoundMessages()
        {
            BoundReflectInsightPackage[] messages;

            lock (Messages)
            {
                messages = Messages.ToArray();
                Messages.Clear();
                Messages.Capacity = 0;
            }

            return messages;
        }
        
        static public void WaitUntilNoMessages(Int32 sleep = 0)
        {
            lock (ThrottleLock)
            {
                while (MessageCount > 0)
                    Thread.Sleep(sleep);
            }
        }
    }
}
