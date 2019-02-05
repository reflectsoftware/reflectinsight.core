using ReflectSoftware.Insight.Common;
using System;
using System.Collections.Generic;

namespace ReflectSoftware.Insight
{
    public interface IRITrace
    {
        /// <summary>   Gets the name of the interface. </summary>
        String Name { get; }

        /// <summary>   Gets the logger assigned to this interface. </summary>
        IReflectInsight Logger { get; }        
    }

    internal class DefaultTracer : IRITrace
    {
        public IReflectInsight Logger { get; internal set; }
        public String Name { get; internal set; }
        
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

        public void Attached(UInt32 requestId)
        {
            RequestId = requestId;
            MethodException = null;
            Tracers = new Stack<IRITrace>();
        }

        public void Reset()
        {
            Tracers.Clear();            
        }

        public void Detached()
        {
        }

        public void Push(IRITrace tracer)
        {
            Tracers.Push(tracer);
        }

        public IRITrace Pop()
        {
            IRITrace tracer;
            if (!EndOfStack())
                tracer = Tracers.Pop();
            else
                tracer = null;

            return tracer;
        }

        public IRITrace Peek()
        {
            IRITrace tracer;
            if (!EndOfStack())
                tracer = Tracers.Peek();
            else
                tracer = null;

            return tracer;
        }

        public Boolean EndOfStack()
        {            
            return Tracers.Count == 0;
        }
    }

    static public class RITraceManager
    {
        private readonly static DefaultTracer Default;
        private readonly static RequestObjectManager<TraceThreadInfo> RequestObjectManager;
        
        static RITraceManager()
        {
            Default = new DefaultTracer();
            RequestObjectManager = new RequestObjectManager<TraceThreadInfo>(()=> new TraceThreadInfo());
        }

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

        static public TraceThreadInfo EnterMethod()
        {
            return EnterMethod(Default);
        }

        static public void ExitMethod()
        {
            TraceThreadInfo threadInfo = RequestObjectManager.GetRequestObject();

            threadInfo.Pop();
            if (threadInfo.EndOfStack())
            {
                RequestObjectManager.RemoveRequest();
            }
        }

        static public TraceThreadInfo GetTraceInfo()
        {
            return RequestObjectManager.GetRequestObject();
        }

        static public IRITrace GetActiveTracer()
        {
            IRITrace tracer;            
            
            TraceThreadInfo threadInfo = RequestObjectManager.GetRequestObject();
            if (threadInfo != null)
                tracer = threadInfo.Peek();
            else
                tracer = null;

            return tracer ?? Default;
        }

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
