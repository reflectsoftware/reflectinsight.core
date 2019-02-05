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

    /// <summary>
    /// 
    /// </summary>
    public class TraceMethod : ITraceMethod
    {
        private readonly static MethodInfo FSendMethodInfo;
        private static Boolean TraceHtttpRequest { get; set; }

        private Int32 LastIndentLevel { get; set; }
        private ControlValues ControlValues { get; set; }
        private TraceMethodState TraceStates { get; set; }

        /// <summary></summary>
        public IReflectInsight RI { get; internal set; }

        /// <summary></summary>
        public String Message { get; internal set; }

        /// <summary></summary>
        public Boolean Disposed { get; private set; }
        
        static TraceMethod()
        {
            FSendMethodInfo = typeof(ReflectInsight).GetMethod("_SendCustomData", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        }

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
        
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }
        
        static internal void OnShutdown()
        {
        }

        ///--------------------------------------------------------------------
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

        public void Execute(Action action, TraceMethodExceptionPolicy policy = TraceMethodExceptionPolicy.LogAndSwallowParentsPolicy)
        {
            Execute<Object>(() =>
            {
                action();
                return null;

            }, policy);
        }

        public Boolean ExceptionHandler(Exception ex, Func<Exception, Boolean> handler)
        {
            if (!TraceStates.ExceptionHandled && handler != null)
            {
                TraceStates.ExceptionHandled = handler(ex);
            }

            return TraceStates.ExceptionHandled;
        }

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
