// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Configuration;
using Plato.Extensions;
using ReflectSoftware.Insight.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ReflectSoftware.Insight
{
    /// <summary>
    /// Types for the configuration mode.
    /// </summary>
    public enum ConfigurationMode
    {
        Application,
        ApplicationExternal,
        External,
        Document
    }
    
    /// <summary>
    /// This class is used to obtain configuration settings used by the ReflectInsightDispatcher. In
    /// addition, it also events subscribers that the active configuration file has changed.
    /// </summary>
	public class ReflectInsightConfig
	{                
        private readonly static object Lockobject;                 
        private static ReflectInsightConfig FDefaultConfig;        
		private static ReflectInsightConfig FAssignedConfig;        
		private static FileSystemWatcher FAppConfigFileWatcher;        
        private static FileSystemWatcher FExternConfigFileWatcher;
        private static DateTime FLastFileChangeTimestamp;
        
        private readonly Dictionary<string, NameValueCollection> FSubsections;
                
        internal static ConfigurationMode CurrentConfigurationMode { get; private set; }
        internal static string LastConfigFullPath { get; private set; }
        internal static bool IgnorePhysicalConfigChange { get; set; }
        
        public XmlNode XmlSection { get; private set; }        
        public static ConfigurationControl Control { get; private set; }

        static ReflectInsightConfig()
        {
            Lockobject = new object();
            CurrentConfigurationMode = ConfigurationMode.Application;                        
            LastConfigFullPath = string.Empty;
            FDefaultConfig = new ReflectInsightConfig(null);
            Control = new ConfigurationControl();
            FAssignedConfig = null;
            FAppConfigFileWatcher = null;
            FExternConfigFileWatcher = null;
            IgnorePhysicalConfigChange = false;            
            FLastFileChangeTimestamp = DateTime.MinValue;
        }

        static internal void OnShutdown()
        {
            DisposeAppConfigFileWatcher();
            DisposeExternConfigFileWatcher();

            FDefaultConfig = null;
            FAssignedConfig = null;
        }

        internal ReflectInsightConfig(XmlNode section)
        {
            XmlSection = section;
            FSubsections = new Dictionary<string, NameValueCollection>();            
        }

        private static string AppConfigFullFileName
		{
			get { return ConfigHelper.GetRootConfigFile().ToLower(); }
		}

        private static void OnConfigFileChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                FileSystemWatcher fileWatcher = (source as FileSystemWatcher);
                if (fileWatcher != null) fileWatcher.EnableRaisingEvents = false;

                try
                {
                    DateTime newFileTimestamp = File.GetLastWriteTime(e.FullPath);
                    if (newFileTimestamp != FLastFileChangeTimestamp)
                    {
                        FLastFileChangeTimestamp = newFileTimestamp;
                        DoOnConfigFileChanged(e);
                    }
                }
                finally
                {
                    if (fileWatcher != null) fileWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static ReflectInsightConfig.OnConfigFileChanged()");
            }
        }
        
        private static void DisposeAppConfigFileWatcher()
        {
            lock (Lockobject)
            {
                if (FAppConfigFileWatcher == null)
                {
                    return;
                }

                FAppConfigFileWatcher.EnableRaisingEvents = false;
                FAppConfigFileWatcher.Changed -= OnConfigFileChanged;
                FAppConfigFileWatcher.Created -= OnConfigFileChanged;
                FAppConfigFileWatcher.Deleted -= OnConfigFileChanged;
                FAppConfigFileWatcher.Dispose();
                FAppConfigFileWatcher = null;
            }
        }
        
        private static void DisposeExternConfigFileWatcher()
        {
            lock (Lockobject)
            {
                if (FExternConfigFileWatcher == null)
                {
                    return;
                }

                FExternConfigFileWatcher.EnableRaisingEvents = false;
                FExternConfigFileWatcher.Changed -= OnConfigFileChanged;
                FExternConfigFileWatcher.Created -= OnConfigFileChanged;
                FExternConfigFileWatcher.Deleted -= OnConfigFileChanged;
                FExternConfigFileWatcher.Dispose();
                FExternConfigFileWatcher = null;
            }
        }
        
        private static void CreateAppConfigFileWatcher()
        {
            lock (Lockobject)
            {
                DisposeAppConfigFileWatcher();

                FAppConfigFileWatcher = new FileSystemWatcher();
                try
                {
                    FAppConfigFileWatcher.BeginInit();
                    try
                    {
                        FAppConfigFileWatcher.Path = AppDomain.CurrentDomain.BaseDirectory;
                        FAppConfigFileWatcher.Filter = Path.GetFileName(AppConfigFullFileName);
                        FAppConfigFileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size;
                        FAppConfigFileWatcher.Changed += OnConfigFileChanged;
                        FAppConfigFileWatcher.Created += OnConfigFileChanged;
                        FAppConfigFileWatcher.Deleted += OnConfigFileChanged;
                        FAppConfigFileWatcher.EnableRaisingEvents = true;
                    }
                    finally
                    {
                        FAppConfigFileWatcher.EndInit();
                    }
                }
                catch (Exception ex)
                {                 
                    DisposeAppConfigFileWatcher();
                    RIExceptionManager.Publish(ex, string.Format("ReflectInsightConfig: Cannot create FileSystemWatcher for file: {0}", AppConfigFullFileName));
                }
            }
        }
        
        private static void CreateExternConfigFileWatcher(string filePath)
        {
            lock (Lockobject)
            {
                DisposeExternConfigFileWatcher();
                
                FExternConfigFileWatcher = new FileSystemWatcher();
                try
                {
                    FExternConfigFileWatcher.BeginInit();
                    try
                    {
                        FExternConfigFileWatcher.Path = Path.GetDirectoryName(filePath);
                        FExternConfigFileWatcher.Filter = Path.GetFileName(filePath);
                        FExternConfigFileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size;
                        FExternConfigFileWatcher.Changed += OnConfigFileChanged;
                        FExternConfigFileWatcher.Created += OnConfigFileChanged;
                        FExternConfigFileWatcher.Deleted += OnConfigFileChanged;
                        FExternConfigFileWatcher.EnableRaisingEvents = true;
                    }
                    finally
                    {
                        FExternConfigFileWatcher.EndInit();
                    }
                }
                catch (Exception ex)
                {                    
                    DisposeExternConfigFileWatcher();
                    RIExceptionManager.Publish(ex, string.Format("ReflectInsightConfig: Cannot create FileSystemWatcher for file: {0}", filePath));
                }
            }
        }
        
        private static ReflectInsightConfig GetActiveApplicationConfig(ReflectInsightConfig appConfig, out string activeFileName)
        {
            activeFileName = AppConfigFullFileName;

            if (appConfig == null)
            {
                return null;
            }

            var externalConfigSource = appConfig.GetAttribute(".", "externalConfigSource", string.Empty);
            if (!string.IsNullOrWhiteSpace(externalConfigSource))
            {
                var fullPathConfigFile = RIUtils.DetermineParameterPath(externalConfigSource);
                if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(externalConfigSource)))
                {
                    fullPathConfigFile = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, externalConfigSource);
                }
                
                // just in case someone is trying to play smart by assigning the external source name 
                // to the App Config file name, just ignore and assume the app config file only

                if (fullPathConfigFile.ToLower() != AppConfigFullFileName.ToLower())
                {
                    appConfig = ReadAndCreateConfigObject(fullPathConfigFile);
                    activeFileName = fullPathConfigFile;
                }
            }

            return appConfig;
        }
        
        private static void SetApplicationConfigFile()
        {
            lock (Lockobject)
            {
                DisposeExternConfigFileWatcher();
                CreateAppConfigFileWatcher();

                var activeFileName = (string)null;
                ReflectInsightConfig appConfig = ReadAndCreateConfigObject(AppConfigFullFileName);
                ReflectInsightConfig activeConfig = GetActiveApplicationConfig(appConfig, out activeFileName);
                
                LastConfigFullPath = activeFileName;
                CurrentConfigurationMode = ConfigurationMode.Application;

                if (LastConfigFullPath.ToLower() != AppConfigFullFileName.ToLower())
                {
                    CurrentConfigurationMode = ConfigurationMode.ApplicationExternal;
                    CreateExternConfigFileWatcher(LastConfigFullPath);
                }

                SetAssignedConfig(activeConfig);
            }
        }
		
        private static void DoOnConfigFileChanged(FileSystemEventArgs e)
		{
            if (IgnorePhysicalConfigChange)
            {
                return;
            }

            var bDoOnConfigChange = false;

            lock (Lockobject)
            {                
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Deleted:
                    case WatcherChangeTypes.Changed:                    
                    {                                                                                    
                        ReflectInsightConfig appConfig = ReadAndCreateConfigObject(e.FullPath);
                        if (appConfig != null && appConfig.GetBaseConfigChangeAttribute("enabled", "true") == "false")
                        {
                            break;
                        }
                            
                        bDoOnConfigChange = true;
                                                                                    
                        // if config mode is either ApplicationExternal or External, then do the following 

                        if (CurrentConfigurationMode == ConfigurationMode.Application || e.FullPath.ToLower() != LastConfigFullPath.ToLower())
                        {
                            // Although the mode can either be Application or Application External,
                            // the app config file is the only file that has changed if we get here.

                            var activeFileName = (string)null;
                            appConfig = GetActiveApplicationConfig(appConfig, out activeFileName);
                                
                            if (e.FullPath.ToLower() != activeFileName.ToLower()) 
                            {
                                // this tells us that the app config is not the active config file
                                // and/or that the last external app config is not the same as the previous external app config

                                if (activeFileName.ToLower() != LastConfigFullPath.ToLower())
                                {
                                    LastConfigFullPath = activeFileName;
                                    CurrentConfigurationMode = ConfigurationMode.ApplicationExternal;
                                    CreateExternConfigFileWatcher(LastConfigFullPath);
                                }
                                else
                                {
                                    // only the app config has changed but external remains the same
                                    bDoOnConfigChange = false;
                                }
                            }
                            else
                            {
                                LastConfigFullPath = activeFileName;
                                CurrentConfigurationMode = ConfigurationMode.Application;
                                DisposeExternConfigFileWatcher();
                            }
                        }

                        if (bDoOnConfigChange)
                        {
                            SetAssignedConfig(appConfig);
                        }
                    }

                    break;
                }
            }

            if (bDoOnConfigChange)
            {
                Control.ForceConfigChange();
            }
		}
        
        private static ReflectInsightConfig ReadAndCreateConfigObject(string configFile)
		{
            var exception = (Exception)null;

            var attempts = 5;
            while (true)
            {
                try
                {
                    using (FileStream fs = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (TextReader tr = new StreamReader(fs))
                    {
                        var xmlData = tr.ReadToEnd();
                        var doc = new XmlDocument() { PreserveWhitespace = true };
                        doc.LoadXml(xmlData);

                        return new ReflectInsightConfig(doc.SelectSingleNode("configuration/insightSettings"));
                    }
                }
                catch (FileNotFoundException)
                {
                    break;
                }
                catch (IOException ex)
                {
                    attempts--;
                    if (attempts < 0)
                    {
                        exception = ex;
                        break;

                    }

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    break;
                }
            }
            
            if(exception != null)
            {
                RIExceptionManager.Publish(exception, string.Format("ReflectInsightConfig: Error opening file: {0}", configFile));
            }

            return null;
		}
        
        private static void SetAssignedConfig(ReflectInsightConfig config)
        {
            lock (Lockobject)
            {
                FAssignedConfig = config ?? FDefaultConfig;
                FAssignedConfig.ConstructSubsections();
                RIEventManager.DoOnConfigSettingsInitialized(FAssignedConfig);
            }
        }
        
        internal static void SetExternalConfigurationMode(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException("xmlDoc");
            }

            lock (Lockobject)
            {
                ReflectInsightConfig appConfig = new ReflectInsightConfig(xmlDoc.SelectSingleNode("configuration/insightSettings"));

                DisposeAppConfigFileWatcher();
                DisposeExternConfigFileWatcher();

                CurrentConfigurationMode = ConfigurationMode.Document;
                LastConfigFullPath = string.Empty;

                SetAssignedConfig(appConfig);
            }

            Control.ForceConfigChange();
        }
        
        internal static void SetExternalConfigurationMode(XDocument xDoc)
        {
            if (xDoc == null)
            {
                throw new ArgumentNullException("xDoc");
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xDoc.ToString());

            SetExternalConfigurationMode(xmlDoc);
        }
        
        internal static void SetExternalConfigurationMode(string externalConfigFile)
        {
            var bForceConfigChange = false;

            lock (Lockobject)
            {
                // if external config mode is true and the last used config file is equal to 
                // the external config file then we already have it loaded 

                externalConfigFile = RIUtils.DetermineParameterPath(externalConfigFile.IfNullOrEmptyUseDefault(string.Empty));

                if (CurrentConfigurationMode != ConfigurationMode.External || externalConfigFile.ToLower() != LastConfigFullPath.ToLower())
                {
                    ReflectInsightConfig appConfig = ReadAndCreateConfigObject(externalConfigFile);

                    DisposeAppConfigFileWatcher();
                    DisposeExternConfigFileWatcher();
                    CreateExternConfigFileWatcher(externalConfigFile);

                    CurrentConfigurationMode = ConfigurationMode.External;
                    LastConfigFullPath = externalConfigFile;

                    SetAssignedConfig(appConfig);
                    bForceConfigChange = true;
                }
            }

            if (bForceConfigChange)
            {
                Control.ForceConfigChange();
            }
        }
         
        internal static void ClearExternalConfigurationMode()
        {
            var bForceConfigChange = false;

            lock (Lockobject)
            {
                if (CurrentConfigurationMode == ConfigurationMode.External || CurrentConfigurationMode == ConfigurationMode.Document)
                {
                    DisposeExternConfigFileWatcher();
                    SetApplicationConfigFile();
                    bForceConfigChange = true;
                }
            }

            if (bForceConfigChange)
            {
                Control.ForceConfigChange();
            }
        }
        
		public static ReflectInsightConfig Settings
		{
			get
			{
                lock (Lockobject)
                {
                    if (FAssignedConfig == null)
                    {
                        SetApplicationConfigFile();
                    }
                                                
                    return FAssignedConfig;
                }
			}
		}
        
		private static string GetAttribute( XmlNode objNode, string attName, string defaultValue )
		{						
			var rValue = defaultValue;
			if( objNode != null )
				if( objNode.Attributes[ attName ] != null )
				{
					rValue = objNode.Attributes[ attName ].Value;
                    if (string.Compare(rValue.Trim(), string.Empty, false) == 0)
                    {
                        rValue = defaultValue;
                    }
				}

			return rValue;
		}


        internal List<ListenerGroup> LoadListenerGroups()
        {
            var groups = new List<ListenerGroup>();

            if (XmlSection == null)
            {
                return groups;
            }

            var objNode = XmlSection.SelectSingleNode("./listenerGroups");
            if (objNode == null)
            {
                return groups;
            }

            foreach (XmlNode gNode in objNode.ChildNodes)
            {
                if (gNode.Name != "group") continue;
                if (gNode.Attributes["name"] == null) continue;

                var name = gNode.Attributes["name"].Value;                
                var bEnabled = (gNode.Attributes["enabled"] != null ? gNode.Attributes["enabled"].Value : "true") == "true";
                var bMask = (gNode.Attributes["maskIdentities"] != null ? gNode.Attributes["maskIdentities"].Value : "false") == "true";

                var group = new ListenerGroup(name, bEnabled, bMask);
                groups.Add(group);

                // let's add the destinations
                XmlNode destNodes  = gNode.SelectSingleNode("./destinations");
                if (destNodes != null)
                {
                    foreach (XmlNode dNode in destNodes.ChildNodes)
                    {
                        if (dNode.Name != "destination")
                        {
                            continue;
                        }

                        if (dNode.Attributes["name"] == null || dNode.Attributes["details"] == null)
                        {
                            continue;
                        }

                        bEnabled = (dNode.Attributes["enabled"] != null ? dNode.Attributes["enabled"].Value : "true") == "true";
                        var filter = dNode.Attributes["filter"] != null ? dNode.Attributes["filter"].Value : string.Empty;

                        group.AddDestination(dNode.Attributes["name"].Value, dNode.Attributes["details"].Value, bEnabled, filter);
                    }
                }

                // let's add the destination binding groups
                XmlNode destinationBindingGroupNodes = gNode.SelectSingleNode("./destinationBindingGroups");
                if (destinationBindingGroupNodes != null)
                {
                    foreach (XmlNode bNode in destinationBindingGroupNodes.ChildNodes)
                    {
                        if (bNode.Name != "destinationBindingGroup")
                        {
                            continue;
                        }

                        var bindingGroupName = bNode.Attributes["name"] != null ? bNode.Attributes["name"].Value : null;                        
                        var bindingGroup = group.GetDestinationBindingGroup(bindingGroupName);
                        if (bindingGroup == null)
                        {
                            bindingGroup = group.AddDestinationBindingGroup(bindingGroupName);
                        }
                        else
                        {
                            bindingGroup.ClearDestinationBindings();
                        }
                        
                        foreach (XmlNode dbNode in bNode.ChildNodes)
                        {
                            if (dbNode.Name != "destination")
                            {
                                continue;
                            }

                            if (dbNode.Attributes["name"] == null)
                            {
                                continue;
                            }

                            bindingGroup.AddDestinationBinding(dbNode.Attributes["name"].Value);
                        }
                    }
                }                                                
            }

            return groups;
        }
        
        internal List<RIInstance> LoadLogManagerInstances()
        {
            var instances = new List<RIInstance>();

            if (XmlSection == null)
            {
                return instances;
            }

            var objNode = XmlSection.SelectSingleNode("./logManager");
            if (objNode == null)
            {
                return instances;
            }

            foreach (XmlNode node in objNode.ChildNodes)
            {
                if (node.Name != "instance") continue;
                if (node.Attributes["name"] == null) continue;

                string name = node.Attributes["name"].Value;
                string category = node.Attributes["category"] != null ? node.Attributes["category"].Value : string.Empty;                                
                string bkColor = node.Attributes["bkColor"] != null ? node.Attributes["bkColor"].Value : string.Empty;
                string destinationBindingGroup = node.Attributes["destinationBindingGroup"] != null ? node.Attributes["destinationBindingGroup"].Value : string.Empty;

                instances.Add(new RIInstance(name, category, bkColor, destinationBindingGroup));
            }
            
            return instances;
        }
        
        internal Hashtable LoadMessageColors()
        {
            var messageColors = new Hashtable();

            if (XmlSection == null)
            {
                return messageColors;
            }

            var objNode = XmlSection.SelectSingleNode("./messageColors");
            if (objNode == null)
            {
                return messageColors;
            }

            foreach (XmlNode node in objNode.ChildNodes)
            {
                if (node.Name != "message") continue;
                if (node.Attributes["type"] == null) continue;
                if (node.Attributes["bkColor"] == null) continue;

                messageColors[node.Attributes["type"].Value] = node.Attributes["bkColor"].Value;
            }

            return messageColors;
        }
        
		internal FilterInfo GetFilterInfo(string name)
		{
            if (XmlSection == null)
            {
                return null;
            }

            var xPath = string.Format("./filters/filter[@name='{0}']", name );
			var objNode = XmlSection.SelectSingleNode(xPath);
            if (objNode == null)
            {
                return null;
            }
				
			// validate info				
			var sMode = GetAttribute( objNode, "mode", "include" ).Trim().ToLower();				
			if(!(sMode == "include" || sMode == "exclude")) return null;

			var mode = sMode == "include" ? FilterMode.Include : FilterMode.Exclude;
            var filter = new FilterInfo(name.Trim().ToLower(), mode);
									
			foreach(XmlNode childNode in objNode.ChildNodes)
			{
                if (childNode.Name != "method" || childNode.Attributes["type"] == null)
                {
                    continue;
                }

                string[] methods = childNode.Attributes["type"].Value.Split(',');
                foreach (string method in methods)
                {
                    filter.IDs.Add(method.Trim());
                }
			}
								
			return filter;
		}
        

		public string GetAttribute( string xPath, string attName, string defaultValue )
		{
            if (XmlSection == null)
            {
                return defaultValue;
            }

			return GetAttribute( XmlSection.SelectSingleNode( xPath ), attName, defaultValue );
		}

		public string GetListenerType( string listenerName )
		{            
            return GetAttribute(string.Format("./listeners/listener[@name='{0}']", listenerName), "type", null);
		}

        public string GetSenderName()
        {
            return GetSenderNameAttribute("name", AppDomain.CurrentDomain.FriendlyName.Replace(".exe", string.Empty));
        }

        public int GetExceptionEventTracker(int defaultTime)
        {
            if (!int.TryParse(GetExceptionEventTrackerAttribute("time", defaultTime.ToString()), out int time))
            {
                if (!int.TryParse(defaultTime.ToString(), out time))
                {
                    time = 20;
                }
            }

            return time;
        }

        public int GetMessageProcessingMaxValue(string attName, int defaultValue)
        {
            if (!int.TryParse(GetMessageProcessingAttribute(attName, defaultValue.ToString()), out int value))
            {
                if (!int.TryParse(defaultValue.ToString(), out value))
                {
                    value = 0;
                }
            }

            if (attName == "queueThrottleMaxLimit")
            {
                if (value < 100000) value = 100000;
                else if (value > 500000) value = 500000;
            }
            else if (attName == "dispatchChunkingMax")
            {
                if (value < 10000) value = 10000;
                else if (value > 50000) value = 50000;
            }
            
            return value;
        }

        public string GetFilesAttribute(string attName, string defaultValue)
        {
            return GetAttribute("./files", attName, defaultValue);
        }

        public string GetBaseConfigChangeAttribute(string attName, string defaultValue)
        {
            return GetAttribute("./baseSettings/configChange", attName, defaultValue);
        }

		public string GetSenderNameAttribute( string attName, string defaultValue )
		{
            return GetSubsection("baseSettings")[string.Format("senderName.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);            
		}

        public string GetExceptionEventTrackerAttribute(string attName, string defaultValue)
        {
            return GetSubsection("baseSettings")[string.Format("exceptionEventTracker.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);            
        }

        public string GetMessageProcessingAttribute(string attName, string defaultValue)
        {
            return GetSubsection("baseSettings")[string.Format("messageProcessing.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);
        }

		public string GetBasePropagateExceptionAttribute( string attName, string defaultValue )
		{			
            return GetSubsection("baseSettings")[string.Format("propagateException.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);
		}

        public string GetBaseTraceHttpRequestAttribute(string attName, string defaultValue)
        {
            return GetSubsection("baseSettings")[string.Format("traceHttpRequest.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);
        }
        
		public string GetBaseEnableAttribute( string attName, string defaultValue )
		{
            return GetSubsection("baseSettings")[string.Format("enable.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);            
		}

        public string GetBaseGlobalAttribute(string attName, string defaultValue)
        {
            return GetSubsection("baseSettings")[string.Format("global.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);
        }

        public string GetDebugWriterAttribute(string attName, string defaultValue)
        {
            return GetSubsection("baseSettings")[string.Format("debugWriter.{0}", attName)].IfNullOrEmptyUseDefault(defaultValue);            
        }

		public string GetListenerGroupsAttribute( string attName, string defaultValue )
		{
			return GetAttribute( "./listenerGroups", attName, defaultValue );
		}

		public string GetGroupAttribute( string gName, string attName, string defaultValue )
		{            
            return GetAttribute(string.Format("./listenerGroups/group[@name='{0}']", gName), attName, defaultValue);
		}

        public string GetExtensionAttribute(string aName, string attName, string defaultValue)
        {            
            return GetAttribute(string.Format("./extensions/extension[@name='{0}']", aName), attName, defaultValue);
        }

		public string GetFiltersAttribute( string attName, string defaultValue )
		{
			return GetAttribute( "./filters", attName, defaultValue );
		}

        public string GetLogManagerAttribute(string attName, string defaultValue)
        {
            return GetAttribute("./logManager", attName, defaultValue);
        }

        public string GetExceptionManagerAttribute(string attName, string defaultValue)
        {
            return GetAttribute("./exceptionManagement", attName, defaultValue);
        }


        public XmlNode GetNode(string xPath)
        {
            if (XmlSection == null) return null;

            return XmlSection.SelectSingleNode(xPath);
        }

        public XmlNodeList GetNodes(string xPath)
        {
            if (XmlSection == null) return null;

            return XmlSection.SelectNodes(xPath);
        }

        public NameValueCollection GetSubsection(string section)
        {
            lock (FSubsections)
            {
                return FSubsections.ContainsKey(section) ? FSubsections[section] : null;
            }
        }

        private void AddConstructedBaseSettings()
        {
            NameValueCollection nvc = new NameValueCollection();

            XmlNodeList nodes = GetNodes("./baseSettings/*");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    string name = node.Name;
                    if (name == "configChange")
                        continue;

                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        nvc[string.Format("{0}.{1}", name, attr.Name)] = attr.Value;
                    }
                }
            }

            AddSubsection("baseSettings", nvc);
        }
        
        private void AddConstructedSubsection(string subsection, string xPath)
        {
            var nodes = GetNodes(xPath);
            if (nodes == null)
            {
                return;
            }

            foreach (XmlNode node in nodes)
            {
                NameValueCollection nvc = new NameValueCollection();

                foreach (XmlAttribute attr in node.Attributes)
                {
                    nvc[attr.Name] = attr.Value;
                }

                string name = nvc["name"];
                if (nvc.Count > 0 && !string.IsNullOrWhiteSpace(name))
                {
                    AddSubsection(string.Format("{0}.{1}", subsection, name), nvc);
                }
            }
        }

        private void ConstructSubsections()
        {
            ClearSubsections();
            AddConstructedBaseSettings();
            AddConstructedSubsection("certificates", "./certificates/certificate");
            AddConstructedSubsection("routers", "./routers/router");
            AddConstructedSubsection("autoSaves", "./files/autoSave");
            AddConstructedSubsection("messagePatterns", "./messagePatterns/messagePattern");       
        }

        private void ClearSubsections()
        {
            lock (FSubsections)
            {
                FSubsections.Clear();                
            }
        }

        public NameValueCollection AddSubsection(string section, NameValueCollection nvc)
        {
            lock (FSubsections)
            {
                FSubsections[section] = nvc;
                return nvc;
            }
        }

        public void ClearSubsection(string section)
        {
            lock (FSubsections)
            {
                if (FSubsections.ContainsKey(section))
                {
                    FSubsections[section].Clear();
                }
            }
        }
        public void RemoveSubsection(string section)
        {
            lock (FSubsections)
            {
                FSubsections.Remove(section);
            }
        }
	}

    internal class ConfigurationHandler: IConfigurationSectionHandler
	{
		object IConfigurationSectionHandler.Create(object parent, object context, XmlNode section)
		{			
			return new ReflectInsightConfig(section);
		}
	}
}