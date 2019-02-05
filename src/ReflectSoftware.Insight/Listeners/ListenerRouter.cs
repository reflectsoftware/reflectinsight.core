using Plato.Security.Cryptography;
using Plato.Serializers;
using Plato.Serializers.FormatterPools;
using Plato.Serializers.Interfaces;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using ReflectSoftware.Insight.Common.Router;
using RI.Messaging.ReadWriter;
using System;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;

namespace ReflectSoftware.Insight
{
    internal class ListenerRouter : IReflectInsightListener, IDisposable
	{        
        private readonly Int32 LastUnsuccessfulWaitTime;     
        private readonly MessageRequest ListenerRequest;                
        private DateTime LastUnsuccessfulConnection;        
        private IMessageWriter MessageWriter;                

        public Boolean Disposed { get; private set; }

        public ListenerRouter()
        {
            Disposed = false;
            ListenerRequest = new MessageRequest() { SessionId = CryptoServices.RandomIdToUInt64() };
            LastUnsuccessfulConnection = DateTime.MinValue;
            LastUnsuccessfulWaitTime = 5; // 5 seconds
        }

		public void Dispose()
		{
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    if (MessageWriter != null)
                    {
                        if (MessageWriter is IThreadExceptionEvent)
                            (MessageWriter as IThreadExceptionEvent).OnThreadException -= OnWriterThreadException;

                        MessageWriter.Dispose();
                        MessageWriter = null;
                    }
                }
            }
		}

        public void UpdateParameterVariables(IListenerInfo listener)
        {
            String routerName = listener.Params["name"].Trim();
            if (string.IsNullOrWhiteSpace(routerName))
            {                
                throw new ReflectInsightException(String.Format("Missing name parameter for listener: '{0}' using details: '{1}'.", listener.Name, listener.Details));
            }

            NameValueCollection routerParameters = ReflectInsightConfig.Settings.GetSubsection(String.Format("routers.{0}", routerName));
            if (routerParameters == null)
            {             
                throw new ReflectInsightException(String.Format("Missing router section '{0}' for listener: '{1}' using details: '{2}'.", routerName, listener.Name, listener.Details));
            }

            if (string.IsNullOrWhiteSpace(routerParameters["type"]))
            {                
                throw new ReflectInsightException(String.Format("Missing type parameter for router: '{0}'. Insure that the router is correctly configured.", routerName));
            }

            ListenerRequest.DestinationBinding = 0;
            if (!string.IsNullOrWhiteSpace(listener.Params["destinationBindingGroup"]))
            {
                ListenerRequest.DestinationBinding = DestinationBindingGroup.GetId(listener.Params["destinationBindingGroup"]);
            }

            MessageWriter = ReadWriterFactory.CreateInstance<IMessageWriter>(routerParameters);
            if (MessageWriter is IThreadExceptionEvent)
                (MessageWriter as IThreadExceptionEvent).OnThreadException += OnWriterThreadException;
        }

        private void OnWriterThreadException(Exception ex)
        {
            RIExceptionManager.PublishIfEvented(ex);
        }

        private void WriteRequest(MessageRequestType requestType, IFastBinarySerializable request)
        {
            Byte[] bData = null;
            using (var pool = FastFormatterPool.Pool.Container())
            {
                bData = pool.Instance.Serialize(new MessageHeader(pool.Instance, requestType, request));                
            }

            MessageWriter.Write(bData);
        }

        private void ConstructAndSendMessages(ReflectInsightPackage[] messages)
        {
            if (DateTime.Now.Subtract(LastUnsuccessfulConnection).TotalSeconds < LastUnsuccessfulWaitTime)
            {
                // The last successful connection was less than the specified time, just return.
                // We need to try again later
                return;
            }
            
            // serialize the packages to a binary blob
            Byte[] bData = null;
            using (var pool = FastFormatterPool.Pool.Container())
            {
                bData = FastSerializerEnumerable<ReflectInsightPackage>.Serialize(pool.Instance, messages);
            }

            ListenerRequest.DecompressedLength = 0;
            if (bData.Length > MessageRequestConstants.MAX_COMPRESSION)
            {
                ListenerRequest.DecompressedLength = bData.Length;

                // compress data
                using (var ms = new MemoryStream())
                {
                    using (GZipStream msCompressed = new GZipStream(ms, CompressionMode.Compress))
                    {
                        msCompressed.Write(bData, 0, bData.Length);
                    }

                    bData = ms.ToArray();
                }
            }
                
            ListenerRequest.RequestId = CryptoServices.RandomIdToUInt64();
            ListenerRequest.SequenceCount = (Int16)((bData.Length / MessageRequestConstants.MAX_CHUNKSIZE) + ((bData.Length % MessageRequestConstants.MAX_CHUNKSIZE) > 0 ? 1 : 0));
            
            try
            {
                if (!MessageWriter.IsOpen())
                    MessageWriter.Open();

                try
                {
                    WriteRequest(MessageRequestType.Request, ListenerRequest);

                    // now send data in chunks if larger than 3 MB
                    Int16 nextSequence = 1;
                    Int32 atSource = 0;
                    Int32 remaining = bData.Length;
                    Int32 chunkSize = remaining < MessageRequestConstants.MAX_CHUNKSIZE ? remaining : MessageRequestConstants.MAX_CHUNKSIZE;

                    while (remaining > 0)
                    {
                        Byte[] bChunk = new Byte[chunkSize];
                        Array.Copy(bData, atSource, bChunk, 0, bChunk.Length);

                        atSource += chunkSize;
                        remaining -= chunkSize;
                        chunkSize = remaining < MessageRequestConstants.MAX_CHUNKSIZE ? remaining : MessageRequestConstants.MAX_CHUNKSIZE;

                        MessageSequence sequence = new MessageSequence();
                        sequence.SessionId = ListenerRequest.SessionId;
                        sequence.RequestId = ListenerRequest.RequestId;
                        sequence.Sequence = nextSequence++;
                        sequence.Chunk = bChunk;

                        WriteRequest(MessageRequestType.Sequence, sequence);
                    }
                }
                finally
                {
                    if (!MessageWriter.IsThreadSafe())
                        MessageWriter.Close();
                }
            }
            catch (Exception)
            {
                LastUnsuccessfulConnection = DateTime.Now;
                throw;
            }
        }

		public void Receive(ReflectInsightPackage[] messages)
		{
            ConstructAndSendMessages(messages);
		}
	}
}
