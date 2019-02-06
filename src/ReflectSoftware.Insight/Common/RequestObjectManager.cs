using Plato.Security.Cryptography;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading;

namespace ReflectSoftware.Insight.Common
{
    public interface IRequestObject
    {
        void Attached(UInt32 requestId);
        void Detached();        
        void Reset();
        UInt32 RequestId { get; }
    }

    public static class CallContext
    {
        static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();
        public static void SetData(string name, object data) => state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;
        public static object GetData(string name) =>state.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : null;
        public static void FreeNamedDataSlot(string name) => state.TryRemove(name, out AsyncLocal<object> data);
    }

    public delegate T CreateRequestObjectHandler<T>();

    public class RequestObjectManager<T> where T : IRequestObject
    {                        
        [ThreadStatic]
        private static RequestObjectExtension ThreadRequestionExtension;        
        private static readonly String ThreadDataName;

        class RequestObjectExtension : IExtension<OperationContext>, IDisposable
        {
            public Boolean Disposed { get; private set; } 
            public UInt32 Key { get; internal set; }            
            public T RequestObject { get; set; }

            public RequestObjectExtension(UInt32 key)
            {
                Disposed = false;
                Key = key;
            }

            ~RequestObjectExtension()
            {
                Dispose(false);
            }

            public void Attach(OperationContext owner) { }

            public void Detach(OperationContext owner) { }


            protected void Dispose(Boolean bDisposing)
            {
                lock (this)
                {
                    if (!Disposed)
                    {
                        Disposed = true;
                        GC.SuppressFinalize(this);
                        RequestObject.Detached();                        
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        private static Object LockObject { get; set; }

        private static TimeSpan CollectWindow { get; set; }

        private static DateTime LastGCCollection { get; set; }


        static RequestObjectManager()
        {
            LockObject = new Object();
            LastGCCollection = DateTime.Now;
            CollectWindow = TimeSpan.FromMinutes(5);
            ThreadDataName = CryptoServices.RandomIdToUInt32().ToString();
        }

        static void GCCollection()
        {
            lock (LockObject)
            {
                if (DateTime.Now.Subtract(LastGCCollection) > CollectWindow)
                {                    
                    GC.Collect();                    
                    LastGCCollection = DateTime.Now;
                }
            }
        }
        
        private readonly UInt32 FId;        
        private readonly CreateRequestObjectHandler<T> DefaultCreateRequestObject;


        public RequestObjectManager(CreateRequestObjectHandler<T> defaultCreateRequestObject)
        {
            FId = CryptoServices.RandomIdToUInt32();
            DefaultCreateRequestObject = defaultCreateRequestObject;
        }

        public RequestObjectManager(): this(null)
        {
        }

        private static T CreateAndAttachRequestObject(CreateRequestObjectHandler<T> createRequestObject, UInt32 requestId)
        {
            T requestObject = createRequestObject(); 
            requestObject.Attached(requestId);
            requestObject.Reset();

            return requestObject;
        }

        public void RemoveRequest()
        {
            if (OperationContext.Current != null)
            {
                RequestObjectExtension requestExtention = OperationContext.Current.Extensions.Find<RequestObjectExtension>();
                if (requestExtention != null)
                {                    
                    OperationContext.Current.Extensions.Remove(requestExtention);
                    requestExtention.Dispose();
                }
            }
            else // we are bound to thread affinity 
            {                
                if(ThreadRequestionExtension != null)
                {
                    CallContext.FreeNamedDataSlot(ThreadDataName);
                    ThreadRequestionExtension.Dispose();
                    ThreadRequestionExtension = null;
                }
            }
        }

        public T GetRequestObject(CreateRequestObjectHandler<T> createRequestObject, out Boolean bNew)
        {
            GCCollection();
            
            RequestObjectExtension requestExtention = null;
            bNew = false;

            if (OperationContext.Current != null)
            {
                requestExtention = OperationContext.Current.Extensions.Find<RequestObjectExtension>();
                if (createRequestObject != null && requestExtention == null)
                {
                    bNew = true;
                    requestExtention = new RequestObjectExtension(CryptoServices.RandomIdToUInt32());
                    requestExtention.RequestObject = CreateAndAttachRequestObject(createRequestObject, requestExtention.Key);

                    OperationContext.Current.Extensions.Add(requestExtention);
                }
            }
            else // we are bound to thread affinity 
            {
                requestExtention = ThreadRequestionExtension;

                if (createRequestObject != null && (requestExtention == null || (Thread.CurrentThread.IsThreadPoolThread && CallContext.GetData(ThreadDataName) == null)))
                {
                    if (requestExtention != null)
                    {
                        requestExtention.Dispose();
                        ThreadRequestionExtension = null;
                    }


                    var threadId = (UInt32)Thread.CurrentThread.ManagedThreadId + ReflectInsightService.SessionId;

                    if (Thread.CurrentThread.IsThreadPoolThread)
                    {
                        CallContext.SetData(ThreadDataName, new Object());
                    }

                    bNew = true;
                    requestExtention = new RequestObjectExtension(threadId);
                    requestExtention.RequestObject = CreateAndAttachRequestObject(createRequestObject, requestExtention.Key);

                    // bind the request to the thread
                    ThreadRequestionExtension = requestExtention; 
                }
            }

            return requestExtention != null ? requestExtention.RequestObject : default(T);
        }

        public T GetRequestObject(CreateRequestObjectHandler<T> createRequestObject)
        {
            return GetRequestObject(createRequestObject, out bool bNew);
        }

        public T GetRequestObject(out Boolean bNew)
        {
            return GetRequestObject(DefaultCreateRequestObject, out bNew);
        }

        public T GetRequestObject()
        {
            return GetRequestObject(DefaultCreateRequestObject);
        }

        public void RemoveRequestObject()
        {
            RemoveRequest();
        }
    }
}
