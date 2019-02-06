// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

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

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePropertyContainer"/> class.
        /// </summary>
        public MessagePropertyContainer()
        {
            Captions = new List<String>();
            Properties = new Dictionary<String, NameValueCollection>();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                Properties.Clear();
                Captions.Clear();
                Captions.Capacity = 0;
            }
        }

        /// <summary>
        /// Clears the specified caption.
        /// </summary>
        /// <param name="caption">The caption.</param>
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

        /// <summary>
        /// Clears the specified caption.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
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

        /// <summary>
        /// Adds the specified caption.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Adds the specified caption.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="nvcollection">The nvcollection.</param>
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

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
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

        /// <summary>
        /// Initializes the <see cref="RIExtendedMessageProperty"/> class.
        /// </summary>
        static RIExtendedMessageProperty()
        {
            AllRequests = new MessagePropertyContainer();
        }

        /// <summary>
        /// Clears the single message property.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        static public void ClearSingleMessageProperty(String caption, String property)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Clear(caption, property);
        }

        /// <summary>
        /// Clears the single message properties.
        /// </summary>
        /// <param name="caption">The caption.</param>
        static public void ClearSingleMessageProperties(String caption)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Clear(caption);
        }

        /// <summary>
        /// Clears the single message properties.
        /// </summary>
        static public void ClearSingleMessageProperties()
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Clear();
        }

        /// <summary>
        /// Clears the request property.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        static public void ClearRequestProperty(String caption, String property)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Clear(caption, property);
        }

        /// <summary>
        /// Clears the request properties.
        /// </summary>
        /// <param name="caption">The caption.</param>
        static public void ClearRequestProperties(String caption)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Clear(caption);
        }

        /// <summary>
        /// Clears the request properties.
        /// </summary>
        static public void ClearRequestProperties()
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Clear();
        }

        /// <summary>
        /// Clears all requests property.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        static public void ClearAllRequestsProperty(String caption, String property)
        {
            AllRequests.Clear(caption, property);
        }

        /// <summary>
        /// Clears all requests properties.
        /// </summary>
        /// <param name="caption">The caption.</param>
        static public void ClearAllRequestsProperties(String caption)
        {
            AllRequests.Clear(caption);
        }

        /// <summary>
        /// Clears all requests properties.
        /// </summary>
        static public void ClearAllRequestsProperties()
        {
            AllRequests.Clear();
        }

        /// <summary>
        /// Attaches to single message.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        static public void AttachToSingleMessage(String caption, String property, String value)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Add(caption, property, value);
        }

        /// <summary>
        /// Attaches to single message.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="nvcollection">The nvcollection.</param>
        static public void AttachToSingleMessage(String caption, NameValueCollection nvcollection)
        {
            RequestManager.GetRequestObject().SingleMessageProperties.Add(caption, nvcollection);
        }

        /// <summary>
        /// Attaches to request.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        static public void AttachToRequest(String caption, String property, String value)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Add(caption, property, value);
        }

        /// <summary>
        /// Attaches to request.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="nvcollection">The nvcollection.</param>
        static public void AttachToRequest(String caption, NameValueCollection nvcollection)
        {
            RequestManager.GetRequestObject().RequestMessageProperties.Add(caption, nvcollection);
        }

        /// <summary>
        /// Attaches to all requests.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        static public void AttachToAllRequests(String caption, String property, String value)
        {
            AllRequests.Add(caption, property, value);
        }

        /// <summary>
        /// Attaches to all requests.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="nvcollection">The nvcollection.</param>
        static public void AttachToAllRequests(String caption, NameValueCollection nvcollection)
        {
            AllRequests.Add(caption, nvcollection);
        }

        /// <summary>
        /// Appends the extended properties.
        /// </summary>
        /// <param name="propertyList">The property list.</param>
        /// <param name="container">The container.</param>
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

        /// <summary>
        /// Assigns to package.
        /// </summary>
        /// <param name="controlValue">The control value.</param>
        /// <param name="package">The package.</param>
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
