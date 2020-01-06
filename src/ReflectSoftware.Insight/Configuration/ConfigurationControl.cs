// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Xml;
using System.Xml.Linq;

namespace ReflectSoftware.Insight
{
    /// <summary>
    /// Managing configuration control
    /// </summary>
    public class ConfigurationControl
    {        
        internal ConfigurationControl()
        {
            ReflectInsightService.Initialize();
        }

        public ConfigurationMode CurrentConfigurationMode
        {
            get { return ReflectInsightConfig.CurrentConfigurationMode; }
        }

        public string LastConfigFullPath 
        {
            get { return ReflectInsightConfig.LastConfigFullPath; }
        }

        public bool IgnorePhysicalConfigChange
        {
            get { return ReflectInsightConfig.IgnorePhysicalConfigChange; }
            set { ReflectInsightConfig.IgnorePhysicalConfigChange = value; }
        }

        public void SetExternalConfigurationMode(XmlDocument xmlDoc)
        {
            ReflectInsightConfig.SetExternalConfigurationMode(xmlDoc);
        }

        public void SetExternalConfigurationMode(XDocument xDoc)
        {
            ReflectInsightConfig.SetExternalConfigurationMode(xDoc);
        }

        public void SetExternalConfigurationMode(string externalConfigFile)
        {
            ReflectInsightConfig.SetExternalConfigurationMode(externalConfigFile);
        }

        public void ClearExternalConfigurationMode()
        {
            ReflectInsightConfig.ClearExternalConfigurationMode();
        }

        public void ForceConfigChange()
        {
            RIEventManager.DoOnConfigChange();
        }
    }
}
