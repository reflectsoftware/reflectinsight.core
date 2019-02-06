// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Runtime.Serialization;

namespace ReflectSoftware.Insight.Common
{
	[Serializable]
	public class ReflectInsightException: ApplicationException
	{
		public ReflectInsightException( String msg ): base( msg ) {}
		public ReflectInsightException( String msg, Exception innerException ): base( msg, innerException ) {}
		public ReflectInsightException( SerializationInfo info, StreamingContext context ): base( info, context ) {}
	}
}
