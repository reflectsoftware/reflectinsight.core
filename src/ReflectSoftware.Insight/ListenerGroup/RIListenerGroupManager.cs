using System;
using System.Collections;
using System.Collections.Generic;

namespace ReflectSoftware.Insight
{
    /// <summary>   RIListenerGroupManager class. </summary>
    /// <remarks>   ReflectInsight Version 5.3. </remarks>

    public class RIListenerGroupManager
    {
        static private ListenerGroup FDefaultGroup;
        static private ListenerGroup FActiveGroup;
        private readonly static Hashtable FListenerGroups;
        
        static RIListenerGroupManager()
        {
            FListenerGroups = new Hashtable();
            ReflectInsightService.Initialize();
        }
          
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }
        
        static internal void OnShutdown()
        {
            ForceDisposeListenerGroups();

            FDefaultGroup = null;
            FActiveGroup = null;              
        }
        
        static private void ForceDisposeListenerGroups()
        {
            lock (FListenerGroups)
            {
                foreach (ListenerGroup group in FListenerGroups.Values)
                    group.InternalDispose(true);

                FListenerGroups.Clear();                
            }
        }
        
        static internal void OnConfigFileChange()
        {
            try
            {
                lock (FListenerGroups)
                {                    
                    RemoveConfigListenerGroups();
                    ObtainConfigListenerGroups();
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static RIListenerGroupManager.OnConfigFileChange()");                
            }
        }
        
        static private void RemoveConfigListenerGroups()
        {
            lock (FListenerGroups)
            {
                List<ListenerGroup> removeGroups = new List<ListenerGroup>();
                foreach (ListenerGroup group in FListenerGroups.Values)
                {
                    if (group.FromConfig)
                        removeGroups.Add(group);
                }

                foreach (ListenerGroup group in removeGroups)
                {
                    FListenerGroups.Remove(group.Name);
                    group.InternalDispose(false);
                }
                
                removeGroups.Clear();
                removeGroups.Capacity = 0;
            }
        }
        
        static private void ObtainConfigListenerGroups()
        {
            lock (FListenerGroups)
            {                                
                AddDefaultListenerGroupIfNecessary();
                
                // load groups from config file                
                List<ListenerGroup> groups = ReflectInsightConfig.Settings.LoadListenerGroups();
                foreach (ListenerGroup group in groups)
                    AddGroup(group, true);
                
                PrepareActiveGroup();
                
                groups.Clear();
                groups.Capacity = 0;
            }
        }
        
        static private void AddDefaultListenerGroupIfNecessary()
        {
            lock (FListenerGroups)
            {
                String defaultName = FDefaultGroup != null ? FDefaultGroup.Name : "_default";

                ListenerGroup group = (ListenerGroup)FListenerGroups[defaultName];
                if (group == null)
                {
                    // default group doesn't exist, add it
                    FDefaultGroup = new ListenerGroup("_default", true, false);
                    FDefaultGroup.AddDestination("_default", "Viewer");
                    AddGroup(FDefaultGroup, true);
                }
            }
        }
         
        static private void PrepareActiveGroup()
        {            
            lock (FListenerGroups)
            {
                String activeGroupName = ReflectInsightConfig.Settings.GetListenerGroupsAttribute("active", "_default");
                ListenerGroup group = (ListenerGroup)FListenerGroups[activeGroupName];
                if (group == null)
                {
                    activeGroupName = ReflectInsightConfig.Settings.GetListenerGroupsAttribute("active", "_default");
                    group = (ListenerGroup)FListenerGroups[activeGroupName];
                    if (group == null)
                        group = FDefaultGroup;
                }

                SetActiveListenerGroup(group);
            }
        }
        
        static private void SetDefaultListenerGroup(ListenerGroup group)
        {
            lock (FListenerGroups)
            {
                if (group == null)
                    return;

                ListenerGroup gNode = (ListenerGroup)FListenerGroups[group.Name];
                if (gNode == null || group.IsDefault)
                    return;

                if (Object.ReferenceEquals(FDefaultGroup, FActiveGroup))
                    SetActiveListenerGroup(group);

                FDefaultGroup = group;
            }
        }
             
        static private void SetActiveListenerGroup(ListenerGroup group)
        {
            lock (FListenerGroups)
            {
                MessageQueue.WaitUntilNoMessages(100);
                
                if (group == null)
                    return;

                FActiveGroup = group;
            }
        }
            
        static private void AddGroup(ListenerGroup group, Boolean bFromConfig)
        {
            if (group == null)
                return;

            lock (FListenerGroups)
            {
                ListenerGroup gNode = (ListenerGroup)FListenerGroups[group.Name];
                if(gNode != null && !Object.ReferenceEquals(gNode, group))
                {
                    // from this point the groups names are similar but are not the same object

                    // we cannot replace a non-config'd group with a config'd group
                    if (!gNode.FromConfig && bFromConfig)
                    {
                        group.InternalDispose(false);
                        return;
                    }

                    // make sure the one we are replacing was not active.
                    // if active, then the group that is replacing will be active
                    
                    if (gNode.IsActive)
                        SetActiveListenerGroup(group);

                    // set to default if the one being replaced was default
                    if (gNode.IsDefault)
                        FDefaultGroup = group;

                    group.InternalDispose(false);
                }
                
                group.FromConfig = bFromConfig;
                FListenerGroups[group.Name] = group;
            }
        }
           
        static public ListenerGroup Add(String name, Boolean bEnabled, Boolean bMaskIdentities)
        {
            ListenerGroup group = new ListenerGroup(name, bEnabled, bMaskIdentities);
            AddGroup(group, false);

            return group;
        }
		   
        static public ListenerGroup Add(String name, Boolean bEnabled)
        {
            return Add(name, bEnabled, false);
        }

        static public ListenerGroup Add(String name)
        {
            return Add(name, true, false);
        }

        static public Boolean Remove(ListenerGroup group)
        {
            lock (FListenerGroups)
            {
                ListenerGroup gNode = (ListenerGroup)FListenerGroups[group.Name];
                if (gNode == null)
                    return false;

                if (!Object.ReferenceEquals(gNode, group))
                    return false;
                                
                if (Object.ReferenceEquals(gNode, group))
                {
                    FListenerGroups.Remove(group.Name);
                    group.InternalDispose(false);
                    
                    AddDefaultListenerGroupIfNecessary();
                }

                if (group.IsActive)
                    PrepareActiveGroup();

                return true;
            }
        }

        static public Boolean Remove(String name)
        {
            lock (FListenerGroups)
            {
                ListenerGroup group = (ListenerGroup)FListenerGroups[name];
                if (group != null)
                    return Remove(group);

                return false;
            }
        }

        static public ListenerGroup Get(String name)
        {
            lock (FListenerGroups)
            {
                ListenerGroup group = (ListenerGroup)FListenerGroups[name];
                if (group == null)
                    return null;

                return group;
            }
        }

        static public ListenerGroup[] ListenerGroups
        {
            get
            {
                List<ListenerGroup> list = new List<ListenerGroup>();
                lock (FListenerGroups)
                {
                    foreach (ListenerGroup group in FListenerGroups.Values)
                        list.Add(group);
                }

                return list.ToArray();
            }
        }

        static public ListenerGroup ActiveGroup
        {
            get { return FActiveGroup; }
            set { SetActiveListenerGroup(value); }
        }

        static public ListenerGroup DefaultGroup
        {
            get { return FDefaultGroup; }
            set { SetDefaultListenerGroup(value); }
        }
    }
}
