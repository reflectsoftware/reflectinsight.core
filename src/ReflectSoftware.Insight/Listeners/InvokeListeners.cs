// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common.Data;

namespace ReflectSoftware.Insight
{
    internal static class InvokeListeners
	{        
		internal static void Receive(DestinationInfo dObject, ReflectInsightPackage[] messages)
		{
			lock (dObject)
			{
				foreach (ListenerInfo listener in dObject.Listeners)
				{
					listener.Listener.Receive(messages);
				}
			}
		}
	}
}
