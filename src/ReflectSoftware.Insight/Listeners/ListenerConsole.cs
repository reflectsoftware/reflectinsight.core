// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using Plato.Extensions;

namespace ReflectSoftware.Insight
{
    internal class ListenerConsole : IReflectInsightListener
	{
        protected MessageTextFlag FDetails;
        protected String FMessagePattern;
        protected List<String> FTimePatterns;
        protected Boolean FColored;
        
        public virtual void UpdateParameterVariables(IListenerInfo listener)
        {
            FDetails = ListenerFileHelper.DetermineMessageTextFlagParam(listener);
            FMessagePattern = ListenerFileHelper.DetermineMessageTextPattern(listener);
            FTimePatterns = RIUtils.GetListOfTimePatterns(FMessagePattern);
            FColored = listener.Params["colored"].IfNullOrEmptyUseDefault("true").Trim() == "true";
        }
        
        public virtual void Receive(ReflectInsightPackage[] messages)
        {
            DateTime dt = DateTime.Now.ToUniversalTime();

            foreach (ReflectInsightPackage message in messages)
            {
                if (message.FMessageType == MessageType.Clear
                || message.FMessageType == MessageType.PurgeLogFile
                || RIUtils.IsViewerSpecificMessageType(message.FMessageType))
                {
                    continue;
                }
                
                message.FDateTime = dt;

                if (FColored)
                {
                    switch (message.FMessageType)
                    {
                        case MessageType.SendDebug: Console.ForegroundColor = ConsoleColor.Green; break;
                        case MessageType.SendInformation: Console.ForegroundColor = ConsoleColor.White; break;
                        case MessageType.SendWarning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                        case MessageType.SendError: Console.ForegroundColor = ConsoleColor.Magenta; break;
                        case MessageType.SendFatal: Console.ForegroundColor = ConsoleColor.Red; break;
                        case MessageType.SendMiniDumpFile: Console.ForegroundColor = ConsoleColor.Red; break;
                        case MessageType.SendException: Console.ForegroundColor = ConsoleColor.Red; break;                        
                    }
                }

                Console.Write(MessageText.Convert(message, FDetails, FMessagePattern, FTimePatterns));

                if (FColored)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                DebugManager.Sleep(0);
            }
        }
	}
}
