using System;
using System.Collections.Specialized;

namespace RI.Messaging.ReadWriter
{
    public static class ReadWriterFactory
    {

        public static T CreateInstance<T>(String name, params Object[] args) where T: IMessageReadWriterBase
        {
            MessageConfigImplementation implementation = ReadWriterConfiguration.GetImplementation(name);
            if (implementation == null)
            {
                throw new Exception(String.Format("Unable to obtain ReadWriterFactory configuration settings for implementation: '{0}'. Please check configuration file.", name));
            }

            Type impType = Type.GetType(implementation.ImplementationType);
            if (impType == null)
            {
                throw new TypeLoadException(String.Format("Unable to load implementation type '{0}' for ReadWriter: {1}.", implementation.ImplementationType, name));
            }

            return (T)Activator.CreateInstance(impType, args);
        }

        public static T CreateInstance<T>(NameValueCollection parameters) where T : IMessageReadWriterBase
        {
            var typeParam = parameters["type"];
            var impType = Type.GetType(typeParam);
            if (impType == null)
            {
                throw new TypeLoadException(String.Format("Unable to load implementation type '{0}' for ReadWriter: {1}.", typeParam, parameters["name"]));
            }

            return (T)Activator.CreateInstance(impType, parameters);
        }
    }
}
