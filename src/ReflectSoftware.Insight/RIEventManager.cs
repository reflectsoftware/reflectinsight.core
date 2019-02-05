using System;

namespace ReflectSoftware.Insight
{
    static public class RIEventManager
    {        
        static public event Action<Exception> OnAllExceptions;        
        static public event Action<Exception> OnQueueException;        
        static public event Action<Exception> OnSendInternalException;        
        static public event Action<ReflectInsight> OnCreatedInstance;        
        static public event Action<ReflectInsightConfig> OnConfigSettingsInitialized;        
        static public event Action OnConfigChange;        
        static public event Action OnServiceConfigChange;        
        static public event Action OnStartup;        
        static public event Action OnShutdown;
        
        static internal void DoOnSendInternalException(Exception ex)
        {
            try
            {
                OnSendInternalException?.Invoke(ex);
                OnAllExceptions?.Invoke(ex);
            }
            catch (Exception exc)
            {
                RIExceptionManager.Publish(exc, "Failed during: RIEventManager.DoOnSendInternalException()");
            }
        }
        
        static internal void DoOnQueueException(Exception ex)
        {
            try
            {
                OnQueueException?.Invoke(ex);
                OnAllExceptions?.Invoke(ex);
            }
            catch (Exception exc)
            {
                RIExceptionManager.Publish(exc, "Failed during: RIEventManager.DoOnQueueException()");
            }
        }

        static internal void DoOnCreatedInstance(ReflectInsight ri)
        {
            try
            {
                OnCreatedInstance?.Invoke(ri);
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: RIEventManager.DoOnCreatedInstance()");
            }
        }

        static internal void DoOnConfigSettingsInitialized(ReflectInsightConfig settings)
        {
            try
            {
                OnConfigSettingsInitialized?.Invoke(settings);
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: RIEventManager.DoOnConfigSettingsInitialized()");
            }
        }
        
        static internal void DoOnConfigChange()
        {
            try
            {
                OnConfigChange?.Invoke();
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: RIEventManager.DoOnConfigChange()");
            }
        }
        
        static internal void DoOnServiceConfigChange()
        {
            try
            {
                OnServiceConfigChange?.Invoke();
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: RIEventManager.DoOnServiceConfigChange()");
            }
        }
        
        static internal void DoOnStartup()
        {
            try
            {
                OnStartup?.Invoke();
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: RIEventManager.DoOnStartup()");
            }
        }
        
        static internal void DoOnShutdown()
        {
            try
            {
                OnShutdown?.Invoke();
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: RIEventManager.DoOnShutdown()");
            }
        }
    }
}
