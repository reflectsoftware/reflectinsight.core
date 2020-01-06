// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;

namespace RI.Messaging.ReadWriter
{
    public interface IMessageReadWriterBase: IDisposable
    {
        Boolean IsThreadSafe();
        Boolean IsOpen();
        void Open();
        void Close();
    }
}
