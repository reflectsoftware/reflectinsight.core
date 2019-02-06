// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Extensions;
using ReflectSoftware.Insight.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ReflectSoftware.Insight
{
    internal class CheckpointSetContainer
    {
        public Int32 CheckpointRed { get; set; }
        public Int32 CheckpointOrange { get; set; }
        public Int32 CheckpointYellow { get; set; }
        public Int32 CheckpointGreen { get; set; }
        public Int32 CheckpointBlue { get; set; }
        public Int32 CheckpointPurple { get; set; }
        
        public CheckpointSetContainer()
        {
            ResetAll();
        }
        
        public void ResetAll()
        {
            CheckpointRed = 0;
            CheckpointOrange = 0;
            CheckpointYellow = 0;
            CheckpointGreen = 0;
            CheckpointBlue = 0;
            CheckpointPurple = 0;
        }
        
        public void ResetCheckpoint(Checkpoint cType)
        {
            switch (cType)
            {
                case Checkpoint.Red: CheckpointRed = 0; break;
                case Checkpoint.Orange: CheckpointOrange = 0; break;
                case Checkpoint.Yellow: CheckpointYellow = 0; break;
                case Checkpoint.Green: CheckpointGreen = 0; break;
                case Checkpoint.Blue: CheckpointBlue = 0; break;
                case Checkpoint.Purple: CheckpointPurple = 0; break;
            }
        }
        
        public Int32 GetNextCheckpoint(Checkpoint cType)
        {
            switch (cType)
            {
                case Checkpoint.Red: return ++CheckpointRed;
                case Checkpoint.Orange: return ++CheckpointOrange;
                case Checkpoint.Yellow: return ++CheckpointYellow;
                case Checkpoint.Green: return ++CheckpointGreen;
                case Checkpoint.Blue: return ++CheckpointBlue;
                case Checkpoint.Purple: return ++CheckpointPurple;
            }

            return 0;
        }
    }
    
    internal class ControlValues : IRequestObject
    {
        public UInt32 ThreadId { get; set; }
        public IndentValue IndentValue { get; set; }
        public CheckpointSetContainer CheckpointSet;
        public Checkpoint DefaultCheckpoint { get; set; }
        public Dictionary<String, CheckpointSetContainer> NamedCheckpoints { get; set; }

        public UInt32 RequestId { get; private set; }
        public Dictionary<String, Object> States { get; set; }
        public MessagePropertyContainer SingleMessageProperties { get; set; }
        public MessagePropertyContainer RequestMessageProperties { get; set; }

        public Boolean ShouldClear { get; set; }
        public SendPack SendPack { get; set; }

        
        public void Attached(UInt32 requestId)
        {
            RequestId = requestId;
            ThreadId = (UInt32)Thread.CurrentThread.ManagedThreadId + ReflectInsightService.SessionId;
            SendPack = new SendPack();
            IndentValue = new IndentValue();
            CheckpointSet = new CheckpointSetContainer();
            NamedCheckpoints = new Dictionary<String, CheckpointSetContainer>();
            RequestMessageProperties = new MessagePropertyContainer();
            States = new Dictionary<String, Object>();

            Reset();
        }
        
        public void Release()
        {
            SendPack.Release();
        }
        
        public void Detached()
        {
            Release();
            Reset();
        }
        
        public void Reset()
        {
            ResetStates();
            ResetSingleRequestProperties();
            Clear();

            DefaultCheckpoint = Checkpoint.Red;
        }
        
        public void ResetStates()
        {
            foreach (Object obj in States.Values)
            {
                obj.DisposeObject();
            }

            States.Clear();
        }
        
        public void ResetSingleRequestProperties()
        {
            SingleMessageProperties = new MessagePropertyContainer();
        }
        
        public void Clear()
        {
            ShouldClear = false;
            ResetAllCheckpoints();
            IndentValue.Reset();

            SingleMessageProperties.Clear();
            RequestMessageProperties.Clear();
        }
        
        public void AddState(String key, Object state)
        {
            States[key] = state;
        }
        
        public void RemoveState(String key, Boolean bDispose)
        {
            if (States.ContainsKey(key))
            {
                if (bDispose)
                {
                    States[key].DisposeObject();
                }

                States.Remove(key);
            }
        }
        
        public void RemoveState(String key)
        {
            RemoveState(key, false);
        }
        
        public T GetState<T>(String key)
        {
            return States.ContainsKey(key) ? (T)States[key] : default(T);
        }
        
        public Int32 GetNextCheckpoint(Checkpoint cType)
        {
            return CheckpointSet.GetNextCheckpoint(cType);
        }
        
        public Int32 GetNextCheckpoint(String name, Checkpoint cType)
        {
            CheckpointSetContainer set = null;
            if(NamedCheckpoints.ContainsKey(name))
            {
                set = NamedCheckpoints[name];
            }
            else
            {
                set = new CheckpointSetContainer();
                NamedCheckpoints.Add(name, set);
            }
            
            return set.GetNextCheckpoint(cType);
        }
        
        public void ResetAllCheckpoints()
        {
            CheckpointSet.ResetAll();
            NamedCheckpoints.Clear();
        }
        
        public void ResetCheckpoint(Checkpoint cType)
        {
            CheckpointSet.ResetCheckpoint(cType);
        }
        
        public void ResetCheckpoint(String name, Checkpoint cType)
        {
            if (!NamedCheckpoints.ContainsKey(name))
                return;

            NamedCheckpoints[name].ResetCheckpoint(cType);
        }
    }

    internal class ControlValuesContainer : IDisposable
    {
        private readonly ControlValues FControlValues;

        public Boolean Disposed { get; private set; }
        
        public ControlValuesContainer(ControlValues values)
        {
            Disposed = false;
            FControlValues = values;
        }
        
        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);

                    if (FControlValues.ShouldClear)
                        FControlValues.Clear();

                    FControlValues.Release();
                }
            }
        }
    }

    static internal class RequestManager
    {
        private readonly static RequestObjectManager<ControlValues> FRequestObjectManager;
        
        static RequestManager()
        {
            FRequestObjectManager = new RequestObjectManager<ControlValues>(() => new ControlValues());
        }
        
        static public ControlValues GetRequestObject()
        {
            return FRequestObjectManager.GetRequestObject();
        }
    }
}
