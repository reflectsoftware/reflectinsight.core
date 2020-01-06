// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Reflection;

namespace ReflectSoftware.Insight
{
    internal class TraceMethodState
    {
        public Int32 TraceLevel { get; set; }
        public Boolean ExceptionHandled { get; set; }
    }

    public class TraceMethod : ITraceMethod
    {
        private readonly static MethodInfo FSendMethodInfo;
        private static Boolean TraceHtttpRequest { get; set; }
        private Int32 LastIndentLevel { get; set; }
        private ControlValues ControlValues { get; set; }
        private TraceMethodState TraceStates { get; set; }
        public IReflectInsight RI { get; internal set; }
        public String Message { get; internal set; }
        public Boolean Disposed { get; private set; }

        /// <summary>
        /// Initializes the <see cref="TraceMethod"/> class.
        /// </summary>
        static TraceMethod()
        {
            FSendMethodInfo = typeof(ReflectInsight).GetMethod("_SendCustomData", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        }

        /// <summary>
        /// Called when [configuration file change].
        /// </summary>
        static internal void OnConfigFileChange()
        {
            try
            {
                TraceHtttpRequest = ReflectInsightConfig.Settings.GetBaseTraceHttpRequestAttribute("enabled", "false").ToLower() == "true";
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static TraceMethod.OnConfigFileChange()");
            }
        }

        /// <summary>
        /// Called when [startup].
        /// </summary>
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }

        /// <summary>
        /// Called when [shutdown].
        /// </summary>
        static internal void OnShutdown()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceMethod"/> class.
        /// </summary>
        /// <param name="ri">The ri.</param>
        /// <param name="message">The message.</param>
        internal TraceMethod(ReflectInsight ri, String message)
        {
            RI = ri;
            Message = message ?? "(null)";
            Disposed = false;

            ControlValues = RequestManager.GetRequestObject();
            TraceStates = ControlValues.GetState<TraceMethodState>("TraceMethodState");

            if (TraceStates == null)
            {
                // must be parent trace method
                TraceStates = new TraceMethodState() { TraceLevel = 0, ExceptionHandled = false };
                ControlValues.AddState("TraceMethodState", TraceStates);
            }

            RICustomData cData = null;

            if (cData == null)
            {
                RI.EnterMethod(Message);
            }
            else
            {
                FSendMethodInfo.Invoke(RI, new object[] { MessageType.EnterMethod, message, cData, new object[] { } });
            }
                
            LastIndentLevel = ReflectInsight.IndentLevel;
            TraceStates.TraceLevel++;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="policy">The policy.</param>
        /// <returns></returns>
        public T Execute<T>(Func<T> action, TraceMethodExceptionPolicy policy = TraceMethodExceptionPolicy.LogAndSwallowParentsPolicy)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                ExceptionHandler(ex, (e) =>
                {
                    Boolean handled = false;
                    if (policy != TraceMethodExceptionPolicy.Ignore)
                    {
                        RI.SendException(e);
                        handled = policy == TraceMethodExceptionPolicy.LogAndSwallowParentsPolicy;
                    }

                    return handled;
                });

                throw;
            }
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="policy">The policy.</param>
        public void Execute(Action action, TraceMethodExceptionPolicy policy = TraceMethodExceptionPolicy.LogAndSwallowParentsPolicy)
        {
            Execute<Object>(() =>
            {
                action();
                return null;

            }, policy);
        }

        /// <summary>
        /// Exceptions the handler.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        public Boolean ExceptionHandler(Exception ex, Func<Exception, Boolean> handler)
        {
            if (!TraceStates.ExceptionHandled && handler != null)
            {
                TraceStates.ExceptionHandled = handler(ex);
            }

            return TraceStates.ExceptionHandled;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);
                                        
                    TraceStates.TraceLevel--;
                    if (TraceStates.TraceLevel == 0)
                    {
                        // must be parent trace method
                        ControlValues.RemoveState("TraceMethodState");
                    }

                    Int32 currentIndentLevel = ReflectInsight.IndentLevel;
                    while (currentIndentLevel > LastIndentLevel)
                    {
                        RI.ExitMethod("Matching ExitMethod was missing...");
                        currentIndentLevel--;
                    }
                        
                    RI.ExitMethod(Message);
                }
            }
        }
    }
}
