// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

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

        /// <summary>
        /// Does the on send internal exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
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

        /// <summary>
        /// Does the on queue exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
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

        /// <summary>
        /// Does the on created instance.
        /// </summary>
        /// <param name="ri">The ri.</param>
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

        /// <summary>
        /// Does the on configuration settings initialized.
        /// </summary>
        /// <param name="settings">The settings.</param>
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

        /// <summary>
        /// Does the on configuration change.
        /// </summary>
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

        /// <summary>
        /// Does the on service configuration change.
        /// </summary>
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

        /// <summary>
        /// Does the on startup.
        /// </summary>
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

        /// <summary>
        /// Does the on shutdown.
        /// </summary>
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
