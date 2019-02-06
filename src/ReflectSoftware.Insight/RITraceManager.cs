// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common;
using System;
using System.Collections.Generic;

namespace ReflectSoftware.Insight
{
    public interface IRITrace
    {
        String Name { get; }
        IReflectInsight Logger { get; }        
    }

    internal class DefaultTracer : IRITrace
    {
        public IReflectInsight Logger { get; internal set; }
        public String Name { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTracer"/> class.
        /// </summary>
        public DefaultTracer()
        {
            Name = "_DefaultTracer";
            Logger = RILogManager.Default;
        }
    }

    [Serializable]
    public class TraceThreadInfo : IRequestObject
    {        
        public Stack<IRITrace> Tracers;
        public Exception MethodException;
        public IRITrace RootTracer { get; set; }
        public UInt32 RequestId { get; private set; }

        /// <summary>
        /// Attacheds the specified request identifier.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        public void Attached(UInt32 requestId)
        {
            RequestId = requestId;
            MethodException = null;
            Tracers = new Stack<IRITrace>();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            Tracers.Clear();            
        }

        /// <summary>
        /// Detacheds this instance.
        /// </summary>
        public void Detached()
        {
        }

        /// <summary>
        /// Pushes the specified tracer.
        /// </summary>
        /// <param name="tracer">The tracer.</param>
        public void Push(IRITrace tracer)
        {
            Tracers.Push(tracer);
        }

        /// <summary>
        /// Pops this instance.
        /// </summary>
        /// <returns></returns>
        public IRITrace Pop()
        {
            IRITrace tracer;
            if (!EndOfStack())
                tracer = Tracers.Pop();
            else
                tracer = null;

            return tracer;
        }

        /// <summary>
        /// Peeks this instance.
        /// </summary>
        /// <returns></returns>
        public IRITrace Peek()
        {
            IRITrace tracer;
            if (!EndOfStack())
            {
                tracer = Tracers.Peek();
            }
            else
            {
                tracer = null;
            }

            return tracer;
        }

        /// <summary>
        /// Ends the of stack.
        /// </summary>
        /// <returns></returns>
        public Boolean EndOfStack()
        {            
            return Tracers.Count == 0;
        }
    }

    static public class RITraceManager
    {
        private readonly static DefaultTracer Default;
        private readonly static RequestObjectManager<TraceThreadInfo> RequestObjectManager;

        /// <summary>
        /// Initializes the <see cref="RITraceManager"/> class.
        /// </summary>
        static RITraceManager()
        {
            Default = new DefaultTracer();
            RequestObjectManager = new RequestObjectManager<TraceThreadInfo>(()=> new TraceThreadInfo());
        }

        /// <summary>
        /// Enters the method.
        /// </summary>
        /// <param name="tracer">The tracer.</param>
        /// <returns></returns>
        static public TraceThreadInfo EnterMethod(IRITrace tracer)
        {
            TraceThreadInfo threadInfo = RequestObjectManager.GetRequestObject(out bool bNew);
            if (bNew)
            {
                // must be first calling thread (parent)
                threadInfo.RootTracer = tracer;
            }

            threadInfo.Push(tracer);

            return threadInfo;
        }

        /// <summary>
        /// Enters the method.
        /// </summary>
        /// <returns></returns>
        static public TraceThreadInfo EnterMethod()
        {
            return EnterMethod(Default);
        }

        /// <summary>
        /// Exits the method.
        /// </summary>
        static public void ExitMethod()
        {
            TraceThreadInfo threadInfo = RequestObjectManager.GetRequestObject();

            threadInfo.Pop();
            if (threadInfo.EndOfStack())
            {
                RequestObjectManager.RemoveRequest();
            }
        }

        /// <summary>
        /// Gets the trace information.
        /// </summary>
        /// <returns></returns>
        static public TraceThreadInfo GetTraceInfo()
        {
            return RequestObjectManager.GetRequestObject();
        }

        /// <summary>
        /// Gets the active tracer.
        /// </summary>
        /// <returns></returns>
        static public IRITrace GetActiveTracer()
        {
            IRITrace tracer;            
            
            TraceThreadInfo threadInfo = RequestObjectManager.GetRequestObject();
            if (threadInfo != null)
            {
                tracer = threadInfo.Peek();
            }
            else
            {
                tracer = null;
            }

            return tracer ?? Default;
        }

        /// <summary>
        /// Gets the active logger.
        /// </summary>
        /// <value>
        /// The active logger.
        /// </value>
        static public IReflectInsight ActiveLogger
        {
            get
            {
                IReflectInsight logger;
                IRITrace tracer = GetActiveTracer();

                if (tracer != null)
                {
                    logger = tracer.Logger;
                }
                else
                {
                    logger = null;
                }

                return logger;
            }
        }

        /// <summary>
        /// Gets the name of the active.
        /// </summary>
        /// <value>
        /// The name of the active.
        /// </value>
        static public String ActiveName
        {
            get
            {
                String name;
                IRITrace tracer = GetActiveTracer();

                if (tracer != null)
                {
                    name = tracer.Name;
                }
                else
                {
                    name = String.Empty;
                }

                return name;
            }
        }
    }
}
