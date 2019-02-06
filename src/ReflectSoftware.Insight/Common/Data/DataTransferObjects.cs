using Plato.Extensions;
using Plato.Serializers;
using Plato.Serializers.FormatterPools;
using Plato.Serializers.Interfaces;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.Linq;

namespace ReflectSoftware.Insight.Common.Data
{
    public class ReflectInsightExtendedProperties : IFastBinarySerializable
    {
        public String Caption { get; set; }
        public NameValueCollection Properties { get; set; }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(Caption ?? String.Empty);
            writer.Write((Int16)Properties.Count);
            foreach (String key in Properties.AllKeys)
            {
                writer.Write(key ?? String.Empty);
                writer.Write(Properties[key] ?? String.Empty);
            }
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            Caption = reader.ReadString();
            Properties = new NameValueCollection();

            Int16 count = reader.ReadInt16();
            for (Int32 i = 0; i < count; i++)
            {
                Properties[reader.ReadString()] = reader.ReadString();
            }
        }
    }

    public class ReflectInsightPropertiesContainer : IFastBinarySerializable
    {
        public ReflectInsightExtendedProperties[] ExtendedProperties;

        public ReflectInsightPropertiesContainer(ReflectInsightExtendedProperties[] extendedProperties)
        {
            ExtendedProperties = extendedProperties;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(ExtendedProperties);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            ExtendedProperties = reader.ReadEnumerable<ReflectInsightExtendedProperties>().ToArray();
        }
    }

    public class ReflectInsightColorInfo: IFastBinarySerializable
    {
        public Int32 FColor;
        public Byte FHue;
        public Byte FSaturation;
        public Byte FBrightness;

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(FColor);
            writer.Write(FHue);
            writer.Write(FSaturation);
            writer.Write(FBrightness);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FColor = reader.ReadInt32();
            FHue = reader.ReadByte();
            FSaturation = reader.ReadByte();
            FBrightness = reader.ReadByte();
        }
    }

    public class ReflectInsightAttachmentInfo : IFastBinarySerializable
    {
        public String FileName;
        public Int32 FileSize;

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.WriteSafeString(FileName);
            writer.Write(FileSize);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FileName = reader.ReadSafeString();
            FileSize = reader.ReadInt32();
        }
    }

    public class DetailContainerByteArray: IFastBinarySerializable
    {        
        public Byte[] FData;

        public DetailContainerByteArray(Byte[] data)
        {
            FData = data;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.WriteByteArray(FData);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FData = reader.ReadByteArray();
        }
    }

    public class DetailContainerString: IFastBinarySerializable
    {
        public String FData;

        public DetailContainerString(String data)
        {
            FData = data;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.WriteSafeString(FData);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FData = reader.ReadSafeString();
        }

        public override string ToString()
        {
            return FData;    
        }
    }

    public class DetailContainerDataSet: IFastBinarySerializable, IDisposable
    {
        private Boolean FbOwnsData;
        public DataSet FData;
        public Boolean Disposed { get; private set; }

        public DetailContainerDataSet(DataSet data)
        {
            Disposed = false;
            FbOwnsData = false;
            FData = data;
        }
    
        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);
                 
                    if (FbOwnsData && FData != null)
                        FData.Dispose();
                    
                    FData = null;
                }
            }
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(FData);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FbOwnsData = true;
            FData = reader.ReadDataSet();
        }
    }

    public class DetailContainerDataTable: IFastBinarySerializable, IDisposable
    {        
        private Boolean FbOwnsData;        
        public DataTable FData;
        public Boolean Disposed { get; private set; }

        public DetailContainerDataTable(DataTable data)
        {
            Disposed = false;
            FbOwnsData = false;
            FData = data;
        }
        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    if (FbOwnsData && FData != null)
                        FData.Dispose();
                    
                    FData = null;
                }
            }
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(FData);
        }
        
        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FbOwnsData = true;
            FData = reader.ReadDataTable();
        }
    }

    public class DetailContainerInt32 : IFastBinarySerializable
    {        
        public Int32 FValue;

        public DetailContainerInt32(Int32 value)
        {
            FValue = value;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(FValue);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FValue = reader.ReadInt32();
        }
    }


    public class ReflectInsightPackage : IFastBinarySerializable
    {
        public Int32 FSequenceID;        
        public UInt32 FSessionID;        
        public UInt32 FRequestID;        
        public Int16 FSourceUtcOffset;
        public DateTime FDateTime;        
        public Int32 FDomainID;        
        public Int32 FProcessID;        
        public Int32 FThreadID;        
        public String FCategory;        
        public String FApplication;        
        public String FMachineName;        
        public String FUserDomainName;        
        public String FUserName;        
        public SByte FIndentLevel;        
        public Color FBkColor;        
        public MessageType FMessageType;
        public Byte FMessageSubType;        
        public String FMessage;        
        public Int32 FDetailType;        
        public ReflectInsightPropertiesContainer FExtPropertyContainer;        
        public FastSerializerObjectData FSubDetails;        
        public FastSerializerObjectData FDetails;
                        
        public ReflectInsightPackage()
        {
            // Version 5.0 data types            
            FSequenceID = 0;
            FSessionID = 0;
            FRequestID = 0;
            FSourceUtcOffset = 0;
            FDateTime = DateTime.MinValue;
            FDomainID = 0;
            FProcessID = 0;
            FThreadID = 0;
            FCategory = String.Empty;
            FApplication = String.Empty;
            FMachineName = String.Empty;
            FUserDomainName = String.Empty;
            FUserName = String.Empty;
            FIndentLevel = 0;
            FBkColor = Color.White;
            FMessageType = MessageType.Clear;
            FMessageSubType = 0;
            FMessage = String.Empty;
            FDetailType = 0;
            FExtPropertyContainer = null;
            FSubDetails = null;
            FDetails = null;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            // Version 5.0 data types
            writer.Write(FSequenceID);
            writer.Write(FSessionID);
            writer.Write(FRequestID);
            writer.Write(FSourceUtcOffset);
            writer.Write(FDateTime);            
            writer.Write(FDomainID);
            writer.Write(FProcessID);
            writer.Write(FThreadID);
            writer.Write(FCategory);
            writer.Write(FApplication);
            writer.Write(FMachineName);
            writer.Write(FUserDomainName);
            writer.Write(FUserName);
            writer.Write(FIndentLevel);
            writer.Write(FBkColor.ToArgb());
            writer.Write(FMessageType.GetHashCode());
            writer.Write(FMessageSubType);
            writer.Write(FMessage);
            writer.Write(FDetailType);
            writer.Write(FExtPropertyContainer);
            writer.Write(FSubDetails);
            writer.Write(FDetails);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            // Version 5.0 data types
            FSequenceID = reader.ReadInt32();
            FSessionID = reader.ReadUInt32();
            FRequestID = reader.ReadUInt32();
            FSourceUtcOffset = reader.ReadInt16();
            FDateTime = reader.ReadDateTime();            
            FDomainID = reader.ReadInt32();
            FProcessID = reader.ReadInt32();
            FThreadID = reader.ReadInt32();
            FCategory =  reader.ReadString();
            FApplication = reader.ReadString();
            FMachineName = reader.ReadString();
            FUserDomainName = reader.ReadString();
            FUserName = reader.ReadString();
            FIndentLevel = reader.ReadSByte();
            FBkColor = Color.FromArgb(reader.ReadInt32());
            FMessageType = (MessageType)reader.ReadInt32();
            FMessageSubType = reader.ReadByte();
            FMessage = reader.ReadString();
            FDetailType = reader.ReadInt32();
            FExtPropertyContainer = reader.ReadObject<ReflectInsightPropertiesContainer>();
            FSubDetails = reader.ReadObject<FastSerializerObjectData>();
            FDetails = reader.ReadObject<FastSerializerObjectData>();
        }

        public Boolean HasExtendedProperties
        {
            get { return FExtPropertyContainer != null; }
        }
        public Boolean HasDetails
        {
            get { return FDetails != null; }
        }
        
        public Boolean HasSubDetails
        {
            get { return FSubDetails != null; }
        }
        
        public Boolean IsDetail<T>()
        {
            return FDetailType == (Int32)typeof(T).FullName.BKDRHash();
        }
        
        public void ClearExtendedProperties()
        {
            FExtPropertyContainer = null;
        }
        
        public void ClearDetails()
        {
            FDetails = null;
        }

        public void ClearSubDetails()
        {
            FSubDetails = null;
        }
        
        public void SetDetails(IFastBinarySerializable details, Boolean bSetType)
        {
            if (details == null)
            {
                if(bSetType)
                    FDetailType = 0;

                return;
            }

            if (bSetType)
                FDetailType = (Int32)details.GetType().FullName.BKDRHash();

            using (var pool = FastFormatterPool.Pool.Container())
            {
                FDetails = new FastSerializerObjectData(pool.Instance, details);                
            }
        }        

        public void SetDetails(IFastBinarySerializable details)
        {
            SetDetails(details, true);
        }
        

        public void SetSubDetails(IFastBinarySerializable subDetails)
        {
            if (subDetails == null)
                return;

            using (var pool = FastFormatterPool.Pool.Container())
            {
                FSubDetails = new FastSerializerObjectData(pool.Instance, subDetails);
            }
        }

        public T GetSubDetails<T>() where T : IFastBinarySerializable
        {
            T subDetails;
            if (FSubDetails != null)
                subDetails = FSubDetails.GetObject<T>();
            else
                subDetails = default(T);

            return subDetails;
        }

        public T GetSubDetails<T>(FastBinaryFormatter ff) where T : IFastBinarySerializable
        {
            T subDetails;
            if (FSubDetails != null)
            {
                subDetails = FSubDetails.GetObject<T>(ff);
            }
            else
            {
                subDetails = default(T);
            }

            return subDetails;
        }

        public T GetDetails<T>() where T : IFastBinarySerializable
        {
            T details;
            if (FDetails != null)
                details = FDetails.GetObject<T>();
            else
                details = default(T);

            return details;
        }

        public T GetDetails<T>(FastBinaryFormatter ff) where T : IFastBinarySerializable
        {
            T details;
            if (FDetails != null)
                details = FDetails.GetObject<T>(ff);
            else
                details = default(T);

            return details;
        }
    }
}
