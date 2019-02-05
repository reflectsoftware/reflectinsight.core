using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using ReflectSoftware.Insight.Common.Data;

namespace ReflectSoftware.Insight
{
    internal class MessagePropertyContainer: ICloneable
    {
        public List<String> Captions { get; private set; }
        public Dictionary<String, NameValueCollection> Properties { get; private set; }
        
        public MessagePropertyContainer()
        {
            Captions = new List<String>();
            Properties = new Dictionary<String, NameValueCollection>();
        }
        
        public void Clear()
        {
            lock (this)
            {
                Properties.Clear();
                Captions.Clear();
                Captions.Capacity = 0;
            }
        }
        
        public void Clear(String caption)
        {
            lock (this)
            {
                if (Captions.Contains(caption))
                {
                    Captions.Remove(caption);
                    Properties.Remove(caption);
                }
            }
        }
        
        public void Clear(String caption, String property)
        {
            lock (this)
            {
                if (Captions.Contains(caption))
                {
                    Properties[caption].Remove(property);                    
                }
            }
        }
        
        public void Add(String caption, String property, String value)
        {
            lock (this)
            {
                if (!Captions.Contains(caption))
                {
                    Captions.Add(caption);
                    Properties.Add(caption, new NameValueCollection());
                }

                Properties[caption][property] = value;
            }
        }
        
        public void Add(String caption, NameValueCollection nvcollection)
        {
            lock (this)
            {
                if (!Captions.Contains(caption))
                {
                    Captions.Add(caption);
                    Properties.Add(caption, new NameValueCollection());
                }

                Properties[caption].Add(nvcollection);
            }
        }
        
        public Object Clone()
        {
            lock (this)
            {
                MessagePropertyContainer clone = new MessagePropertyContainer();

                clone.Captions.AddRange(Captions);
                foreach (String caption in Captions)
                    clone.Properties.Add(caption, new NameValueCollection(Properties[caption]));

                return clone;
            }
        }
    }
    

    static public class RIExtendedMessageProperty
    {
        private readonly static MessagePropertyContainer AllRequests;

        static RIExtendedMessageProperty()
        {
            AllRequests = new MessagePropertyContainer();
        }

        static public void ClearSingleMessageProperty(String caption, String property)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Clear(caption, property);
        }

        static public void ClearSingleMessageProperties(String caption)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Clear(caption);
        }

        static public void ClearSingleMessageProperties()
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Clear();
        }

        static public void ClearRequestProperty(String caption, String property)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Clear(caption, property);
        }

        static public void ClearRequestProperties(String caption)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Clear(caption);
        }

        static public void ClearRequestProperties()
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Clear();
        }

        static public void ClearAllRequestsProperty(String caption, String property)
        {
            AllRequests.Clear(caption, property);
        }

        static public void ClearAllRequestsProperties(String caption)
        {
            AllRequests.Clear(caption);
        }

        static public void ClearAllRequestsProperties()
        {
            AllRequests.Clear();
        }

        static public void AttachToSingleMessage(String caption, String property, String value)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Add(caption, property, value);
        }

        static public void AttachToSingleMessage(String caption, NameValueCollection nvcollection)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Add(caption, nvcollection);
        }

        static public void AttachToRequest(String caption, String property, String value)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Add(caption, property, value);
        }

        static public void AttachToRequest(String caption, NameValueCollection nvcollection)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Add(caption, nvcollection);
        }

        static public void AttachToAllRequests(String caption, String property, String value)
        {
            AllRequests.Add(caption, property, value);
        }

        static public void AttachToAllRequests(String caption, NameValueCollection nvcollection)
        {
            AllRequests.Add(caption, nvcollection);
        }

        static private void AppendExtendedProperties(List<ReflectInsightExtendedProperties> propertyList, MessagePropertyContainer container)
        {
            if (container.Captions.Count > 0)
            {
                foreach (String caption in container.Captions)
                {
                    NameValueCollection properties = container.Properties[caption];
                    if (properties.Count > 0)
                    {
                        ReflectInsightExtendedProperties exProps = new ReflectInsightExtendedProperties();
                        propertyList.Add(exProps);

                        exProps.Caption = caption;
                        exProps.Properties = new NameValueCollection(properties);
                    }
                }
            }
        }

        static internal void AssignToPackage(ControlValues controlValue, ReflectInsightPackage package)
        {
            List<ReflectInsightExtendedProperties> propertyList = new List<ReflectInsightExtendedProperties>();

            lock (AllRequests)
            {
                AppendExtendedProperties(propertyList, AllRequests);
            }

            AppendExtendedProperties(propertyList, controlValue.RequestMessageProperties);
            AppendExtendedProperties(propertyList, controlValue.SingleMessageProperties);

            if (propertyList.Count > 0)
            {
                package.FExtPropertyContainer = new ReflectInsightPropertiesContainer(propertyList.ToArray());
                controlValue.ResetSingleRequestProperties();
            }
        }
    }
}
