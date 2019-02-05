using ReflectSoftware.Insight.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace ReflectSoftware.Insight
{
    public class RILogManagerNode: IDisposable
    { 
        public String Name { get; internal set; }
        public IReflectInsight Instance { get; internal set; }
        public Boolean Disposed { get; private set; }
        
        public RILogManagerNode(String name, IReflectInsight ri) 
        {
            Disposed = false;
            Name = name;
            Instance = ri;
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);
                 
                    if (Instance != null)
                    {
                        Instance.Dispose();
                        Instance = null;
                    }
                }
            }
        }
    }

    static public class RILogManager
    {
        static private Hashtable FInstances;
        static private RILogManagerNode FDefault;

        static RILogManager()
        {
            FInstances = new Hashtable();
            ReflectInsightService.Initialize();
        }
        
        static private void FreeInstances()
        {
            if (FInstances != null)
            {
                foreach (RILogManagerNode node in FInstances.Values)
                    node.Dispose();

                FInstances = null;
                FDefault = null;
            }
        }
        
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }
        
        static internal void OnShutdown()
        {
            FreeInstances();
        }
        
        static internal void OnConfigFileChange()
        {
            try
            {
                lock (FInstances)
                {
                    ObtainConfigInstances();
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static RILogManager.OnConfigFileChange()");    
            }
        }

        static private IReflectInsight CreateInstance(RIInstance instance)
        {
            IReflectInsight ri = new ReflectInsight(instance.Category);            
            ri.BackColor = RIPastelBackColor.GetColorByName(instance.BkColor);
            ri.SetDestinationBindingGroup(instance.DestinationBindingGroup);

            return ri;
        }
        
        static private RILogManagerNode GetNode(String name)
        {
            return (RILogManagerNode)FInstances[name];
        }
        
        static private void EstablishDefault()
        {
            lock (FInstances)
            {
                String defaultRi = ReflectInsightConfig.Settings.GetLogManagerAttribute("default", "default");
                RILogManagerNode node = GetNode(defaultRi);
                   
                if (node == null)
                {
                    Add(defaultRi, new ReflectInsight(defaultRi)); 
                    node = GetNode(defaultRi);
                }

                FDefault = node;
            }
        }
        
        static private void ObtainConfigInstances()
        {
            lock (FInstances)
            {                                
                List<RIInstance> list = ReflectInsightConfig.Settings.LoadLogManagerInstances();
                foreach (RIInstance instance in list)
                    Add(instance); 

                EstablishDefault();
            }
        }

        static public Boolean Remove(String name, Boolean bDispose)
        {
            lock (FInstances)
            {
                RILogManagerNode node = GetNode(name);
                if (node == null)
                    return false;

                if (bDispose)
                    node.Dispose();

                FInstances.Remove(name);
                EstablishDefault();

                return true;
            }
        }

        static public Boolean Remove(String name)
        {
            return Remove(name, true);
        }

        static public IReflectInsight Add(RIInstance instance) 
        {
            lock (FInstances)
            {
                // this method checks to see if the RI already exists
                // and only updates the category, bkColor and destination binding values.
                // if it doesn't exist then it creates a new RI and adds it to the list
                RILogManagerNode node = GetNode(instance.Name);
                if (node != null)
                {
                    // if the RI already exists, only change the category, color and destination binding groups
                    lock (node.Instance)
                    {
                        node.Instance.ClearDestinationBindingGroup();
                        node.Instance.Category = instance.Category;                        
                        node.Instance.BackColor = RIPastelBackColor.GetColorByName(instance.BkColor);
                        node.Instance.SetDestinationBindingGroup(instance.DestinationBindingGroup);
                    }

                    return node.Instance;
                }
                
                // create a new instance and add it
                return Add(instance.Name, CreateInstance(instance)); 
            }
        }

        static public IReflectInsight Add(String name, IReflectInsight ri) 
        {
            lock (FInstances)
            {
                RILogManagerNode node = GetNode(name);
                if (node != null)
                {
                    // if the RI already exists, only change the category, color, enabled state and destination binding groups
                    lock (node.Instance)
                    {
                        node.Instance.ClearDestinationBindingGroup();
                        node.Instance.Category = ri.Category;
                        node.Instance.BackColor = ri.BackColor;
                        node.Instance.Enabled = ri.Enabled;
                        node.Instance.DestinationBindingGroupId = ri.DestinationBindingGroupId;
                    }

                    ri.Dispose();
                    return node.Instance;
                }

                FInstances[name] = new RILogManagerNode(name, ri); 
                return ri;
            }
        }

        static public IReflectInsight Add(String name, String category, String bkColor, String destinationBindings)
        {
            return Add(new RIInstance(name, category, bkColor, destinationBindings));
        }

        static public IReflectInsight Add(String name, String category, String bkColor)
        {
            return Add(new RIInstance(name, category, bkColor)); 
        }

        static public IReflectInsight Add(String name, String category)
        {
            return Add(name, category, String.Empty);
        }
        static public IReflectInsight Add(String name)
        {
            return Add(name, name);
        }

        static public IReflectInsight Get(String name)
        {
            lock (FInstances)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return FDefault.Instance;
                
                RILogManagerNode node = (RILogManagerNode)FInstances[name];
                if (node != null)
                    return node.Instance;

                return Add(name, name);
            }
        }

        static public IReflectInsight Get(Type type)
        {
            return Get(type.FullName);
        }

        static public IReflectInsight GetCurrentClassLogger()
        {
            StackFrame frame = new StackFrame(1, false);
            MethodBase method = frame.GetMethod();

            return Get(method.DeclaringType);
        }

        static public void SetDefault(String name)
        {
            lock (FInstances)
            {
                RILogManagerNode node = (RILogManagerNode)FInstances[name];
                if (node == null)
                    return;

                FDefault = node;
            }
        }

        static public IReflectInsight Default
        {
            get { lock (FInstances) return FDefault.Instance; }
        }

        static public IReflectInsight[] Instances
        {
            get
            {
                lock (FInstances)
                {
                    List<IReflectInsight> list = new List<IReflectInsight>();
                    foreach (RILogManagerNode ln in FInstances.Values)
                        list.Add(ln.Instance);

                    return list.ToArray();
                }         
            }
        }

        static public RILogManagerNode[] Nodes
        {
            get
            {
                lock (FInstances)
                {
                    RILogManagerNode[] list = new RILogManagerNode[FInstances.Count];
                    FInstances.Values.CopyTo(list, 0);

                    return list;
                }
            }
        }
    }
}
