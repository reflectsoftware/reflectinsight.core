using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;

namespace ReflectSoftware.Insight.Common
{
    public class SafeNameValueCollection : NameValueCollection
    {
        public SafeNameValueCollection()
        {
        }

        public SafeNameValueCollection(SerializationInfo info, StreamingContext ctx) : base(info, ctx)
        {
        }

        public SafeNameValueCollection(Int32 capacity, NameValueCollection col) : base(capacity, col)
        {
        }

        public SafeNameValueCollection(Int32 capacity, IEqualityComparer equalityCompare) : base(capacity, equalityCompare)
        {
        }

        public SafeNameValueCollection(Int32 capacity) : base(capacity)
        {
        }

        public SafeNameValueCollection(IEqualityComparer equalityCompare) : base(equalityCompare)
        {
        }

        public SafeNameValueCollection(NameValueCollection nvCol) : base(nvCol)
        {
        }

        new public String this[String key]
        {
            get { return Get(key) ?? String.Empty; }
            set { Set(key, value); }
        }
    }
}
