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
