using Newtonsoft.Json;
using Plato.Extensions;
using Plato.Security.Xml;
using Plato.Serializers.Interfaces;
using Plato.Strings;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace ReflectSoftware.Insight
{
    [Serializable]
    public class ReflectInsight : ReflectInsightDispatcher, IReflectInsight
	{        
        private readonly static Object FLockObject;      
        private readonly static Int16 FSourceUtcOffset;
        static private String FDomainName;      

        static public event ReflectInsightMessageInterceptHandler OnGlobalReflectInsightMessageIntercept;
        
        static protected String FUserDomainName;
        static protected String FUserName;        
        static protected String FMachineName;        
        static protected String FMaskedUserDomainName;        
        static protected String FMaskedUserName;        
        static protected String FMaskedMachineName;

        public event ReflectInsightMessageInterceptHandler OnReflectInsightMessageIntercept;

        protected String FCategory;
        protected Color FBkColor;
        public Checkmark DefaultCheckmark { get; set; }
        public static Boolean PropagateException { get; set; }
                					
		#region Constructors
		
		static ReflectInsight()
		{
			FLockObject = new Object();			
            PropagateException = false;			
			FSourceUtcOffset = (Int16)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now.ToUniversalTime()).TotalMinutes;

			FUserName = Environment.UserName;
			FUserDomainName = Environment.UserDomainName;
			FMachineName = Environment.MachineName;
			FMaskedUserName = RIUtils.HashString(FUserName);
            FMaskedUserDomainName = RIUtils.HashString(FUserDomainName);
            FMaskedMachineName = RIUtils.HashString(FMachineName);

			ReflectInsightService.Initialize();            
		}

        ///--------------------------------------------------------------------
		private void Init(String category)
		{            
			Category = category;			
			DefaultCheckmark = Checkmark.Red;
            BackColor = Color.White;

			GetConfigSettings();
			RIEventManager.DoOnCreatedInstance(this);
		}

        public ReflectInsight(String category)
		{
			Init(category);            
		}

		public ReflectInsight(): this("ReflectInsight") 
		{
		}
		#endregion

		#region Public static Methods
                
        static internal void OnConfigFileChange()
        {
            try
            {
                lock (FLockObject)
                {
                    PropagateException = ReflectInsightConfig.Settings.GetBasePropagateExceptionAttribute("enabled", "false").ToLower() == "true";                    
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static ReflectInsight.OnConfigFileChange()");
            }
        }
        
		static internal void OnStartup()
		{
			OnConfigFileChange();
		}
        
        static internal void OnShutdown()
        {
        }

		protected static MessageType GetMessageTypeForSendObject(Object obj)
		{
            if (obj == null)
                return MessageType.Unknown;

			if (obj is Enum) return MessageType.SendEnum;
			if (obj is Boolean) return MessageType.SendBoolean;
			if (obj is Byte) return MessageType.SendByte;
			if (obj is SByte) return MessageType.SendByte;
			if (obj is Char) return MessageType.SendChar;
			if (obj is Decimal) return MessageType.SendDecimal;
			if (obj is Double) return MessageType.SendDouble;
			if (obj is Single) return MessageType.SendSingle;
			if (obj is Int32) return MessageType.SendInteger;
			if (obj is UInt32) return MessageType.SendInteger;
			if (obj is Int64) return MessageType.SendInteger;
			if (obj is UInt64) return MessageType.SendInteger;
			if (obj is Int16) return MessageType.SendInteger;
			if (obj is UInt16) return MessageType.SendInteger;
			if (obj is String) return MessageType.SendString;
			if (obj is StringBuilder) return MessageType.SendString;
			if (obj is DateTime) return MessageType.SendDateTime;
            
			return MessageType.SendObject;
		}

		#endregion

		#region Protected        
		static private ControlValues GetControlValues()
		{            
            var rValue = RequestManager.GetRequestObject();

            if (rValue.ShouldClear)
            {
                rValue.Clear();
            }

			return rValue;
		}

		protected override void GetConfigSettings()
		{
			try
			{
				lock (this)
				{
					base.GetConfigSettings();
					FDomainName = ReflectInsightConfig.Settings.GetSenderName();
				}
			}
			catch (Exception ex)
			{
				RIExceptionManager.Publish(ex, "Failed during: ReflectInsight.GetConfigSettings()");
			}
		}

		private void Send(ControlValues controlValues)
		{
            // No need to construct the ListenerGroup if we're not enabled.
            if (!Enabled)
            {
                return;
            }

			var lgroup = RIListenerGroupManager.ActiveGroup;
            if (lgroup == null || !lgroup.Enabled)
                return;

            if (controlValues.SendPack.FMessageType == MessageType.ExitMethod)
            {
                if (controlValues.IndentValue.Level == 0)
                {
                    // Don't dispatch this message.
                    // There are no valid matching EnterMethods.
                    return;
                }

                controlValues.IndentValue.Decrement();
            }

            ReflectInsightPackage package = new ReflectInsightPackage();

            package.FSourceUtcOffset = FSourceUtcOffset;
            package.FDateTime = DateTime.Now.ToUniversalTime();
            package.FProcessID = ReflectInsightService.ProcessId;
            package.FSessionID = ReflectInsightService.SessionId;
            package.FRequestID = controlValues.RequestId;
            package.FThreadID = (Int32)controlValues.ThreadId;
            package.FApplication = FDomainName;
            package.FDomainID = Thread.GetDomainID();
            package.FCategory = Category;
            package.FMessage = controlValues.SendPack.FMessage;
            package.FMessageSubType = controlValues.SendPack.FMessageSubType;
            package.FMessageType = controlValues.SendPack.FMessageType;
            package.FBkColor = RIMessageColors.GetBackColor(package.FMessageType, FBkColor);
            package.FIndentLevel = controlValues.IndentValue.Level;

            package.SetSubDetails(controlValues.SendPack.FSubDetails);
            package.SetDetails(controlValues.SendPack.FDetails);

            if (!lgroup.MaskIdentities)
            {
                package.FUserName = FUserName;
                package.FUserDomainName = FUserDomainName;
                package.FMachineName = FMachineName;
            }
            else
            {
                package.FUserName = FMaskedUserName;
                package.FUserDomainName = FMaskedUserDomainName;
                package.FMachineName = FMaskedMachineName;
            }

            RIExtendedMessageProperty.AssignToPackage(controlValues, package);

            if (DoOnMessageIntercept(OnGlobalReflectInsightMessageIntercept, this, package))
            {                
                if (DoOnMessageIntercept(OnReflectInsightMessageIntercept, this, package))
                    Dispatch(package, lgroup);
            }

            if (controlValues.SendPack.FMessageType == MessageType.EnterMethod)
            {
                controlValues.IndentValue.Increment();
            }
            else if (controlValues.SendPack.FMessageType == MessageType.Clear)
            {
                controlValues.Clear();
            }
		}
        
        private static Boolean DoOnMessageIntercept(ReflectInsightMessageInterceptHandler messageIntercept, ReflectInsight ri, ReflectInsightPackage package)
        {            
            if (messageIntercept == null)
                return true;

            Boolean bDispatch = true;
            foreach (ReflectInsightMessageInterceptHandler func in messageIntercept.GetInvocationList())
            {
                try
                {
                    if (!func(ri, package))
                        bDispatch = false;
                }
                catch (Exception ex)
                {
                    RIExceptionManager.PublishIfEvented(ex);
                    RIEventManager.DoOnSendInternalException(ex);			
                }
            }

            return bDispatch;
        }
        
		private Boolean SendInternalError(MessageType mType, Exception ex)
		{
			Boolean bDontPropagate = false;
			if( !PropagateException )
			{
				try
				{
					ControlValues controlValues = GetControlValues();
					using (new ControlValuesContainer(controlValues))
					{
						controlValues.SendPack.Set(MessageType.SendInternalError, String.Format("Internal Exception:<{0}>[{1}->{2}]", mType, ex.GetType().Name, ex.Message.Replace(Environment.NewLine, " ")));
						controlValues.SendPack.FDetails = new DetailContainerString(ExceptionFormatter.ConstructIndentedMessage(ex));
						Send(controlValues);

						bDontPropagate = true;
					}
				}
				catch
				{                    
					bDontPropagate = false;
				}
			}

            RIExceptionManager.PublishIfEvented(ex);
			RIEventManager.DoOnSendInternalException(ex);			

			// Propagate exception to calling method
			return bDontPropagate;
		}
        
        private void SendXML(MessageType mType, String str, XmlNode node, params object[] args)
        {
            if (!Enabled) return;

            if (str == null) throw new ArgumentNullException("str");
            if (node == null) throw new ArgumentNullException("node");
            if (node is XmlEntity || node is XmlNotation) throw new ReflectInsightException(String.Format("Reflecting on following XmlNode type: '{0}' is not supported by ReflectInsight.", node.GetType().FullName));

            String xmlString = null;
            if (node is XmlAttribute)
            {
                xmlString = String.Format("<{0} />", node.OuterXml);
            }
            else
            {
                xmlString = RIUtils.FormatXml(node.OuterXml, true);
            }

            Send(mType, SendPack.ConstructMessage(str, args), xmlString);
        }
        
        private void SendStream(MessageType mType, Byte[] stream, String str, params Object[] args)
		{ 
			if(!Enabled) return;
            if (str == null) throw new ArgumentNullException("str");
            if (stream == null) throw new ArgumentNullException("stream");

            ControlValues controlValues = GetControlValues();
            using (new ControlValuesContainer(controlValues))
            {
                controlValues.SendPack.Set(mType, str, args);
                controlValues.SendPack.FDetails = new DetailContainerByteArray(stream);
                Send(controlValues);
            }
		}

		protected void SendStream(MessageType mType, Stream stream, String str, params Object[] args)
		{ 
			if( !Enabled ) return;
		
            if (stream == null) throw new ArgumentNullException("stream");
            if (str == null) throw new ArgumentNullException("str");

			Byte[] bStream = new Byte[ stream.Length ];
			stream.Seek( 0, SeekOrigin.Begin );
			stream.Read( bStream, 0, bStream.Length );
	
			SendStream( mType, bStream, str, args);
		}

		protected void SendTextStream(MessageType mType, Stream stream, String str, params Object[] args)
		{
			if (!Enabled) return;

            if (str == null) throw new ArgumentNullException("str");
            if (stream == null) throw new ArgumentNullException("stream");

            using (TextReader tr = new StreamReader(stream, Encoding.UTF8))
            {
                Send(mType, SendPack.ConstructMessage(str, args), tr.ReadToEnd());
            }
		}
                
		protected void SendTextFile(MessageType mType, String fileName, String str, params Object[] args)
		{
			if( !Enabled ) return;
            if (str == null) throw new ArgumentNullException("str");
            if (fileName == null) throw new ArgumentNullException("fileName");

            try
            {
                String data = File.ReadAllText(RIUtils.DetermineParameterPath(fileName), Encoding.UTF8);
                Send(mType, SendPack.ConstructMessage(str, args), data);
            }
            catch (Exception ex)
            {
                throw new ReflectInsightException(String.Format("File: '{0}', caused the following error: {1}", fileName, ex.Message), ex);
            }
		}

		protected void SendBinaryFile(MessageType mType, String fileName, String str, params Object[] args)
		{
			if (!Enabled) return;
            if (str == null) throw new ArgumentNullException("str");
            if (fileName == null) throw new ArgumentNullException("fileName");

            try
            {
                using (FileStream fs = new FileStream(RIUtils.DetermineParameterPath(fileName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    SendStream(mType, fs, str, args);
                }
            }
            catch (Exception ex)
            {
                throw new ReflectInsightException(String.Format("File: '{0}', caused the following error: {1}", fileName, ex.Message), ex);
            }
		}

		public void SendCustomData(String str, RICustomData cData, params object[] args)
		{
            try
            {
                _SendCustomData(MessageType.SendCustomData, str, cData, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendCustomData, ex)) throw;
            }						
		}

		protected void SendObject(String str, Object obj, ObjectScope scope, Boolean bIgnoreStandard, params Object[] args)
		{											
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					if( obj == null )
					{
                        controlValues.SendPack.Set(MessageType.SendObject, SendPack.ConstructMessage(String.Format("{0} = (null)", str), args));
						Send(controlValues);
						return;
					}
					else if (bIgnoreStandard || !obj.IsStandardType())
					{
						controlValues.SendPack.Set(MessageType.SendObject, "{0} = ({1})", SendPack.ConstructMessage(str, args), obj.GetType().Name);
						controlValues.SendPack.FDetails = ObjectBuilder.BuildObjectPropertyMap(obj, scope);
						Send(controlValues);
						return;
					}
			
					MessageType mType = GetMessageTypeForSendObject(obj);
					if( mType != MessageType.SendDateTime )
					{
						controlValues.SendPack.Set(mType, "{0} = {1} : {2}", SendPack.ConstructMessage(str, args), obj, obj.GetType().FullName);
						Send(controlValues);
					}
					else
					{
						SendDateTime(str, (DateTime )obj, args);
					}
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError(MessageType.SendObject, ex ) ) throw;
			}						
		}

		protected void AddCheckpoint(String lable, Int32 checkPointValue, Checkpoint checkPointType)
		{
			if(!Enabled) return;			

			try
			{
				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    controlValues.SendPack.Set(MessageType.AddCheckpoint, lable, checkPointValue);
					controlValues.SendPack.FMessageSubType = (Byte)checkPointType;                    
					Send(controlValues);
				}
			}
			catch( Exception ex )
			{
				if( !SendInternalError( MessageType.AddCheckpoint, ex ) ) throw;
			}
		}
		#endregion
		
		#region Public Methods
		
		public void Clear()
		{
            if (!Enabled) return;

			try
			{
				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.Clear, null);
					Send(controlValues);
				}
			}
			catch( Exception ex )
			{
				if( !SendInternalError( MessageType.Clear, ex ) ) throw;
			}
		}

		public void ViewerClearAll()
		{
			if (!Enabled) return;

			try
			{
				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.ViewerClearAll, null);
					Send(controlValues);
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.ViewerClearAll, ex)) throw;
			}
		}

		public void ViewerClearWatches()
		{
            if (!Enabled) return;

			try
			{
                ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.ViewerClearWatches, null);
					Send(controlValues);
				}
			}
			catch( Exception ex )
			{
				if (!SendInternalError(MessageType.ViewerClearWatches, ex)) throw;
			}
		}

        protected void Send(MessageType mType, String str, String details, byte subType, params object[] args)
		{
			if (!Enabled) return;
            if (str == null) throw new ArgumentNullException("str");

            if (mType >= RIUtils.ComplexMessageTypeStartRange)
            {
                str = String.Format("{0} ->{1}: is not a simple message type and is not permissible via the ReflectInsight.Send() method", str, mType);
                mType = MessageType.SendWarning;
            }

			ControlValues controlValues = GetControlValues();
			using (new ControlValuesContainer(controlValues))
			{
				controlValues.SendPack.Set(mType, str, args);
				controlValues.SendPack.FMessageSubType = subType;
				controlValues.SendPack.FDetails = details != null ? new DetailContainerString(details) : null;
				Send(controlValues);
			}
		}

		public void Send(MessageType mType, String str, String details, params object[] args)
		{
			if (!Enabled) return;
            if (str == null) throw new ArgumentNullException("str");

            if (mType == MessageType.AddCheckpoint)
            {
                Send(mType, str, details, (Byte)DefaultCheckpoint, args);
            }
            else if (mType == MessageType.SendCheckmark)
            {
                Send(mType, str, details, (Byte)DefaultCheckmark, args);
            }
            else if (mType == MessageType.SendLevel)
            {
                Send(mType, str, details, (Byte)LevelType.Green, args);
            }
            else
            {
                Send(mType, str, details, (Byte)0, args);
            }
		}

        public void Send(MessageType mType, String str, params object[] args)
        {
            try
            {
                Send(mType, str, null, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(mType, ex)) throw;
            }
        }

        protected void _SendCustomData(MessageType mType, String str, RICustomData cData, params object[] args)
		{
			if (!Enabled) return;

            if (str == null) throw new ArgumentNullException("str");
            if (cData == null) throw new ArgumentNullException("cData");

			ControlValues controlValues = GetControlValues();
			using (new ControlValuesContainer(controlValues))
			{
				controlValues.SendPack.Set(mType, str, args);
				controlValues.SendPack.FDetails = cData;
				Send(controlValues);
			}
		}

        public Checkpoint DefaultCheckpoint
        {
            set
            {
                ControlValues controlValues = GetControlValues();
                controlValues.DefaultCheckpoint = value;
            }
            get
            {
                ControlValues controlValues = GetControlValues();
                return controlValues.DefaultCheckpoint;
            }
        }

        public void ResetCheckpoint(Checkpoint cType)
        {
            ControlValues controlValues = GetControlValues();
            controlValues.ResetCheckpoint(cType);
        }

        public void ResetAllCheckpoints()
        {
            ControlValues controlValues = GetControlValues();
            controlValues.ResetAllCheckpoints();
        }

        public void ResetCheckpoint()
        {
            ControlValues controlValues = GetControlValues();
            controlValues.ResetCheckpoint(controlValues.DefaultCheckpoint);
        }

        public void ResetCheckpoint(String name, Checkpoint cType)
        {
            ControlValues controlValues = GetControlValues();
            controlValues.ResetCheckpoint(name, cType);
        }

        public void ResetCheckpoint(String name)
        {
            ControlValues controlValues = GetControlValues();
            controlValues.ResetCheckpoint(name, controlValues.DefaultCheckpoint);
        }

		public void AddCheckpoint(Checkpoint cType)
		{
			if (!Enabled) return;

			try
			{
                if (cType < Checkpoint.Red || cType > Checkpoint.Purple)
                    throw new ArgumentOutOfRangeException("cType", "Checkpoint type is not in the valid range for Checkpoint values");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					AddCheckpoint("Checkpoint: {0}", controlValues.GetNextCheckpoint(cType), cType);
				}				
			}
			catch(Exception ex)
			{
				if( !SendInternalError( MessageType.AddCheckpoint, ex ) ) throw;
			}
		}
        
		public void AddCheckpoint()
		{ 
			AddCheckpoint(DefaultCheckpoint);
		}

        public void AddCheckpoint(String name, Checkpoint cType)
        {
            if (!Enabled) return;

            try
            {
                if (cType < Checkpoint.Red || cType > Checkpoint.Purple)
                    throw new ArgumentOutOfRangeException("cType", "Checkpoint type is not in the valid range for Checkpoint values");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    AddCheckpoint(String.Format("{0}: {{0}}", name), controlValues.GetNextCheckpoint(name, cType), cType);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.AddCheckpoint, ex)) throw;
            }
        }

        public void AddCheckpoint(String name)
        {
            AddCheckpoint(name, DefaultCheckpoint);
        }

		public void AddSeparator()
		{
            if (!Enabled) return;

			try
			{
				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.AddSeparator, null);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.AddSeparator, ex ) ) throw;
			}
		}	
        
		public void EnterMethod(String str, params Object[] args)
		{
            if (!Enabled) return;

            try
			{
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.EnterMethod, str, args);
                    Send(controlValues);
                }
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.EnterMethod, ex ) ) throw;
			}
		}

		public void EnterMethod(MethodBase currentMethod, Boolean fullName = true)
		{
			if (!Enabled) return;
            if (currentMethod == null) throw new ArgumentNullException("currentMethod");

			if( fullName ) 
				EnterMethod( String.Format("{0}.{1}", currentMethod.ReflectedType.FullName, currentMethod.Name ) );
			else
				EnterMethod( currentMethod.Name );
		}

		public ITraceMethod TraceMethod(String str, params Object[] args)
		{
            SendPack sp = new SendPack();
            try
            {
                sp.Set(MessageType.EnterMethod, str, args);
                return new TraceMethod(this, sp.FMessage);
            }
            finally
            {
                sp.Release();
            }
		}

		public ITraceMethod TraceMethod(MethodBase currentMethod, Boolean fullName = false)
		{
            return TraceMethod(fullName ? String.Format("{0}.{1}()", currentMethod.ReflectedType.FullName, currentMethod.Name) : String.Format("{0}()", currentMethod.Name));
		}
        
		public void ExitMethod(String str, params Object[] args)
		{ 
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.ExitMethod, str, args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if(!SendInternalError(MessageType.ExitMethod, ex)) throw;
			}
		}
        
		public void ExitMethod(MethodBase currentMethod, Boolean fullName = true)
		{
			if (!Enabled) return;

            if (currentMethod == null) throw new ArgumentNullException("currentMethod");

			if( fullName ) 
				ExitMethod(String.Format("{0}.{1}", currentMethod.ReflectedType.FullName, currentMethod.Name));
			else
				ExitMethod(currentMethod.Name);
		}		
        
		public void SendMessage(String str, params Object[] args)
		{
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendMessage, str, args);
                    Send(controlValues);
                }
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendMessage, ex ) ) throw;				
			}
		}

		public void SendComment(String str, params Object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendComment, str, args);
					Send(controlValues);
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendComment, ex)) throw;
			}
		}
		
		public void SendNote( String str, params Object[] args )
		{ 
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendNote, str, args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendNote, ex ) ) throw;
			}
		}		

		public void SendInformation( String str, params Object[] args )
		{ 
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendInformation, str, args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendInformation, ex ) ) throw;
			}
		}		

		public void SendWarning( String str, params Object[] args )
		{ 			
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendWarning, str, args);						
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendWarning, ex ) ) throw;
			}
		}		

		public void SendError( String str, params Object[] args )
		{ 
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendError, str, args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendError, ex ) ) throw;
			}
		}		

		public void SendFatal(String str, params Object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
			 
				List<String> ignoreList = new List<String>();
				ignoreList.Add("Diagnostics.Trace.Fail");
				ignoreList.Add("ReflectInsight.SendFatal");
				ignoreList.Add("GReflectInsight.SendFatal");
				ignoreList.Add("GDebugReflectInsight.SendFatal");
				
				Send(MessageType.SendFatal, SendPack.ConstructMessage(str, args), SimpleAPIHelper.GetIdentedCallStack(ignoreList));
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendFatal, ex)) throw;
			}
		}		

        public void SendFatal(String str, Exception excep, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (excep == null) throw new ArgumentNullException("excep");
			                 
                List<String> ignoreList = new List<String>();
				ignoreList.Add("Diagnostics.Trace.Fail");
				ignoreList.Add("ReflectInsight.SendFatal");
				ignoreList.Add("GReflectInsight.SendFatal");
				ignoreList.Add("GDebugReflectInsight.SendFatal");

				StringBuilder sb = new StringBuilder();
				sb.AppendLine(ExceptionFormatter.ConstructIndentedMessage(excep));
				sb.AppendLine();
				sb.AppendLine(SimpleAPIHelper.GetIdentedCallStack(ignoreList));

				Send(MessageType.SendFatal, SendPack.ConstructMessage(str, args), sb.ToString());
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendFatal, ex)) throw;
			}
		}
        
        public void SendDebug(String str, params Object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendDebug, str, args);
					Send(controlValues);
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendDebug, ex)) throw;
			}
		}
        
        public void SendTrace(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendTrace, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTrace, ex)) throw;
            }
        }
        
        public void SendStart(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendStart, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendStart, ex)) throw;
            }
        }

        public void SendStop(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendStop, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendStop, ex)) throw;
            }
        }

        public void SendSuspend(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendSuspend, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendSuspend, ex)) throw;
            }
        }

        public void SendResume(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendResume, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendResume, ex)) throw;
            }
        }

        public void SendTransfer(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendTransfer, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTransfer, ex)) throw;
            }
        }

        public void SendVerbose(String str, params Object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendVerbose, str, args);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendVerbose, ex)) throw;
            }
        }

		public void SendAuditSuccess(String str, params Object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendAuditSuccess, str, args);
					Send(controlValues);
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendAuditSuccess, ex)) throw;
			}
		}

		public void SendAuditFailure(String str, params Object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendAuditFailure, str, args);
					Send(controlValues);
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendAuditFailure, ex)) throw;
			}
		}

        public void SendLevel(String str, LevelType level, params object[] args)
		{ 
			if (!Enabled) return;			

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    controlValues.SendPack.Set(MessageType.SendLevel, str, args);
					controlValues.SendPack.FMessageSubType = (Byte)level;
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendLevel, ex ) ) throw;
			}
		}	

		public void SendReminder( String str, params Object[] args )
		{ 
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendReminder, str, args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendReminder, ex ) ) throw;
			}
		}

        public void SendStream(String str, Byte[] stream, params object[] args)
		{
            try
            {
                SendStream(MessageType.SendStream, stream, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendStream, ex)) throw;
            }
		}

        public void SendStream(String str, Stream stream, params object[] args)
		{
            try
            {
                SendStream(MessageType.SendStream, stream, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendStream, ex)) throw;
            }
		}

        public void SendStream(String str, String fileName, params object[] args)
		{
            try
            {
                SendBinaryFile(MessageType.SendStream, fileName, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendStream, ex)) throw;
            }            
		}

        public void SendMemory(String str, Byte[] stream, params object[] args)
		{
            try
            {
                SendStream(MessageType.SendMemory, stream, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendMemory, ex)) throw;
            }
		}

        public void SendMemory(String str, Stream stream, params object[] args)
		{
            try
            {
                SendStream(MessageType.SendMemory, stream, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendMemory, ex)) throw;
            }
		}

        public void SendMemory(String str, String fileName, params object[] args)
		{
            try
            {
                SendBinaryFile(MessageType.SendMemory, fileName, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendMemory, ex)) throw;
            }
		}

		public void SendLoadedAssemblies(String str, params Object[] args )
		{
            try
            {
                _SendCustomData(MessageType.SendLoadedAssemblies, SendPack.ConstructMessage(str, args), SimpleAPIHelper.GetLoadedAssemblies());
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendLoadedAssemblies, ex)) throw;
            }						
		}
        
        public void SendLoadedAssemblies()
		{ 
			SendLoadedAssemblies("Assemblies Loaded");
		}
        
        public void SendLoadedProcesses(String str, params Object[] args)
        {
            try
            {
                _SendCustomData(MessageType.SendLoadedProcesses, SendPack.ConstructMessage(str, args), SimpleAPIHelper.GetLoadedProcesses());
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendLoadedProcesses, ex)) throw;
            }
        }

        public void SendLoadedProcesses()
        {
            SendLoadedProcesses("Processes Loaded");
        }

        public void SendCollection(String str, IEnumerable enumerator, ObjectScope scope, params object[] args)
		{ 
			if( !Enabled ) return;			

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (enumerator == null) throw new ArgumentNullException("enumerator");                

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    controlValues.SendPack.Set(MessageType.SendCollection, str, args);
					controlValues.SendPack.FDetails = SimpleAPIHelper.GetCollection(enumerator, scope);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendCollection, ex ) ) throw;
			}
		}

        public void SendCollection(String str, IEnumerable enumerator, params object[] args)
		{
            SendCollection(str, enumerator, ObjectScope.None, args);
		}

        public void SendTextFile(String str, String fileName, params object[] args)
		{
            try
            {
                SendTextFile(MessageType.SendTextFile, fileName, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTextFile, ex)) throw;
            }            
		}

        public void SendTextFile(String str, Stream stream, params object[] args)
		{
            if (!Enabled) return;

            try
            {
                if (stream == null) throw new ArgumentNullException("stream");
                if (str == null) throw new ArgumentNullException("str");

                using (TextReader tr = new StreamReader(stream, true))
                {
                    Send(MessageType.SendTextFile, SendPack.ConstructMessage(str, args), tr.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTextFile, ex)) throw;
            }            
		}

        public void SendTextFile(String str, TextReader reader, params object[] args)
		{
            try
            {
                if (reader == null) throw new ArgumentNullException("reader");
                if (str == null) throw new ArgumentNullException("str");

                Send(MessageType.SendTextFile, SendPack.ConstructMessage(str, args), reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTextFile, ex)) throw;
            }
		}

        public void SendXML(String str, XmlNode node, params object[] args)        
		{
			try
			{
                SendXML(MessageType.SendXML, str, node, args);
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendXML, ex)) throw;
			}
		}
        
        public void SendXMLFile(String str, String fileName, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (fileName == null) throw new ArgumentNullException("fileName");

                try
                {                    
                    using (TextReader tr = new StreamReader(RIUtils.DetermineParameterPath(fileName), Encoding.UTF8))
                    {
                        XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                        doc.Load(tr);

                        SendXML(MessageType.SendXML, str, doc, args);
                    }
                }
                catch (Exception ex)
                {
                    throw new ReflectInsightException(String.Format("File: '{0}', caused the following error: {1}", fileName, ex.Message), ex);
                }
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendXML, ex)) throw;
			}
		}

        public void SendXML(String str, Stream stream, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (stream == null) throw new ArgumentNullException("stream");

                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
				doc.Load(stream);

                SendXML(MessageType.SendXML, str, doc, args);
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendXML, ex)) throw;
			}
		}

        public void SendXML(String str, TextReader reader, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (reader == null) throw new ArgumentNullException("reader");

                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.Load(reader);

                SendXML(MessageType.SendXML, str, doc, args);
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendXML, ex)) throw;
			}
		}

        public void SendXML(String str, XmlReader reader, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (reader == null) throw new ArgumentNullException("reader");

                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
				reader.MoveToContent();
				doc.Load(reader);

                SendXML(MessageType.SendXML, str, doc, args);
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendXML, ex)) throw;
			}
		}
        
        public void SendXMLString(String str, String xmlString, params object[] args)
		{
			if (!Enabled) return;

			try
			{
				if (str == null) throw new ArgumentNullException("str");
                if (xmlString == null) throw new ArgumentNullException("xmlString");

                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(xmlString);

                SendXML(MessageType.SendXML, str, doc, args);
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendXML, ex)) throw;
			}
		}
        
        public void SendHTMLFile(String str, String fileName, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (fileName == null) throw new ArgumentNullException("fileName");

                SendTextFile(MessageType.SendHTML, fileName, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendHTML, ex)) throw;
            }
		}

        public void SendHTML(String str, Stream stream, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (stream == null) throw new ArgumentNullException("stream");

                SendTextStream(MessageType.SendHTML, stream, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendHTML, ex)) throw;
            }
		}
        
        public void SendHTML(String str, TextReader reader, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (reader == null) throw new ArgumentNullException("reader");

                Send(MessageType.SendHTML, SendPack.ConstructMessage(str, args), reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendHTML, ex)) throw;
            }
		}
        
        public void SendHTMLString(String str, String htmlString, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (htmlString == null) throw new ArgumentNullException("htmlString");

                Send(MessageType.SendHTML, SendPack.ConstructMessage(str, args), htmlString);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendHTML, ex)) throw;
            }
		}

        public void SendJSON(String str, Object json, params object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (json == null) throw new ArgumentNullException("json");

                String jsonString = JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
                Send(MessageType.SendJSON, SendPack.ConstructMessage(str, args), jsonString);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendJSON, ex)) throw;
            }
        }
                
        public void SendJSON(String str, String json, params object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (json == null) throw new ArgumentNullException("json");

                var jsonObject = JsonConvert.DeserializeObject(json);
                String jsonString = JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);

                Send(MessageType.SendJSON, SendPack.ConstructMessage(str, args), jsonString);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendJSON, ex)) throw;
            }
        }
        
        public void SendJSONFile(String str, String fileName, params object[] args)
        {
            if (!Enabled) return;
            if (str == null) throw new ArgumentNullException("str");
            if (fileName == null) throw new ArgumentNullException("fileName");

            try
            {
                String jsonFile = File.ReadAllText(RIUtils.DetermineParameterPath(fileName), Encoding.UTF8);

                var jsonObject = JsonConvert.DeserializeObject(jsonFile);
                String jsonString = JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);
                
                Send(MessageType.SendJSON, SendPack.ConstructMessage(str, args), jsonString);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendJSON, ex)) throw;
            }
        }

        public void SendJSON(String str, Stream stream, params object[] args)
        {
            if (!Enabled) return;
            if (str == null) throw new ArgumentNullException("str");
            if (stream == null) throw new ArgumentNullException("stream");

            try
            {
                Byte[] bStream = new Byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(bStream, 0, bStream.Length);

                String jsonStream = Encoding.UTF8.GetString(bStream);

                var jsonObject = JsonConvert.DeserializeObject(jsonStream);
                String jsonString = JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);

                Send(MessageType.SendJSON, SendPack.ConstructMessage(str, args), jsonString);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendJSON, ex)) throw;
            }
        }

        public void SendJSON(String str, TextReader reader, params object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (reader == null) throw new ArgumentNullException("reader");

                String jsonStream = reader.ReadToEnd();

                var jsonObject = JsonConvert.DeserializeObject(jsonStream);
                String jsonString = JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);

                Send(MessageType.SendJSON, SendPack.ConstructMessage(str, args), jsonString);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendJSON, ex)) throw;
            }
        }
        
        public void SendSQLString(String str, String sql, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (sql == null) throw new ArgumentNullException("sql");

                Send(MessageType.SendSQL, SendPack.ConstructMessage(str, args), sql);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendSQL, ex)) throw;
            }
		}
        
        public void SendSQLScript(String str, String fileName, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (fileName == null) throw new ArgumentNullException("fileName");

                SendTextFile(MessageType.SendSQL, fileName, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendSQL, ex)) throw;
            }            
		}
        
        public void SendSQLScript(String str, Stream stream, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (stream == null) throw new ArgumentNullException("stream");

                SendTextStream(MessageType.SendSQL, stream, str, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendSQL, ex)) throw;
            }            
		}

        public void SendSQLScript(String str, TextReader reader, params object[] args)
		{
			if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (reader == null) throw new ArgumentNullException("reader");

                Send(MessageType.SendSQL, SendPack.ConstructMessage(str, args), reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendSQL, ex)) throw;
            }
		}
                                
		public void SendGeneration(String str, Object obj, params object[] args)
		{
			if( !Enabled ) return;
			
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (obj == null) throw new ArgumentNullException("obj");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendGeneration, "{0} = {1}", str, GC.GetGeneration(obj), args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendGeneration, ex ) ) throw;
			}						
		}
        
		public void SendGeneration(String str, WeakReference wRef, params object[] args)
		{
			if( !Enabled ) return;
			
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (wRef == null) throw new ArgumentNullException("wRef");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendGeneration, "{0} = {1}", str, GC.GetGeneration(wRef), args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendGeneration, ex ) ) throw;
			}						
		}
        
		public void SendObject(String str, Object obj, ObjectScope scope, params object[] args)
		{	
			SendObject( str, obj, scope, true, args);
		}

		public void SendObject(String str, Object obj, params object[] args )
		{	
			SendObject(str, obj, ObjectScope.All, false, args);
		}

		public void SendObject( String str, Object obj, Boolean bIgnoreStandard, params object[] args)
		{
			SendObject(str, obj, ObjectScope.All, bIgnoreStandard, args);
		}
        
        public void ViewerSendWatch(String labelID, String str, params Object[] args)
		{ 
			if( !Enabled ) return;			

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (labelID == null) throw new ArgumentNullException("labelID");
                
                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.ViewerSendWatch, str, args);
                    controlValues.SendPack.FDetails = new DetailContainerString(labelID);
                    Send(controlValues);
                }
			}
			catch( Exception ex)
			{
				if (!SendInternalError(MessageType.ViewerSendWatch, ex)) throw;
			}								
		}

        public void ViewerSendWatch(String labelID, Object obj)
        {
            if (!Enabled) return;

            try
            {
                if (labelID == null) throw new ArgumentNullException("labelID");

                ViewerSendWatch(labelID, obj != null ? obj.ToString() : "null");
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.ViewerSendWatch, ex)) throw;
            }
        }

        public void SendException(String str, Exception excep, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (excep == null) throw new ArgumentNullException("excep");

				Send(MessageType.SendException, String.Format("{0}:[{1}] - {2}", SendPack.ConstructMessage(str, args), excep.GetType().Name, excep.Message), ExceptionFormatter.ConstructIndentedMessage(excep));
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendException, ex)) throw;
			}								
		}
        
		public void SendException(Exception excep)
		{
			if (!Enabled) return;

			try
			{
				if (excep == null) throw new ArgumentNullException("excep");

				Send(MessageType.SendException, excep.Message, ExceptionFormatter.ConstructIndentedMessage(excep)); 
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendException, ex)) throw;
			}								
		}

        public void SendCurrency(String str, Decimal? val, CultureInfo ci, params object[] args)
		{
			if( !Enabled ) return;
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (ci == null) throw new ArgumentNullException("ci");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendCurrency, "{0} = {1}", str, val.HasValue ? val.Value.ToString("c", ci): "null", args);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendCurrency, ex ) ) throw;
			}								
		}

        public void SendCurrency(String str, Decimal? val, params object[] args)
		{
			SendCurrency( str, val, Thread.CurrentThread.CurrentCulture, args );
		}

        public void SendDateTime(String str, DateTime? dt, String format, CultureInfo ci, params object[] args)
		{
			if( !Enabled ) return;
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (format == null) throw new ArgumentNullException("format");
                if (ci == null) throw new ArgumentNullException("ci");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendDateTime, "{0} = {1}", SendPack.ConstructMessage(str, args), dt.HasValue ? dt.Value.ToString(format, ci) : "null");
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendDateTime, ex ) ) throw;
			}								
		}

		public void SendDateTime(String str, DateTime? dt, String format, params object[] args)
		{
			SendDateTime(str, dt, format, Thread.CurrentThread.CurrentCulture, args);
		}
        
		public void SendDateTime(String str, DateTime? dt, CultureInfo ci, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (ci == null) throw new ArgumentNullException("ci");

				SendDateTime(str, dt, String.Format("{0} {1}", ci.DateTimeFormat.ShortDatePattern, ci.DateTimeFormat.LongTimePattern), ci, args);
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendDateTime, ex ) ) throw;
			}								
		}
        
		public void SendDateTime(String str, DateTime? dt, params object[] args)
		{
			SendDateTime(str, dt, Thread.CurrentThread.CurrentCulture, args);
		}

        public void SendTimestamp(String str, String timeZoneId, params object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (timeZoneId == null) throw new ArgumentNullException("timeZoneId");

                String display = String.Empty;
                try
                {
                    TimeZoneInfo tz = RIUtils.FindSystemTimeZoneById(timeZoneId);
                    DateTime dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, tz.Id);
                    
                    String displayName;
                    if (tz.Id == TimeZoneInfo.Utc.Id)
                        displayName = "(UTC+00:00)";
                    else
                        displayName = tz.DisplayName;

                    display = String.Format("{0} {1}", dt.ToString("yyyy-mm-ddTHH:mm:ss.fff"), displayName);
                }
                catch (TimeZoneNotFoundException)
                {
                    display = String.Format("Unable to find Time Zone: '{0}'", timeZoneId);
                }
                catch (InvalidTimeZoneException)
                {
                    display = String.Format("TInvalid Time Zone: '{0}'", timeZoneId);
                }

                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.SendTimestamp, "{0}: {1}", SendPack.ConstructMessage(str, args), display);
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTimestamp, ex)) throw;
            }
        }

        public void SendTimestamp(String str, TimeZoneInfo tz, params object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");
                if (tz == null) throw new ArgumentNullException("tz");

                SendTimestamp(str, tz.Id, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTimestamp, ex)) throw;
            }
        }
        
        public void SendTimestamp(TimeZoneInfo tz)
        {
            if (!Enabled) return;

            try
            {                
                if (tz == null) throw new ArgumentNullException("tz");

                SendTimestamp("Timestamp", tz.Id);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTimestamp, ex)) throw;
            }
        }
        
        public void SendTimestamp(String str, params object[] args)
        {
            if (!Enabled) return;

            try
            {
                if (str == null) throw new ArgumentNullException("str");

                SendTimestamp(str, TimeZoneInfo.Local, args);
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTimestamp, ex)) throw;
            }
        }

        public void SendTimestamp()
        {
            if (!Enabled) return;

            SendTimestamp("Timestamp", TimeZoneInfo.Local);
        }

		public void SendPoint( String str, Point pt, params object[] args )
		{
			if( !Enabled ) return;
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (pt == null) throw new ArgumentNullException("pt");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendPoint, "{0} = (X:{1}, Y:{2})", SendPack.ConstructMessage(str, args), pt.X, pt.Y);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendPoint, ex ) ) throw;
			}								
		}

		public void SendRectangle( String str, Rectangle rect, params object[] args)
		{
			if( !Enabled ) return;
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (rect == null) throw new ArgumentNullException("rect");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendRectangle, "{0} = (X:{1}, Y:{2}, W:{3}, H:{4})", SendPack.ConstructMessage(str, args), rect.X, rect.Y, rect.Width, rect.Height);
					Send(controlValues);		
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendRectangle, ex ) ) throw;
			}								
		}

		public void SendSize( String str, Size sz, params object[] args )
		{
			if( !Enabled ) return;
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (sz == null) throw new ArgumentNullException("sz");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendSize, "{0} = (W:{1}, H:{2})", SendPack.ConstructMessage(str, args), sz.Width, sz.Height);
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendSize, ex) ) throw;
			}								
		}

		public void SendColor( String str, Color clrObj, params object[] args )
		{
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (clrObj == null) throw new ArgumentNullException("clrObj");

				ReflectInsightColorInfo ci = new ReflectInsightColorInfo();

				// construct color info
                ci.FColor = clrObj.ToArgb(); 
				ci.FHue = (Byte )(((clrObj.GetHue() * 240) / 360) + 0.5);
				ci.FSaturation = (Byte )((clrObj.GetSaturation() * 240) + 0.5);
				ci.FBrightness = (Byte )((clrObj.GetBrightness() * 240) + 0.5);
						
				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendColor, 
                        clrObj.IsNamedColor ? String.Format("{0} = {1} (A:{2}, R:{3}, G:{4}, B:{5})", str, clrObj.Name, clrObj.A, clrObj.R, clrObj.G, clrObj.B ) :
                        String.Format("{0} = 0x{1} (A:{2}, R:{3}, G:{4}, B:{5})", str, ci.FColor.ToString("X08"), clrObj.A, clrObj.R, clrObj.G, clrObj.B ), args);
					controlValues.SendPack.FDetails = ci;                    
					Send(controlValues);		
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendColor, ex ) ) throw;
			}								
		}
        
		public void SendCheckmark( String str, Checkmark cmType, params object[] args )
		{ 
			if( !Enabled ) return;			
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendCheckmark, str, args);
					controlValues.SendPack.FMessageSubType = (Byte)cmType;
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendCheckmark, ex ) ) throw;
			}								
		}

		public void SendCheckmark( String str, params Object[] args )
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

                SendCheckmark(SendPack.ConstructMessage(str, args), DefaultCheckmark);
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendCheckmark, ex)) throw;
			}								
		}		
        
		public Boolean SendAssert(Boolean condition, String str, params Object[] args)
		{
            if (condition || !Enabled) return condition; 
			
			try
			{
                if (str == null) throw new ArgumentNullException("str");

				List<String> ignoreList = new List<String>();                
				ignoreList.Add("ReflectInsight.SendAssert");
				ignoreList.Add("GReflectInsight.SendAssert");
				ignoreList.Add("GDebugReflectInsight.SendAssert");

                Send(MessageType.SendAssert, SendPack.ConstructMessage(str, args), SimpleAPIHelper.GetIdentedCallStack(ignoreList));                
			}
			catch( Exception ex)
			{
                if (!SendInternalError(MessageType.SendAssert, ex)) throw;
			}

            return condition;
		}

		public Boolean SendAssigned(String str, Object obj, params object[] args)
		{ 
			Boolean bAssigned = obj != null;

            if (!Enabled) return bAssigned;
								
			try
			{
                if (str == null) throw new ArgumentNullException("str");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    controlValues.SendPack.Set(MessageType.SendAssigned, String.Format("{0} is {1}", str, bAssigned ? "Assigned" : "NOT Assigned"), args);
					controlValues.SendPack.FMessageSubType = (Byte)(bAssigned ? 1 : 0);
					Send(controlValues);		
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendAssigned, ex ) ) throw;
			}								

            return bAssigned;
		}

		public void SendStackTrace( String str, params Object[] args )		        
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");

				// need to add the ignore list twice because this
				// method is overloaded twice
				List<String> ignoreList = new List<String>();
				ignoreList.Add("ReflectInsight.SendStackTrace");
				ignoreList.Add("ReflectInsight.SendStackTrace");
				ignoreList.Add("GReflectInsight.SendStackTrace");
				ignoreList.Add("GReflectInsight.SendStackTrace");
				ignoreList.Add("GDebugReflectInsight.SendStackTrace");
				ignoreList.Add("GDebugReflectInsight.SendStackTrace");

                Send(MessageType.SendStackTrace, SendPack.ConstructMessage(str, args), SimpleAPIHelper.GetIdentedCallStack(ignoreList));
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendStackTrace, ex)) throw;
			}								
		}

		public void SendStackTrace()
		{ 		
			SendStackTrace("Stack Trace");
		}

		public void SendProcessInformation(Process aProcess)
		{ 
			if( !Enabled ) return; 								                                    

			try
			{
				if (aProcess == null) throw new ArgumentNullException("aProcess");

                _SendCustomData(MessageType.SendProcessInformation, String.Format("Process ID: {0}", aProcess.Id), SimpleAPIHelper.GetProcessInformation(aProcess));				
            }
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendProcessInformation, ex) ) throw;
			}
		}

		public void SendProcessInformation()
		{ 
			if( !Enabled ) return;

            using (Process process = Process.GetCurrentProcess())
            {               
                SendProcessInformation(process);
            }
		}

		public void SendDataSet(String str, DataSet dSet, params object[] args)
		{
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (dSet == null) throw new ArgumentNullException("dSet");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    using (DetailContainerDataSet details = new DetailContainerDataSet(dSet))
                    {
                        controlValues.SendPack.Set(MessageType.SendDataSet, str, args);
                        controlValues.SendPack.FDetails = details;
                        Send(controlValues);
                    }
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendDataSet, ex ) ) throw;
			}								
		}

		public void SendDataSet(DataSet dSet)
		{
			SendDataSet(dSet.DataSetName, dSet);
		}

		public void SendDataSetSchema(String str, DataSet dSet, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (dSet == null) throw new ArgumentNullException("dSet");
			 
				using (DataSet schemaDataSet = new DataSet(dSet.DataSetName))
				{
					foreach (DataTable table in dSet.Tables)
						SimpleAPIHelper.CreateDataTableSchema(schemaDataSet, table);

					ControlValues controlValues = GetControlValues();
					using (new ControlValuesContainer(controlValues))
					{
                        using (DetailContainerDataSet details = new DetailContainerDataSet(schemaDataSet))
                        {
                            controlValues.SendPack.Set(MessageType.SendDataSetSchema, str, args);
                            controlValues.SendPack.FDetails = details;
                            Send(controlValues);
                        }
					}
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendDataSetSchema, ex)) throw;
			}
		}

		public void SendDataSetSchema(DataSet dSet)
		{
			SendDataSetSchema(dSet.DataSetName, dSet);
		}

		public void SendDataTable(String str, DataTable table, params object[] args)
		{
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (table == null) throw new ArgumentNullException("table");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    using (DetailContainerDataTable details = new DetailContainerDataTable(table))
                    {
                        controlValues.SendPack.Set(MessageType.SendDataTable, str, args);
                        controlValues.SendPack.FDetails = details;
                        Send(controlValues);
                    }
				}
			}
			catch( Exception ex)
			{
				if(!SendInternalError(MessageType.SendDataTable, ex)) throw;
			}								
		}

		public void SendDataTable(DataTable table)
		{
			SendDataTable(table.TableName, table);
		}

        public void SendDataTableSchema(String str, DataTable table, params object[] args)
		{
			if (!Enabled) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (table == null) throw new ArgumentNullException("table");

				using (DataSet schemaDataSet = new DataSet())
				{
					SimpleAPIHelper.CreateDataTableSchema(schemaDataSet, table);
					
					ControlValues controlValues = GetControlValues();
					using (new ControlValuesContainer(controlValues))
					{
                        using (DetailContainerDataTable details = new DetailContainerDataTable(schemaDataSet.Tables[0]))
                        {
                            controlValues.SendPack.Set(MessageType.SendDataTableSchema, str, args);
                            controlValues.SendPack.FDetails = details;
                            Send(controlValues);
                        }
					}
				}
			}
			catch (Exception ex)
			{
				if (!SendInternalError(MessageType.SendDataTableSchema, ex)) throw;
			}
		}

		public void SendDataTableSchema(DataTable table)
		{
			SendDataTableSchema(table.TableName, table);
		}

		public void SendDataView(String str, DataView view, params object[] args)
		{
			if( !Enabled ) return;

			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (view == null) throw new ArgumentNullException("view");

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
                    using (DetailContainerDataTable details = new DetailContainerDataTable(view.Table))
                    {
                        controlValues.SendPack.Set(MessageType.SendDataView, str, args);
                        controlValues.SendPack.FDetails = details;
                        Send(controlValues);
                    }
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendDataView, ex) ) throw;
			}								
		}

		public void SendDataView(DataView view)
		{
			SendDataView(view.Table.TableName, view);
		}

        public void SendTypedCollection<T>(String str, params IEnumerable<T>[] enumerables)
        {
            if (!Enabled) return;

            if (enumerables == null) throw new ArgumentNullException("enumerables");

            try
            {
                using (DataSet ds = SimpleAPIHelper.PopulateDataSet(enumerables))
                {
                    ControlValues controlValues = GetControlValues();
                    using (new ControlValuesContainer(controlValues))
                    {
                        using (DetailContainerDataSet details = new DetailContainerDataSet(ds))
                        {
                            controlValues.SendPack.Set(MessageType.SendTypedCollection, str ?? ds.DataSetName);
                            controlValues.SendPack.FDetails = details;
                            Send(controlValues);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.SendTypedCollection, ex)) throw;
            }
        }

        public void SendTypedCollection<T>(IEnumerable<T> enumerable)
        {
            SendTypedCollection(null, enumerable);
        }

		public void SendAttachment(String str, String fileName, params object[] args)
		{
			if( !Enabled ) return;
						
			try
			{
                if (str == null) throw new ArgumentNullException("str");
                if (fileName == null) throw new ArgumentNullException("fileName");

				ReflectInsightAttachmentInfo aInfo = new ReflectInsightAttachmentInfo();
				DetailContainerByteArray bInfo = new DetailContainerByteArray(null);

                String fname = RIUtils.DetermineParameterPath(fileName);
                try
                {
                    using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Byte[] bStream = new Byte[fs.Length];
                        fs.Seek(0, SeekOrigin.Begin);
                        fs.Read(bStream, 0, bStream.Length);

                        aInfo.FileName = fname;
                        aInfo.FileSize = bStream.Length;
                        bInfo.FData = bStream;
                    }
				}
                catch (Exception ex)
                {
                    throw new ReflectInsightException(String.Format("File: '{0}', caused the following error: {1}", fileName, ex.Message), ex);
                }

				ControlValues controlValues = GetControlValues();
				using (new ControlValuesContainer(controlValues))
				{
					controlValues.SendPack.Set(MessageType.SendAttachment, str, args);
					controlValues.SendPack.FSubDetails = aInfo;
					controlValues.SendPack.FDetails = bInfo;
					Send(controlValues);
				}
			}
			catch( Exception ex)
			{
				if( !SendInternalError( MessageType.SendAttachment, ex ) ) throw;
			}								
		}

        public void SendString(String str, String theString, params object[] args)
        {
            SendObject(str, theString, args);
        }

        public void SendString(String str, StringBuilder theString, params object[] args)
        {
            SendObject(str, theString, args);
        }

        public void PurgeLogFile()
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                ControlValues controlValues = GetControlValues();
                using (new ControlValuesContainer(controlValues))
                {
                    controlValues.SendPack.Set(MessageType.PurgeLogFile, String.Empty, new object[] {});
                    Send(controlValues);
                }
            }
            catch (Exception ex)
            {
                if (!SendInternalError(MessageType.PurgeLogFile, ex)) throw;
            }
        }

        #endregion
		
		#region Public Properties

		public String Category 
		{
			get { return FCategory; }
			set { FCategory = value.IfNullOrEmptyUseDefault("ReflectInsight"); }
		}

		public Color BackColor
		{
			set 
			{ 
				FBkColor = value; 
				if( FBkColor.A == 0x00)
					FBkColor = Color.FromArgb(0xFF, FBkColor.R, FBkColor.G, FBkColor.B);
			}
			get { return FBkColor; }
		}

		#endregion	

		#region Public Static Properties

		static public void ResetEnterExitIndentation()
		{
			ControlValues controlValues = GetControlValues();
            controlValues.IndentValue.Reset();
		}

		static public Int32 Indent()
		{
			var controlValues = GetControlValues();
            
            controlValues.IndentValue.Increment();
			return controlValues.IndentValue.Level;
		}

		static public Int32 Unindent()
		{
			ControlValues controlValues = GetControlValues();
            
            controlValues.IndentValue.Decrement();
			return controlValues.IndentValue.Level;
		}

		static public Int32 IndentLevel
		{
			get
			{
				ControlValues controlValues = GetControlValues();
                return controlValues.IndentValue.Level;
			}
		}
	   
		#endregion	
	}

	internal sealed class IndentValue
	{
		private const SByte RI_MAX_INDENT = 20;     
		private SByte FLevel = 0;
		
		public void Increment()
		{
			FLevel++;
			if (FLevel > RI_MAX_INDENT)
				FLevel = RI_MAX_INDENT;
		}

		public void Decrement()
		{
			if( FLevel > 0 ) FLevel--;
		}

		public void Reset() 
		{ 
			FLevel = 0; 
		}

		public SByte Level
		{
			get { return FLevel; }
		}
	}

	internal sealed class SendPack
	{        
        public MessageType FMessageType = MessageType.Clear;        
		public Byte FMessageSubType = 0;        
		public String FMessage;        
		public IFastBinarySerializable FDetails;
        public IFastBinarySerializable FSubDetails;

        static public String ConstructMessage(String sMessage, params Object[] args)
        {
            if (sMessage != null)
            {
                return (args == null || args.Length == 0 ? sMessage : String.Format(sMessage, args));
            }
           
            return (sMessage ?? String.Empty);
		}	

		public void Set(MessageType mType, String sMessage, params Object[] args)
		{
            FMessage = ConstructMessage(sMessage, args);
			FMessageType = mType;
            FDetails = null;
            FSubDetails = null;
        }

        public void Release()
        {
            FMessage = null;
            FDetails = null;
            FSubDetails = null;
        }
	}	
}
