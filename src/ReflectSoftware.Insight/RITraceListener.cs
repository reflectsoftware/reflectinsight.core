// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using ReflectSoftware.Insight.Common;

namespace ReflectSoftware.Insight
{
	internal class RITraceListenerRequest: IRequestObject
    {
        public StringBuilder StrBuilder;
        public UInt32 RequestId { get; private set; }
        
        public void Attached(UInt32 requestId)
        {
            RequestId = requestId;
            StrBuilder = new StringBuilder();
        }
        
        public void Detached()
        {
        }
        
        public void Reset()
        {
        }
    }

    public class RITraceListenerData
    {
        public MessageType MessageType;     
        public String Message;        
        public Object Details;        
        public IDictionary<String, Object> ExtendedProperties;
    }

	public class RITraceListener: TraceListener
	{
        class ActiveStates
        {
            public IReflectInsight RI { get; set; }
        }

        static private Int32 ReferenceCount;
        static private readonly RequestObjectManager<RITraceListenerRequest> FRequestObjectManager;
        static private readonly MethodInfo FSendInternalErrorMethodInfo;
        
        private ActiveStates CurrentActiveStates;
        
        static readonly protected Object LockObject;
		static protected Boolean FEnabled;        
		static protected RITraceListener FListener;
                        
        protected String FName;        
		protected String FInstanceName;        
		protected Int32 FLastIndentLevel;

        public Boolean Disposed { get; private set; }

		static RITraceListener()
		{
            ReferenceCount = 0;
            LockObject = new Object();
            FRequestObjectManager = new RequestObjectManager<RITraceListenerRequest>(() => new RITraceListenerRequest());
            FSendInternalErrorMethodInfo = typeof(ReflectInsight).GetMethod("SendInternalError", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            FListener = null;
            FEnabled = false;
		}

        static internal void OnStartup()
        {
            lock (LockObject)
            {
                ReferenceCount++;
                if (ReferenceCount == 0)
                {                    
                }
            }
        }
        
        static internal void OnShutdown()
        {
            lock (LockObject)
            {
                ReferenceCount--;
                if (ReferenceCount <= 0)
                {
                    Enabled = false;
                    ReferenceCount = 0;
                }
            }
        }

		public RITraceListener(String instanceName)
		{
            FName = "RITraceListener";
            FInstanceName = instanceName ?? "RITraceListener";
			FLastIndentLevel = IndentLevel;
            CurrentActiveStates = new ActiveStates();
            Disposed = false;

			OnConfigChange();
			RIEventManager.OnServiceConfigChange += DoOnConfigChange;
		}

		public RITraceListener(): this("RITraceListener")
		{
		}

        protected override void Dispose(Boolean bDisposing)
        {
            lock (this)
            {                
                if (!Disposed)
                {
                    Disposed = true;
                    RIEventManager.OnServiceConfigChange -= DoOnConfigChange;
                }
            }

            base.Dispose(bDisposing);
        }
        
		private void DoOnConfigChange()
		{
			OnConfigChange();
		}

		private void OnConfigChange()
		{
			try
			{
				lock (this)
				{
                    ActiveStates states = new ActiveStates() { RI = RILogManager.Get(FInstanceName) ?? RILogManager.Default };
                    Name = states.RI.Category;
                    CurrentActiveStates = states;
				}
			}
			catch (Exception ex)
			{                
                RIExceptionManager.Publish(ex, String.Format("Failed during: RITraceListener.OnConfigChange() for extension: {0}", FInstanceName));
			}
		}
        
        static private Boolean SendInternalError(IReflectInsight ri, MessageType mType, Exception ex)
        {
            return (Boolean)FSendInternalErrorMethodInfo.Invoke(ri, new object[] { mType, ex });
        }
        
		private static void AppendMessage(String message)
		{
            FRequestObjectManager.GetRequestObject().StrBuilder.Append(message);
		}

		private static String GetFullWriteMessage(String writeMessage)
		{
            RITraceListenerRequest request = FRequestObjectManager.GetRequestObject(null);
            if(request != null)
            {
                writeMessage = request.StrBuilder.Append(writeMessage).ToString();
                FRequestObjectManager.RemoveRequest();
            }

            return writeMessage;
		}

		private void TrackIndent()
		{
            RITraceListenerRequest request = FRequestObjectManager.GetRequestObject(null);
            if (request != null)
                return;
            
			if (IndentLevel > FLastIndentLevel)
			{
				ReflectInsight.Indent();
				FLastIndentLevel++;
				TrackIndent();
			}
			else if (IndentLevel < FLastIndentLevel)
			{
				ReflectInsight.Unindent();
				FLastIndentLevel--;
				TrackIndent();
			}
		}
        
        public static void PrepareListenerData(RITraceListenerData tData, TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Information: tData.MessageType = MessageType.SendInformation; break;
                case TraceEventType.Warning: tData.MessageType = MessageType.SendWarning; break;
                case TraceEventType.Error: tData.MessageType = MessageType.SendError; break;
                case TraceEventType.Critical: tData.MessageType = MessageType.SendFatal; break;
                case TraceEventType.Start: tData.MessageType = MessageType.SendStart; break;
                case TraceEventType.Stop: tData.MessageType = MessageType.SendStop; break;
                case TraceEventType.Suspend: tData.MessageType = MessageType.SendSuspend; break;
                case TraceEventType.Resume: tData.MessageType = MessageType.SendResume; break;
                case TraceEventType.Transfer: tData.MessageType = MessageType.SendTransfer; break;
                case TraceEventType.Verbose: tData.MessageType = MessageType.SendVerbose; break;
                default:
                    // safety net catch in case a new or unknown TraceEventType was added in future releases of .NET
                    tData.MessageType = MessageType.SendMessage;
                    tData.Message = String.Format("[{0}]: {1}", eventType, tData.Message);
                    break;
            }
        }
        
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {            
            ActiveStates states = CurrentActiveStates;

            if (data is RITraceListenerData)
            {
                RITraceListenerData tData = data as RITraceListenerData;
                try
                {
                    // ignore complex message types as they are not
                    // allowed for simple message types
                    if (tData.MessageType >= RIUtils.ComplexMessageTypeStartRange)
                        return;

                    // amend extended properties to RI message if any
                    if (tData.ExtendedProperties != null && tData.ExtendedProperties.Count > 0)
                    {
                        StringBuilder sb = tData.Details == null ? new StringBuilder() : null;
                        if (sb != null)
                        {
                            sb.AppendLine("Extended Properties");
                            sb.AppendLine("-------------------");
                        }

                        foreach (String key in tData.ExtendedProperties.Keys)
                        {
                            String value = tData.ExtendedProperties[key] != null ? tData.ExtendedProperties[key].ToString() : String.Empty;
                            RIExtendedMessageProperty.AttachToSingleMessage("Extended Properties", key, value);

                            if (sb != null)
                            {
                                sb.AppendFormat("{0}: {1}{2}", key, value, Environment.NewLine);
                            }
                        }

                        if (sb != null)
                        {
                            tData.Details = sb.ToString();
                        }
                    }

                    if (tData.Message.StartsWith("[Enter]"))
                    {
                        states.RI.EnterMethod(tData.Message.Replace("[Enter]", String.Empty).Trim());
                        return;
                    }
                    else if (tData.Message.StartsWith("[Exit]"))
                    {
                        states.RI.ExitMethod(tData.Message.Replace("[Exit]", String.Empty).Trim());
                        return;
                    }

                    TrackIndent();
                    try
                    {
                        states.RI.Send(tData.MessageType, GetFullWriteMessage(tData.Message), tData.Details == null ? null : tData.Details.ToString());
                    }
                    finally
                    {
                        TrackIndent();
                    }
                }
                catch (Exception ex)
                {
                    if (!SendInternalError(states.RI, tData.MessageType, ex)) throw;
                }           
            }
            else
            {
                TraceEvent(eventCache, source, eventType, id, data.ToString());
            }
        }
        
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
            RITraceListenerData tData = new RITraceListenerData() { Message = message };            
            PrepareListenerData(tData, eventType);

