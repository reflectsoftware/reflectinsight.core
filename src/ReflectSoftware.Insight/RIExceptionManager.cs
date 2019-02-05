using Plato.Extensions;
using Plato.Miscellaneous;
using System;
using System.Collections.Specialized;

namespace ReflectSoftware.Insight
{
    static public class RIExceptionManager
    {
        // TODO: static private ExceptionManagerBase FExceptionManagerComposite;     
        // TODO: static private IExceptionPublisher FDefaultExceptionPublisher;        
        static private TimeSpan FEventTracker;

        static RIExceptionManager()
        {
            // TODO: FExceptionManagerComposite = new ExceptionManagerBase(ReflectInsightConfig.LastConfigFullPath, "./configuration/insightSettings/exceptionManagement", "ReflectInsight Library", false, false);
            // TODO: FDefaultExceptionPublisher = new ExceptionEventPublisher("ReflectInsight Library");

            ReflectInsightService.Initialize();
        }        

        static internal void OnStartup()
        {
            OnConfigFileChange();
        }

        static internal void OnConfigFileChange()
        {
            FEventTracker = new TimeSpan(0, ReflectInsightConfig.Settings.GetExceptionEventTracker(20), 0);
            // TODO: FExceptionManagerComposite.ForceLoadConfigFile();
            // TODO: FExceptionManagerComposite.OnConfigFileChange();
        }

        static internal void OnShutdown()
        {
            // TODO: 
            //if (FExceptionManagerComposite != null)
            //{
            //    FExceptionManagerComposite.Dispose();
            //    FExceptionManagerComposite = null;
            //}

            //if (FDefaultExceptionPublisher != null)
            //{
            //    MiscHelper.DisposeObject(FDefaultExceptionPublisher);
            //    FDefaultExceptionPublisher = null;
            //}
        }

        static public Boolean CanEvent(Exception ex)
        {
            return TimeEventTracker.CanEvent((Int32)ex.Message.BKDRHash(), FEventTracker);
        }

        static public void Publish(Exception ex, NameValueCollection additionalParameters)
        {
            try
            {
                // add time stamps
                DateTime now = DateTime.Now;
                additionalParameters = additionalParameters ?? new NameValueCollection();
                additionalParameters.Add("Local Time", now.ToString("yyyy/MM/dd, HH:mm:ss.fff"));
                additionalParameters.Add("UTC", now.ToUniversalTime().ToString("yyyy/MM/dd, HH:mm:ss.fff"));            
                                  
                //if (FExceptionManagerComposite.Mode == PublisherManagerMode.Off)
                //    return;

                //if (PublisherCount != 0)
                //{
                //    FExceptionManagerComposite.Publish(ex, additionalParameters);
                //    return;
                //}

                //FDefaultExceptionPublisher.Publish(ex, additionalParameters);
            }
            catch (Exception)
            {
                // nothing we can do just swallow
            }
        }

        static public void Publish(Exception ex, String additionalInfo)
        {
           NameValueCollection additionalParams = new NameValueCollection();
            additionalParams.Add("Additional Info", additionalInfo);

            Publish(ex, additionalParams);
        }

        static public void Publish(Exception ex)
        {
            Publish(ex, new NameValueCollection());
        }

        static public void PublishIfEvented(Exception ex, NameValueCollection additionalParameters)
        {
            if (CanEvent(ex)) Publish(ex, additionalParameters);
        }

        static public void PublishIfEvented(Exception ex, String additionalInfo)
        {
            if (CanEvent(ex)) Publish(ex, additionalInfo);
        }

        static public void PublishIfEvented(Exception ex)
        {
            if (CanEvent(ex)) Publish(ex);
        }

        static public void RemovePublisherByType(Type pType)
        {
            // TODO: FExceptionManagerComposite.RemovePublisherByType(pType);
        }

        static public void RemovePublisherByName(String name)
        {
            // TODO: FExceptionManagerComposite.RemovePublisherByName(name);
        }

        // TODO:         
        //static public void AddPublisher(IExceptionPublisher publisher, NameValueCollection parameters)
        //{
        //    FExceptionManagerComposite.AddPublisher(publisher, parameters);
        //}

        static public Int32 PublisherCount
        {
            get { return 0; } // TODO:  return FExceptionManagerComposite.PublisherCount; }
        }

        // TODO: 
        //static public PublisherInfo[] PublisherInfos
        //{
        //    get { return FExceptionManagerComposite.PublisherInfos; }
        //}

        // TODO: 
        //static public IExceptionPublisher[] Publishers
        //{
        //    get { return FExceptionManagerComposite.Publishers; }
        //}
    }
}
