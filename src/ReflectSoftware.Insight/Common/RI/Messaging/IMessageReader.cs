// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;


namespace RI.Messaging.ReadWriter
{
    public interface IMessageReader : IMessageReadWriterBase
    {        
        Byte[] Read();
        Byte[] Read(Int32 msecTimeout);
    }
}