            TraceData(null, null, TraceEventType.Information, 0, tData);
		}
        
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			TraceEvent(eventCache, source, eventType, id, String.Format(format, args ?? new Object[0]));
		}
        
        public override void Fail(String msg)
        {
            TraceData(null, null, TraceEventType.Information, 0, new RITraceListenerData() { MessageType = MessageType.SendFatal, Message = msg });
        }       

        public override void Fail(String msg, String category)
        {
            Fail(String.Format("{0}: {1}", msg, category ?? String.Empty));
        }

		public override void Write(String msg)
		{
			AppendMessage(msg);
		}
		
		public override void Write(Object obj)
		{
			Write(obj.ToString());
		}

		public override void Write(String msg, String category)
		{
			Write(String.Format("{0}: {1}", msg, category));
		}

		public override void Write(Object obj, String category)
		{
			Write(obj.ToString(), category);
		}
        
		public override void WriteLine(String msg)
		{
            RITraceListenerData tData = new RITraceListenerData() { MessageType = MessageType.SendMessage, Message = msg };

            TraceData(null, null, TraceEventType.Information, 0, tData);
		}

		public override void WriteLine(Object obj)
		{
			WriteLine(obj.ToString());
		}
        
		public override void WriteLine(String msg, String category)
		{
			WriteLine(String.Format("{0}: {1}", msg, category));
		}

		public override void WriteLine(Object obj, String category)
		{
			WriteLine(obj.ToString(), category);
		}
		
        public override String Name
		{
			get { return FName; }
			set { FName = value; }
		}
		
        public override bool IsThreadSafe
		{
			get { return true; }
		}
        
		static public void Clear()
		{
			lock (LockObject)
			{
                if (FListener != null && FListener.CurrentActiveStates.RI != null) 
					FListener.CurrentActiveStates.RI.Clear();
			}
		}

		static public Boolean Enabled
		{			
            get
			{
                return FEnabled;
			}
			set
			{
				lock (LockObject)
				{
					if (FEnabled == value)
						return;

					try
					{
						FEnabled = value;
						if (FEnabled)
						{
							FListener = new RITraceListener();
							Trace.Listeners.Add(FListener);
							return;
						}

						Trace.Listeners.Remove(FListener);
						FListener.Dispose();
						FListener = null;
					}
					catch (Exception ex)
					{
						RIExceptionManager.Publish(ex);
					}
				}
			}
		}        
	}
}
