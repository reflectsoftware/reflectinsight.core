// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Extensions;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReflectSoftware.Insight
{
    internal class BoundReflectInsightPackage
    {
        public Int32 BindingGroupId;
        public ReflectInsightPackage Package;
    }

	public class ListenerInfo : IListenerInfo
	{
        private Boolean Disposed { get; set; }
        internal IReflectInsightListener Listener { get; private set; }
        
        public Int32 Id { get; private set; }
        public String Name { get; private set; }
        public String Details { get; private set; }
        public SafeNameValueCollection Params { get; private set; }
        
        internal ListenerInfo(String name, String details, SafeNameValueCollection objParams, IReflectInsightListener listener)
        {
            Id = RIUtils.GetStringHash(name);
            Name = name;
            Details = details;
            Params = objParams;
            Listener = listener;
            Disposed = false;
        }
        
        internal void InternalDispose()
		{
			lock (this)
			{
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    Listener.DisposeObject();
                }
			}
		}
	}

	public class DestinationInfo: IDelayDisposable
	{
        private RIFilter FFilter;
        private Boolean Disposed;
        private readonly List<ReflectInsightPackage> InterimMessageQueue;
        private readonly List<ListenerInfo> FListeners;
        public Int32 Id { get; private set; }
        public String Name { get; private set; }
        public String Details { get; private set; }
		public Boolean Enabled { get; set; }
        public HashSet<Int32> BindingGroupIds { get; set; }

        internal DestinationInfo(String name, String details, Boolean bEnabled)
        {
            Id = RIUtils.GetStringHash(name);
            Name = name;
            Enabled = bEnabled;
            Disposed = false;
            BindingGroupIds = new HashSet<Int32>();
            FFilter = new RIFilter();
            InterimMessageQueue = new List<ReflectInsightPackage>();

            Details = details;
            FListeners = new List<ListenerInfo>();
            DetailParser.AddListenersByDetails(FListeners, details);
        }

        internal DestinationInfo(String name, String details, Boolean bEnabled, FilterInfo filterInfo): this(name, details, bEnabled)
        {
            Filter.SetFilter(filterInfo);
        }
        
        internal DestinationInfo(String name, String details, Boolean bEnabled, String filterName): this(name, details, bEnabled)
        {
            Filter.SetFilter(filterName);
        }
        
        internal DestinationInfo(String name, String details): this(name, details, true)
        {
        }
        
        internal DestinationInfo(String name): this(name, "Console", true)
        {
        }
        
        internal void AddInterimMessageQueue(ReflectInsightPackage package)
        {
            // this message queue doesn't have to be locked as its only
            // called from the Message Manager which is serialized 
            InterimMessageQueue.Add(package);
        }
        
        internal void ClearInterimMessageQueue()
        {
            // this message queue doesn't have to be locked as its only
            // called from the Message Manager which is serialized 
            InterimMessageQueue.Clear();
            InterimMessageQueue.Capacity = 0;
        }
        
        internal ReflectInsightPackage[] GetInterimMessages()
        {
            // this message queue doesn't have to be locked as its only
            // called from the Message Manager which is serialized 
            return InterimMessageQueue.ToArray();
        }
        
        internal void InternalDispose(Boolean bForced)
        {
            if (bForced)
            {
                (this as IDelayDisposable).DelayDispose();
            }
            else
            {
                DelayDisposeManager.Add(this, new TimeSpan(0, 0, 30));
            }
        }
        
        void IDelayDisposable.DelayDispose()        
		{
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    ClearListeners();
                }
            }
		}
        
		private void ClearListeners()
		{
            lock (this)
            {
                foreach (ListenerInfo lInfo in FListeners)
                    lInfo.InternalDispose();

                FListeners.Clear();
                FListeners.Capacity = 0;   
            }
		}
		
		public void SetFilter(String filterName )
		{
			lock(this) Filter.SetFilter(filterName);
		}
		
		public void SetFilter(FilterInfo fInfo)
		{
			lock (this) Filter.SetFilter(fInfo);
		}
		
		public ListenerInfo[] Listeners
		{
			get { lock(this) return FListeners.ToArray(); }
		}
          
        public RIFilter Filter 
        {
            get { lock(this) return FFilter; }
            set { lock(this) FFilter = value ?? new RIFilter(); }
        }
	}

    public class DestinationBindingGroup
    {
        private HashSet<DestinationInfo> Destinations { get; set; }
        private DestinationInfo[] FastDestinationArray { get; set; }

        public Int32 Id { get; private set; }
        public String Name { get; private set; }
        public ListenerGroup Parent { get; private set; }

        internal DestinationBindingGroup(String name, ListenerGroup parent)
        {
            Id = GetId(name);
            Name = name;
            Parent = parent;            
            Destinations = new HashSet<DestinationInfo>();
            ReconstructFastDestinationArray();
        }
        
        private void ReconstructFastDestinationArray()
        {
            // this technique is to avoid locking FastDestinationArray
            DestinationInfo[] TempFixedArray = new DestinationInfo[Destinations.Count];
            Destinations.CopyTo(TempFixedArray);
            FastDestinationArray = TempFixedArray;
        }
        
        static internal Int32 GetId(String name)
        {
            return RIUtils.GetStringHash(name);
        }
        
        public Boolean Contains(String destinationName)
        {
            lock (Parent)
            {
                return Destinations.Contains(Parent.GetDestination(destinationName));
            }
        }
        
        public Boolean Contains(DestinationInfo destination)
        {
            lock (Parent)
            {
                return Destinations.Contains(destination);
            }
        }
        
        public void AddDestinationBinding(String destinationName)
        {
            lock (Parent)
            {
                if (Parent.ContainsDestination(destinationName))
                {
                    DestinationInfo dInfo = Parent.GetDestination(destinationName);
                    dInfo.BindingGroupIds.Add(Id);

                    Destinations.Add(dInfo);
                    ReconstructFastDestinationArray();
                }
            }
        }
        
        public void RemoveDestinationBinding(String destinationName)
        {
            lock (Parent)
            {
                DestinationInfo dInfo = Parent.GetDestination(destinationName);
                dInfo.BindingGroupIds.Remove(Id);

                Destinations.Remove(dInfo);
                ReconstructFastDestinationArray();
            }
        }
        
        public void ClearDestinationBindings()
        {
            lock (Parent)
            {
                foreach(DestinationInfo dInfo in Destinations)
                {
                    dInfo.BindingGroupIds.Remove(Id);
                }

                Destinations.Clear();
                ReconstructFastDestinationArray();
            }
        }
          
        public Boolean IsEmpty
        {
            get { return FastDestinationArray.Length == 0; }            
        }
        
        public DestinationInfo[] BoundDestinations
        {
            get { return FastDestinationArray; }
        }
    }
    
    /// <summary>
    /// ListenerGroup
    /// </summary>
	public class ListenerGroup : IDelayDisposable
	{
        private Boolean Disposed;
        private DestinationInfo[] FFastDestinationArray;
        private readonly Dictionary<Int32, DestinationInfo> FDestinations;
        private readonly Dictionary<Int32, DestinationBindingGroup> FDestinationBindingGroups;
        
        public Int32 Id { get; internal set; }
        public String Name { get; internal set; }
		public Boolean Enabled { get; set; }
		public Boolean MaskIdentities { get; set; }
        public Boolean FromConfig { get; internal set; }


        internal ListenerGroup(String name, Boolean bEnabled, Boolean bMaskIdentities)
        {
            Id = RIUtils.GetStringHash(name);
            Name = name;
            Enabled = bEnabled;
            MaskIdentities = bMaskIdentities;
            FromConfig = false;
            Disposed = false;
            
            FDestinations = new Dictionary<Int32, DestinationInfo>();
            FDestinationBindingGroups = new Dictionary<Int32, DestinationBindingGroup>();
            ReconstructFastDestinationArray();
        }

        internal ListenerGroup(String name, Boolean bEnabled) : this(name, bEnabled, false)
        {
        }
        
        internal ListenerGroup(String name) : this(name, true)
        {
        }
        
        internal void InternalDispose(Boolean bForce)
        {
            if (bForce)
            {
                (this as IDelayDisposable).DelayDispose();
            }
            else
            {
                DelayDisposeManager.Add(this, new TimeSpan(0, 0, 30));
            }
        }
        
        void IDelayDisposable.DelayDispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    ClearDestinations(true);
                }
            }
        }
        
        protected void ReconstructFastDestinationArray()
        {
            lock (this)
            {
                FFastDestinationArray = FDestinations.Values.ToArray();               
            }
        }
        
		protected void ClearDestinations(Boolean bForce)
		{
			lock (this)
			{
                foreach (DestinationInfo dInfo in Destinations)
                    dInfo.InternalDispose(bForce);

                FDestinations.Clear();
                ClearDestinationBindingGroups();                
                ReconstructFastDestinationArray();
			}
		}
        
        protected void RemoveDestinationFromGroupBindings(DestinationInfo dInfo)
        {
            lock (this)
            {
                foreach (DestinationBindingGroup dGroup in FDestinationBindingGroups.Values)
                {
                    dGroup.RemoveDestinationBinding(dInfo.Name);
                }
            }
        }
        
        public Boolean IsActive
        {
            get { return Object.ReferenceEquals(this, RIListenerGroupManager.ActiveGroup); }
        }
        
        public Boolean IsDefault
        {
            get { return Object.ReferenceEquals(this, RIListenerGroupManager.DefaultGroup); }
        }
        
        public Boolean ContainsDestination(Int32 id)
        {
            lock (this) return FDestinations.ContainsKey(id);
        }
        
        public Boolean ContainsDestination(String name)
        {
            return ContainsDestination(RIUtils.GetStringHash(name));
        }
		
		public void RemoveDestination(DestinationInfo dInfo)
		{
			lock (this) 
			{
                if (FDestinations.Remove(dInfo.Id))
                {
                    RemoveDestinationFromGroupBindings(dInfo);                    
                    ReconstructFastDestinationArray();
                    dInfo.InternalDispose(false);
                }
			}
		}
		
		public void RemoveDestination(String name)
		{
            lock (this)
            {
                Int32 id = RIUtils.GetStringHash(name);
                if (FDestinations.ContainsKey(id))
                {
                    RemoveDestination(FDestinations[id]);
                }
            }
		}
        
        public DestinationInfo AddDestination(String name, String details, Boolean bEnabled)
        {            
            lock (this)
            {
                Int32 id = RIUtils.GetStringHash(name);                
                DestinationInfo dInfo = GetDestination(id);
                if (dInfo != null)
                    return dInfo;

                dInfo = new DestinationInfo(name, details, bEnabled);

                FDestinations[dInfo.Id] = dInfo;
                ReconstructFastDestinationArray();

                return dInfo;
            }
        }
		
        public DestinationInfo AddDestination(String name, String details, Boolean bEnabled, FilterInfo filterInfo)
		{
			DestinationInfo dInfo = AddDestination(name, details, bEnabled);
            dInfo.SetFilter(filterInfo);

            return dInfo;
		}
		
        public DestinationInfo AddDestination(String name, String details, Boolean bEnabled, String filterName)
		{
            DestinationInfo dInfo = AddDestination(name, details, bEnabled);
            dInfo.SetFilter(filterName);

            return dInfo;
		}
		
        public DestinationInfo AddDestination(String name, String details)
		{
            return AddDestination(name, details, true);
		}
		
        public DestinationInfo AddDestination(String name)
		{
            return AddDestination(name, "Console", true);
		}
        
        public DestinationInfo GetDestination(Int32 id)
        {
            lock (this)
            {                
                if (FDestinations.ContainsKey(id))
                    return FDestinations[id];

                return null;
            }
        }
        
		public DestinationInfo GetDestination(String name)
		{
			lock (this)
			{
                return GetDestination(RIUtils.GetStringHash(name));
			}
		}
        
        public DestinationInfo[] Destinations
        {
            get { lock(this) return FFastDestinationArray; }
        }
        
        public DestinationBindingGroup GetDestinationBindingGroup(Int32 bindingGroupId)
        {
            lock (this)
            {
                if (FDestinationBindingGroups.ContainsKey(bindingGroupId))
                {
                    return FDestinationBindingGroups[bindingGroupId];
                }

                return null;
            }
        }
        
        public DestinationBindingGroup GetDestinationBindingGroup(String bindingGroupName)
        {
            return GetDestinationBindingGroup(DestinationBindingGroup.GetId(bindingGroupName));
        }
        
        public DestinationBindingGroup AddDestinationBindingGroup(String bindingGroupName)
        {
            lock (this)
            {
                DestinationBindingGroup dGroup = GetDestinationBindingGroup(bindingGroupName);
                if (dGroup == null)
                {
                    dGroup = new DestinationBindingGroup(bindingGroupName, this);
                    FDestinationBindingGroups.Add(dGroup.Id, dGroup);
                }

                return dGroup;
            }
        }
        
        public void RemoveDestinationBindingGroup(DestinationBindingGroup bindingGroup)
        {
            lock (this)
            {
                if (bindingGroup.Parent.Id == Id)
                {
                    FDestinationBindingGroups.Remove(bindingGroup.Id);                    
                }
            }
        }
        
        public void RemoveDestinationBindingGroup(String bindingGroupName)
        {
            lock (this)
            {
                DestinationBindingGroup dGroup = GetDestinationBindingGroup(bindingGroupName);
                if (dGroup != null)
                {
                    RemoveDestinationBindingGroup(dGroup);
                }
            }
        }
        
        public void ClearDestinationBindingGroups()
        {
            lock (this)
            {
                FDestinationBindingGroups.Clear();                
            }
        }
        
        public DestinationBindingGroup[] DestinationBindingGroups
        {
            get 
            {
                lock (this)
                {
                    return FDestinationBindingGroups.Values.ToArray();
                }
            }
        }
	}
}
