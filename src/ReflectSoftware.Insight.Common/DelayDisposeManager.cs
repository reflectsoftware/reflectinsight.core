using Plato.Extensions;
using System;
using System.Collections.Generic;

namespace ReflectSoftware.Insight.Common
{
    public interface IDelayDisposable
    {        
        void DelayDispose();
    }
    
    static public class DelayDisposeManager
    {        
        private const Int32 DISPOSE_OBJECT_TIME = 1;

        private class DelayDisposerInfo : IDisposable
        {
            public Boolean Disposed { get; private set; }
            public IDelayDisposable DisposableObject;
            public TimeSpan DisposeWindow;
            public DateTime ReceivedDateTime;

            public DelayDisposerInfo(IDelayDisposable disposeObject, TimeSpan timeWindow)
            {
                Disposed = false;
                DisposableObject = disposeObject;
                DisposeWindow = timeWindow;
                ReceivedDateTime = DateTime.Now;
            }

            public void Dispose()
            {
                lock (this)
                {
                    if (!Disposed)
                    {
                        Disposed = true;
                        GC.SuppressFinalize(this);

                        if (DisposableObject != null)
                        {
                            DisposableObject.DelayDispose();
                            DisposableObject = null;
                        }
                    }
                }
            }
        }

        private class DelayDisposeContainer : IDelayDisposable
        {
            private Object DisposableObject { get; set; }
            public Boolean DelayDisposed { get; private set; }

            public DelayDisposeContainer(Object obj)
            {
                DisposableObject = obj;
                DelayDisposed = false;
            }

            void IDelayDisposable.DelayDispose()
            {
                lock (this)
                {
                    if (!DelayDisposed)
                    {
                        DelayDisposed = true;
                        GC.SuppressFinalize(this);

                        if (DisposableObject != null)
                        {
                            DisposableObject.DisposeObject();
                            DisposableObject = null;
                        }
                    }
                }
            }
        }
        
        private readonly static Object LockObject;        
        private readonly static List<DelayDisposerInfo> DisposableObjects;        
        private static Int32 ReferenceCount;

        private static TimeSpan LastDisposableScanWindow { get; set; }
        private static DateTime LastDisposableCheck { get; set; }

        static DelayDisposeManager()
        {
            LockObject = new Object();
            ReferenceCount = 0;
            DisposableObjects = new List<DelayDisposerInfo>();
            LastDisposableScanWindow = new TimeSpan(0, DISPOSE_OBJECT_TIME, 0);
            LastDisposableCheck = DateTime.Now;
        }

        static public void OnStartup()
        {
            lock (LockObject)
            {
                ReferenceCount++;
            }
        }

        static public void OnShutdown()
        {
            lock (LockObject)
            {
                ReferenceCount--;
                if (ReferenceCount <= 0)
                {
                    _ForceDispose();
                    ReferenceCount = 0;
                }
            }
        }

        static private void _ForceDispose()
        {
            lock (DisposableObjects)
            {
                foreach (DelayDisposerInfo dInfo in DisposableObjects)
                    dInfo.Dispose();

                DisposableObjects.Clear();
                DisposableObjects.Capacity = 0;
            }
        }

        static private void CheckDisposableObjectsIfNecessary()
        {
            lock (DisposableObjects)
            {
                if (DateTime.Now.Subtract(LastDisposableCheck) > LastDisposableScanWindow)
                {
                    foreach (DelayDisposerInfo dInfo in DisposableObjects.ToArray())
                    {
                        if (DateTime.Now.Subtract(dInfo.ReceivedDateTime) > dInfo.DisposeWindow)
                        {
                            DisposableObjects.Remove(dInfo);
                            dInfo.Dispose();
                        }
                    }

                    LastDisposableCheck = DateTime.Now;
                }
            }
        }

        static public void ForceDispose()
        {
            _ForceDispose();
        }

        static public void PurgeExpiredObjects()
        {
            CheckDisposableObjectsIfNecessary();
        }

        static public void Add(IDelayDisposable obj, TimeSpan timeWindow)
        {
            PurgeExpiredObjects();

            if (obj == null)
                return;

            lock (DisposableObjects)
            {
                DisposableObjects.Add(new DelayDisposerInfo(obj, timeWindow));
            }
        }

        static public void Add(IDelayDisposable obj, Int32 msecTimeWindow)
        {
            if (obj == null)
                return;

            Add(obj, new TimeSpan(0, 0, 0, 0, msecTimeWindow));
        }

        static public void Add(IDisposable obj, TimeSpan timeWindow)
        {
            if (obj != null)
                Add(new DelayDisposeContainer(obj), timeWindow);
        }

        static public void Add(IDisposable obj, Int32 msecTimeWindow)
        {
            if (obj != null)
                Add(new DelayDisposeContainer(obj), msecTimeWindow);
        }
    }
}
