// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Configuration;

namespace RI.Messaging.ReadWriter.Implementation.TCP
{
    public class TCPConfigSetting : ConfigurationElement, ICloneable
    {        
        private const String NameProperty = "name";
        private const String HostNameProperty = "hostname";
        private const String PortProperty = "port";
        private const String ConnectionTimeoutProperty = "connectionTimeout";

        [ConfigurationProperty(NameProperty, IsRequired = true)]
        public String Name
        {
            get { return this[NameProperty] as String; }
            set { this[NameProperty] = value; }
        }

        [ConfigurationProperty(HostNameProperty, IsRequired = false)]
        public String HostName
        {
            get { return this[HostNameProperty] as String; }
            set { this[HostNameProperty] = value; }
        }

        [ConfigurationProperty(PortProperty, IsRequired = false, DefaultValue = 8081)]
        public Int32 Port
        {
            get { return (Int32)this[PortProperty]; }
            set { this[PortProperty] = value; }
        }

        [ConfigurationProperty(ConnectionTimeoutProperty, IsRequired = false, DefaultValue = 2000)]
        public Int32 ConnectionTimeout
        {
            get { return (Int32)this[ConnectionTimeoutProperty]; }
            set { this[ConnectionTimeoutProperty] = value; }
        }

        public Object Clone()
        {
            TCPConfigSetting element = new TCPConfigSetting();
            element.Name = Name;
            element.HostName = HostName;
            element.Port = Port;
            element.ConnectionTimeout = ConnectionTimeout;

            return element;
        }
    }

    [ConfigurationCollection(typeof(TCPConfigSetting))]
    public class TCPConfigSettings : ConfigurationElementCollection
    {        
        private const String PropertyName = "setting";

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
            return new TCPConfigSetting();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TCPConfigSetting)(element)).Name;
        }

        public TCPConfigSetting this[int idx]
        {
            get { return (TCPConfigSetting)BaseGet(idx); }
        }

        public new TCPConfigSetting this[String name]
        {
            get { return (TCPConfigSetting)BaseGet(name); }
        }
    }

    public class TCPConfiguration : ConfigurationSection
    {
        private const String ConfigurationCollectionProperty = "settings";     
        private const String ConfigurationSection = "tcpConfiguration";

        [ConfigurationProperty(ConfigurationCollectionProperty)]
        protected TCPConfigSettings TCPConfigurationElement
        {
            get { return ((TCPConfigSettings)(base[ConfigurationCollectionProperty])); }
        }

        public static TCPConfigSetting GetSetting(String name)
        {
            TCPConfiguration config = (TCPConfiguration)ConfigurationManager.GetSection(ConfigurationSection);
            return config.TCPConfigurationElement[name];
        }
    }
}
