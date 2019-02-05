using Plato.Serializers;
using Plato.Serializers.Interfaces;
using System;

namespace ReflectSoftware.Insight.Common.Router
{
    public class MessageRequestConstants
    {
        static public readonly Int32 MAX_CHUNKSIZE = (1048576 * 3);        
        static public readonly Int32 MAX_COMPRESSION = 1048576 / 2;
    }

    public enum MessageRequestType
    {
        /// <summary>
        /// A Request
        /// </summary>
        Request,
        /// <summary>
        /// A Sequence
        /// </summary>
        Sequence
    }


    public class MessageHeader : IFastBinarySerializable
    {        
        public MessageRequestType RequestType { get; set; }
        public FastSerializerObjectData Request { get; set; }   // depending on the request type, this blob can either be a MessageRequest or MessageSequence object
        
        public MessageHeader(FastBinaryFormatter ff, MessageRequestType requestType, IFastBinarySerializable request)
        {            
            RequestType = requestType;
            Request = new FastSerializerObjectData(ff, request);
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(RequestType.GetHashCode());
            writer.Write(Request);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            // Version 5.0 data types
            RequestType = (MessageRequestType)reader.ReadInt32();
            Request = reader.ReadObject<FastSerializerObjectData>();
        }
    }

    public class MessageRequest : IFastBinarySerializable
    {
        public UInt64 SessionId { get; set; }
        public Int32 DestinationBinding { get; set; }
        public Byte[] EncryptedKey { get; set; }                // if null then encryption was not applied
        public Byte[] EncryptedIV { get; set; }                 // if null then encryption was not applied
        public String CertificateThumbprint { get; set; }       // if null then encryption was not applied        
        public UInt64 RequestId { get; set; }
        public Int16 SequenceCount { get; set; }
        public Int32 DecompressedLength { get; set; }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(SessionId);
            writer.Write(DestinationBinding);
            writer.WriteByteArray(EncryptedKey);
            writer.WriteByteArray(EncryptedIV);
            writer.WriteSafeString(CertificateThumbprint);
            writer.Write(RequestId);
            writer.Write(SequenceCount);
            writer.Write(DecompressedLength);
        }
        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            // Version 5.0 data types
            SessionId = reader.ReadUInt64();
            DestinationBinding = reader.ReadInt32();
            EncryptedKey = reader.ReadByteArray();
            EncryptedIV = reader.ReadByteArray();
            CertificateThumbprint = reader.ReadSafeString();
            RequestId = reader.ReadUInt64();
            SequenceCount = reader.ReadInt16();
            DecompressedLength = reader.ReadInt32();
        }
    }

    public class MessageSequence : IFastBinarySerializable
    {
        public UInt64 SessionId { get; set; }
        public UInt64 RequestId { get; set; }
        public Int16 Sequence { get; set; }
        public Byte[] Chunk { get; set; }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(SessionId);
            writer.Write(RequestId);
            writer.Write(Sequence);
            writer.WriteByteArray(Chunk);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            // Version 5.0 data types
            SessionId = reader.ReadUInt64();
            RequestId = reader.ReadUInt64();
            Sequence = reader.ReadInt16();
            Chunk = reader.ReadByteArray();
        }
    }
}
