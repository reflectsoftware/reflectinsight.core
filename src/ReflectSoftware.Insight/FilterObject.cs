using Plato.Serializers;
using Plato.Serializers.Interfaces;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ReflectSoftware.Insight
{
    /*** String ID's for MessageType, Checkpoint, Checkmark and LevelType
    AddCheckpoint -- all check points
	AddCheckpointRed
	AddCheckpointOrange
	AddCheckpointYellow
	AddCheckpointGreen
	AddCheckpointBlue
	AddCheckpointPurple

    SendCheckmark -- all checkmarks 
    SendCheckmarkRed
    SendCheckmarkOrange
	SendCheckmarkYellow
	SendCheckmarkGreen
	SendCheckmarkBlue
	SendCheckmarkPurple
	
    SendLevel -- all level types
	SendLevelRed
	SendLevelOrange
	SendLevelYellow
	SendLevelGreen
	SendLevelBlue
	SendLevelCyan
	SendLevelPurple
    SendLevelMagenta
							
	Unknown
    AddSeparator		
	EnterExitMethod
    SendMessage
    SendNote
    SendInformation    
    SendWarning
    SendError
    SendFatal
    SendMiniDumpFile      
    SendDebug
    SendTrace     
    SendStart
    SendStop
    SendSuspend
    SendResume
    SendTransfer
    SendVerbose    
    SendReminder
    SendTextFile
    SendXML
    SendHTML
    SendSQL
    SendImage
    SendStream
    SendMemory
    SendMemoryStatus
    SendGeneration
    SendCustomData
    SendObject
    SendSerializedObject    
    SendCustomData
    SendException
    SendDateTime
    SendTimestamp
    SendCurrency
    SendPoint
    SendRectangle
    SendSize
    SendColor        
    SendAssert
    SendAttachment
    SendLoadedAssemblies
    SendCollection
    SendAssigned
    SendStackTrace
    SendProcessInformation
    SendAppDomainInformation
    SendThreadInformation
    SendSystemInformation
    SendDataSet
    SendDataTable
    SendDataView
    SendDataSetSchema,
    SendDataTableSchema
    SendHttpModuleInformation
    SendHttpRequest
    SendAuditSuccess
    SendAuditFailure    
    SendInternalError
    SendComment
    SendEnum
    SendBoolean
    SendByte
    SendChar
    SendDecimal
    SendDouble
    SendSingle
    SendInteger
    SendString
    ****/


    public enum FilterMode
	{
        /// <summary>
        /// Include
        /// </summary>
        Include,
        /// <summary>
        /// Exclude
        /// </summary>
		Exclude
	}

	public class FilterInfo : IFastBinarySerializable
	{
        public String Name { get; set; }
        public FilterMode Mode { get; set; }       
        public List<String> IDs { get; private set; }

        #region Constructors
        public FilterInfo(String name, FilterMode mode)
        {
            Name = name;
            Mode = mode;
            IDs = new List<String>();
        }

        #endregion

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.WriteSafeString(Name);
            writer.Write(Mode.GetHashCode());
            writer.Write(IDs.ToArray());
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            Name = reader.ReadSafeString();
            Mode = (FilterMode)reader.ReadInt32();
            IDs = new List<String>(reader.ReadStringArray());
        }
	}

    public class RIFilter : IFastBinarySerializable
	{        
        private FilterInfo FFilterInfo;
        private Boolean FFilterDefined;
        private Boolean[] FMessages;
        private Boolean[] FCheckpoints;
        private Boolean[] FCheckmarks;
        private Boolean[] FLevels;

        #region Constructors

        public RIFilter()
        {            
            FMessages = new Boolean[MessageType._End.GetHashCode()];
            FCheckpoints = new Boolean[Enum.GetNames(typeof(Checkpoint)).Length];
            FCheckmarks = new Boolean[Enum.GetNames(typeof(Checkmark)).Length];
            FLevels = new Boolean[Enum.GetNames(typeof(LevelType)).Length];

            FFilterInfo = new FilterInfo(String.Empty, FilterMode.Include);
            FFilterDefined = false;
            InitFilter(FilterMode.Include);
        }
        #endregion

        public RIFilter(String filterName): this()
        {            
            SetFilter(filterName);
        }

        public RIFilter(FilterInfo fInfo): this()
        {
            SetFilter(fInfo);
        }

        private static void WriteBooleanArray(FastBinaryWriter writer, Boolean[] array)
        {
            writer.Write(array.Length);
            foreach (Boolean value in array)
            {
                writer.Write(value);
            }
        }

        private static Boolean[] ReadBooleanArray(FastBinaryReader reader)
        {
            Boolean[] array = new Boolean[reader.ReadInt32()];
            for (Int32 i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadBoolean();
            }

            return array;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(FFilterInfo);
            writer.Write(FFilterDefined);
            WriteBooleanArray(writer, FMessages);
            WriteBooleanArray(writer, FCheckpoints);
            WriteBooleanArray(writer, FCheckmarks);
            WriteBooleanArray(writer, FLevels);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FFilterInfo = reader.ReadObject<FilterInfo>();
            FFilterDefined = reader.ReadBoolean();
            FMessages = ReadBooleanArray(reader);
            FCheckpoints = ReadBooleanArray(reader);
            FCheckmarks = ReadBooleanArray(reader);
            FLevels = ReadBooleanArray(reader);
        }

        private static void InitializeFilterModeArray(Boolean[] mArray, FilterMode mode)
        {
            for (Int32 i = 0; i < mArray.Length; i++)
            {
                mArray[i] = mode == FilterMode.Exclude;
            }
        }

        private Boolean HandleCategoryMessageTypeIfApplicable(Type subMessageType, String categoryName, Boolean[] types, String id, FilterMode mode)
        {
            if (id.Contains(categoryName))
            {
                if (id == categoryName) // exact match
                {
                    FMessages[Enum.Parse(typeof(MessageType), id).GetHashCode()] = mode == FilterMode.Include;
                    foreach (Object ct in Enum.GetValues(subMessageType))
                    {
                        types[ct.GetHashCode()-1] = mode == FilterMode.Include;
                    }
                    
                    return true; // handled
                }

                id = id.Replace(categoryName, String.Empty);
                if (Enum.IsDefined(subMessageType, id))
                {
                    types[Enum.Parse(subMessageType, id).GetHashCode()-1] = mode == FilterMode.Include;
                    return true; // handled
                }
            }

            return false; // not handled
        }
        
        private void InitFilter(FilterMode mode)
        {
            InitializeFilterModeArray(FMessages, mode);
            InitializeFilterModeArray(FCheckmarks, mode);
            InitializeFilterModeArray(FCheckpoints, mode);
            InitializeFilterModeArray(FLevels, mode);
        }

        public void SetFilter(FilterInfo fInfo)
		{
            FFilterInfo = fInfo;
            FFilterDefined = true;
            InitFilter(FFilterInfo.Mode);

            // assume these category message types are not allowed by default
            FMessages[MessageType.AddCheckpoint.GetHashCode()] = false;
            FMessages[MessageType.SendCheckmark.GetHashCode()] = false;
            FMessages[MessageType.SendLevel.GetHashCode()] = false;

            Type msgTypeFlag = typeof(MessageType);

            foreach (String id in FFilterInfo.IDs)
            {
                if (HandleCategoryMessageTypeIfApplicable(typeof(Checkpoint), "AddCheckpoint", FCheckpoints, id, FFilterInfo.Mode)) continue;
                if (HandleCategoryMessageTypeIfApplicable(typeof(Checkmark), "SendCheckmark", FCheckmarks, id, FFilterInfo.Mode)) continue;
                if (HandleCategoryMessageTypeIfApplicable(typeof(LevelType), "SendLevel", FLevels, id, FFilterInfo.Mode)) continue;
                
                if (id == "EnterExitMethod")
                {
                    FMessages[MessageType.EnterMethod.GetHashCode()] = FFilterInfo.Mode == FilterMode.Include;
                    FMessages[MessageType.ExitMethod.GetHashCode()] = FFilterInfo.Mode == FilterMode.Include;
                }
                else if (Enum.IsDefined(msgTypeFlag, id))
                {
                    FMessages[Enum.Parse(msgTypeFlag, id).GetHashCode()] = FFilterInfo.Mode == FilterMode.Include;
                }
            }

            // these method types are always allowed
            FMessages[MessageType.Clear.GetHashCode()] = true;
            FMessages[MessageType.SendInternalError.GetHashCode()] = true;
            FMessages[MessageType.ViewerClearAll.GetHashCode()] = true;
            FMessages[MessageType.ViewerClearWatches.GetHashCode()] = true;
            FMessages[MessageType.ViewerSendWatch.GetHashCode()] = true;
		}

        public void SetFilter(String filterName)
        {
            FilterInfo fInfo = null;
            if (!string.IsNullOrWhiteSpace(filterName))
            {
                fInfo = ReflectInsightConfig.Settings.GetFilterInfo(filterName);
            }

            if (fInfo != null)
            {
                SetFilter(fInfo);
            }
            else // clear filter
            {
                FFilterInfo.Name = filterName;
                FFilterDefined = false;
                InitFilter(FilterMode.Include);
            }
        }

		private Boolean IsPackageAllowed(ReflectInsightPackage package)
		{            
            if (FMessages[package.FMessageType.GetHashCode()])
            {
                return true;
            }

            if (package.FMessageType == MessageType.AddCheckpoint)
            {
                return FCheckpoints[package.FMessageSubType-1];
            }

            if (package.FMessageType == MessageType.SendCheckmark)
            {
                return FCheckmarks[package.FMessageSubType-1];
            }

            if (package.FMessageType == MessageType.SendLevel)
            {
                return FLevels[package.FMessageSubType-1];
            }

            return false;
		}

        public ReflectInsightPackage FilterMessage(ReflectInsightPackage message)
        {
            if (!FFilterDefined)
            {
                return message;
            }

            if (IsPackageAllowed(message))
            {
                return message;
            }

            return null;
        }

        public ReflectInsightPackage[] FilterMessages(ReflectInsightPackage[] messages)
        {
            // see if there's anything to filter
            if (!FFilterDefined)
                return messages;

            List<ReflectInsightPackage> filterMessages = new List<ReflectInsightPackage>(messages.Length);
            foreach (ReflectInsightPackage message in messages)
            {
              if (IsPackageAllowed(message))
                    filterMessages.Add(message);

                Thread.Sleep(0);
            }

            return filterMessages.ToArray();
        }

        public String Name
        {
            get { return FFilterInfo.Name; }            
        }
	}
}
