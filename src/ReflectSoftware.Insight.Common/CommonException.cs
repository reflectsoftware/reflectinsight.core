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
