using System;
using System.Diagnostics;
using Plato.Extensions;

namespace ReflectSoftware.Insight
{
    static public class ReflectInsightService
    {
        private readonly static Object FLockObject;        
        static public Int32 ProcessId { get; private set; }
        static public UInt32 SessionId { get; private set; }

        static ReflectInsightService()
        {                       
            try 
            {
                DebugTextLoggerManager.OnStartup();

                FLockObject = new Object();                
                ProcessId = Process.GetCurrentProcess().Id;
                SessionId = (uint)Guid.NewGuid().ToString().BKDRHash();

                OnStartup();

                AppDomain.CurrentDomain.ProcessExit += OnShutdown;
                RIEventManager.OnConfigChange += OnConfigFileChange;
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static ReflectInsightService.ctor()");
            }
        }
        
        static private void OnStartup()
        {
            try
            {                                                                
                MessageQueue.OnStartup();
                MessageManager.OnStartup();
                RIListenerGroupManager.OnStartup();
                DebugManager.OnStartup();
                RILogManager.OnStartup();
                RIMessageColors.OnStartup();
                TraceMethod.OnStartup();
                ReflectInsight.OnStartup();                
                RIEventManager.DoOnStartup();
                                
                RITraceListener.OnStartup();
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static ReflectInsightService.OnStartup()");
            }
        }
        
        static private void OnShutdown(Object sender, EventArgs e)
        {            
            OnShutdown();
        }

        static private void OnShutdown()
        {
            try
            {
                AppDomain.CurrentDomain.ProcessExit -= OnShutdown;
                RIEventManager.OnConfigChange -= OnConfigFileChange;

                RIEventManager.DoOnShutdown();  
                ReflectInsight.OnShutdown();
                DebugManager.OnShutdown();                
                RIMessageColors.OnShutdown();
                RILogManager.OnShutdown();
                RIListenerGroupManager.OnShutdown();                
                ReflectInsightConfig.OnShutdown();
                TraceMethod.OnShutdown();

                RITraceListener.OnShutdown();                            
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static ReflectInsightService.OnShutdown()");
            }
            finally
            {
                RIExceptionManager.OnShutdown();
                DebugTextLoggerManager.OnShutdown();
            }
        }
        
        static private void OnConfigFileChange()
        {
            try
            {
                lock (FLockObject)
                {
                    RIExceptionManager.OnConfigFileChange();
                    MessageQueue.OnConfigFileChange();
                    MessageManager.OnConfigFileChange();
                    RIListenerGroupManager.OnConfigFileChange();
                    RILogManager.OnConfigFileChange();
                    RIMessageColors.OnConfigFileChange();
                    TraceMethod.OnConfigFileChange();
                    ReflectInsight.OnConfigFileChange();              
                    RIEventManager.DoOnServiceConfigChange();
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static ReflectInsightService.OnConfigFileChange()");
            }
        }

        static public void Initialize()
        {
            // don't remove this
        }
    }
}
