// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Configuration;

namespace RI.Messaging.ReadWriter
{
    public class MessageConfigImplementation : ConfigurationElement, ICloneable
    {        
        private const String NameProperty = "name";        
        private const String TypeProperty = "type";

        [ConfigurationProperty(NameProperty, IsRequired = true)]
        public String Name
        {
            get { return this[NameProperty] as String; }
            set { this[NameProperty] = value; }
        }

        [ConfigurationProperty(TypeProperty, IsRequired = true)]
        public String ImplementationType
        {
            get { return this[TypeProperty] as String; }
            set { this[TypeProperty] = value; }
        }

        public Object Clone()
        {
            MessageConfigImplementation element = new MessageConfigImplementation()
            {
                Name = Name, ImplementationType = ImplementationType
            };

            return element;
        }
    }

    [ConfigurationCollection(typeof(MessageConfigImplementation))]
    public class MessageConfigImplementations : ConfigurationElementCollection
    {        
        private const String PropertyName = "implementation";

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMapAlternate; }
        }

        protected override String ElementName
        {
            get { return PropertyName; }
        }

        protected override bool IsElementName(String elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageConfigImplementation();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MessageConfigImplementation)(element)).Name;
        }

        public MessageConfigImplementation this[int idx]
        {
            get { return (MessageConfigImplementation)BaseGet(idx); }
        }

        public new MessageConfigImplementation this[String name]
        {
            get { return (MessageConfigImplementation)BaseGet(name); }
        }
    }

    public class ReadWriterConfiguration : ConfigurationSection
    {
        private const String ConfigurationCollectionProperty = "implementations";        
        private const String ConfigurationSection = "readerWriterConfiguration";

        [ConfigurationProperty(ConfigurationCollectionProperty)]
        protected MessageConfigImplementations MessageReaderWriterConfigSetting
        {
            get { return ((MessageConfigImplementations)(base[ConfigurationCollectionProperty])); }
        }

        public static MessageConfigImplementation GetImplementation(String name)
        {
            ReadWriterConfiguration config = (ReadWriterConfiguration)ConfigurationManager.GetSection(ConfigurationSection);
            return config.MessageReaderWriterConfigSetting[name];
        }
    }
}
