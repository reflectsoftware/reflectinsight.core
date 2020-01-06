// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections.Generic;

namespace ReflectSoftware.Insight
{
    public enum RIObjectType
	{        
		Instance,
		Global
	}


	[Serializable]
    public class ReflectInsightDispatcher : IReflectInsightDispatcher
	{        
        public String DestinationBindingGroupName { get; set; }
        public Int32 DestinationBindingGroupId { get; set; }
        public Boolean Disposed { get; private set; }
        public Boolean Enabled { get; set; }

        static ReflectInsightDispatcher()
		{
			ReflectInsightService.Initialize();
		}

		public ReflectInsightDispatcher()
		{
            Disposed = false;
            Enabled = true;
            ClearDestinationBindingGroup();
            RIEventManager.OnServiceConfigChange += OnConfigChange;
		}

		~ReflectInsightDispatcher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }
		
        #region Protected
        protected virtual void Dispose(Boolean bDisposing)
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);
                    RIEventManager.OnServiceConfigChange -= OnConfigChange;
                }
            }
        }		

		private void OnConfigChange()
		{
			GetConfigSettings();
		}


        protected virtual void GetConfigSettings()
		{
			Enabled = IsStateEnabledForType(RIObjectType.Instance);
		}
        
		public virtual void Dispatch(ReflectInsightPackage userPackage, ListenerGroup lgroup)
		{
			if (!Enabled || !lgroup.Enabled) return;

            MessageQueue.SendMessage(new BoundReflectInsightPackage() { BindingGroupId = DestinationBindingGroupId, Package = userPackage });
		}

        public void SetDestinationBindingGroup(String destinationBindingGroup)
        {
            lock (this)
            {
                DestinationBindingGroupName = destinationBindingGroup;
                if(!string.IsNullOrWhiteSpace(destinationBindingGroup))
                    DestinationBindingGroupId = DestinationBindingGroup.GetId(destinationBindingGroup);                
            }
        }

        public String GetDestinationBindingGroup()
        {
            lock (this)
            {
                return DestinationBindingGroupName;
            }
        }

        public void ClearDestinationBindingGroup()
        {
            lock (this)
            {
                DestinationBindingGroupId = 0;
                DestinationBindingGroupName = String.Empty;
            }
        }        
		#endregion		
		
		#region Public

		public static void Initialize()
		{
			ReflectInsightService.Initialize();
		}

        public static void Dispatch(IEnumerable<ReflectInsightPackage> packages, Int32 destinationBindingGroupId)
		{            
			try
			{
                List<BoundReflectInsightPackage> boundPackages = new List<BoundReflectInsightPackage>();
                foreach (ReflectInsightPackage package in packages)
                    boundPackages.Add(new BoundReflectInsightPackage() { BindingGroupId = destinationBindingGroupId, Package = package });

                MessageQueue.SendMessages(boundPackages);
			}
			catch (Exception ex)
			{
				RIExceptionManager.PublishIfEvented(ex, "Failed during: ReflectInsightDispatcher.Dispatch()");
			}
		}

        public static void Dispatch(IEnumerable<ReflectInsightPackage> packages)
        {
            Dispatch(packages, 0);
        }

        public static void Dispatch(ReflectInsightPackage package, Int32 destinationBindingGroupId)
		{
			try
			{
                MessageQueue.SendMessage(new BoundReflectInsightPackage() { BindingGroupId = destinationBindingGroupId, Package = package });
			}
			catch (Exception ex)
			{
				RIExceptionManager.PublishIfEvented(ex, "Failed during: ReflectInsightDispatcher.Dispatch()");
			}
		}

        public static void Dispatch(ReflectInsightPackage package)
        {
            Dispatch(package, 0);
        }
		#endregion

		#region Public Properties

        ///--------------------------------------------------------------------
        internal static Boolean IsStateEnabledForType(RIObjectType riType)
		{
			Boolean bEnabled = true;
			String state = ReflectInsightConfig.Settings.GetBaseEnableAttribute("state", "all").ToLower();
			switch (riType)
			{
				case RIObjectType.Instance: bEnabled = (state == "instanceonly" || state == "all"); break;
				case RIObjectType.Global: bEnabled   = (state == "globalonly"   || state == "all"); break;
				default: 
					bEnabled = false; 
					break;
			}

			return bEnabled;
		}

		#endregion
	}
}
