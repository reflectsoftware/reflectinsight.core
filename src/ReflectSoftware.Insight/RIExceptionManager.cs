// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Extensions;
using Plato.Interfaces;
using Plato.Miscellaneous;
using Plato.Strings;
using System;
using System.Collections.Specialized;

namespace ReflectSoftware.Insight
{
    /// <summary>
    /// 
    /// </summary>
    static public class RIExceptionManager
    {
        static private TimeSpan _eventTracking;
        static private ILogTextFileWriterFactory _logTextFileWriterFactory;
        static private ILogTextFileWriter _logTextFileWriter;

        /// <summary>
        /// Initializes the <see cref="RIExceptionManager"/> class.
        /// </summary>
        static RIExceptionManager()
        {
            _logTextFileWriterFactory = new LogTextFileWriterFactory();
            ReflectInsightService.Initialize();
        }

        /// <summary>
        /// Called when [startup].
        /// </summary>
        static internal void OnStartup()
        {
            OnConfigFileChange();
        }

        /// <summary>
        /// Called when [configuration file change].
        /// </summary>
        static internal void OnConfigFileChange()
        {
            _eventTracking = new TimeSpan(0, ReflectInsightConfig.Settings.GetExceptionEventTracker(20), 0);

            var mode = ReflectInsightConfig.Settings.GetExceptionManagerAttribute("mode", "off");
            var filePath = ReflectInsightConfig.Settings.GetExceptionManagerAttribute("filePath", @"$(workingdir)\RI.Exceptions.txt");
            var recycle = ReflectInsightConfig.Settings.GetExceptionManagerAttribute("recycle", "7");

            var oldWriter = _logTextFileWriter;

            if (mode == "on")
            {
                if (int.TryParse(recycle, out int irecycle) == false)
                {
                    irecycle = 7;
                }

                _logTextFileWriter = _logTextFileWriterFactory.Create(filePath, irecycle, true);
            }
            else
            {
                _logTextFileWriter = new LogNullFileWriter();
            }

            oldWriter?.Dispose();
        }

        /// <summary>
        /// Called when [shutdown].
        /// </summary>
        static internal void OnShutdown()
        {
            _logTextFileWriter?.Dispose();
            _logTextFileWriter = null;
        }

        /// <summary>
        /// Determines whether this instance can event the specified ex.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>
        ///   <c>true</c> if this instance can event the specified ex; otherwise, <c>false</c>.
        /// </returns>
        static public bool CanEvent(Exception ex)
        {
            return TimeEventTracker.CanEvent((int)ex.Message.BKDRHash(), _eventTracking);
        }

        /// <summary>
        /// Publishes the specified ex.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="additionalParameters">The additional parameters.</param>
        static public void Publish(Exception ex, NameValueCollection additionalParameters)
        {
            try
            {
                // add time stamps
                var now = DateTime.UtcNow;
                additionalParameters = additionalParameters ?? new NameValueCollection();
                additionalParameters.Add("Local Time", now.ToString("yyyy/MM/dd, HH:mm:ss.fff"));
                additionalParameters.Add("UTC", now.ToUniversalTime().ToString("yyyy/MM/dd, HH:mm:ss.fff"));

                if (!TimeEventTracker.CanEvent(ex.Message, _eventTracking, out int occurrences))
                {
                    return;
                }


                additionalParameters = additionalParameters ?? new NameValueCollection();

                if (occurrences != 0)
                {
                    additionalParameters = additionalParameters ?? new NameValueCollection();
                    additionalParameters["Occurrences"] = occurrences.ToString("N0");
                }

                var message = ExceptionFormatter.ConstructMessage(ex, additionalParameters);
                _logTextFileWriter.WriteLine(message);

            }
            catch (Exception)
            {
                // nothing we can do just swallow
            }
        }

        /// <summary>
        /// Publishes the specified ex.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="additionalInfo">The additional information.</param>
        static public void Publish(Exception ex, string additionalInfo)
        {
            Publish(ex, new NameValueCollection
            {
                { "Additional Info", additionalInfo }
            });
        }

        /// <summary>
        /// Publishes the specified ex.
        /// </summary>
        /// <param name="ex">The ex.</param>
        static public void Publish(Exception ex)
        {
            Publish(ex, new NameValueCollection());
        }

        /// <summary>
        /// Publishes if evented.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="additionalParameters">The additional parameters.</param>
        static public void PublishIfEvented(Exception ex, NameValueCollection additionalParameters)
        {
            if (CanEvent(ex))
            {
                Publish(ex, additionalParameters);
            }
        }

        /// <summary>
        /// Publishes if evented.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="additionalInfo">The additional information.</param>
        static public void PublishIfEvented(Exception ex, string additionalInfo)
        {
            if (CanEvent(ex))
            {
                Publish(ex, additionalInfo);
            }
        }

        /// <summary>
        /// Publishes if evented.
        /// </summary>
        /// <param name="ex">The ex.</param>
        static public void PublishIfEvented(Exception ex)
        {
            if (CanEvent(ex))
            {
                Publish(ex);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Plato.Interfaces.ILogTextFileWriter" />
    internal class LogNullFileWriter : ILogTextFileWriter
    {
        public string LogFilePath => string.Empty;
        public bool CreateDirectory => false;
        public int RecycleNumber => 0;
        public bool Disposed => true;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {            
        }

        /// <summary>
        /// Writes the specified MSG.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public void Write(string msg)
        {            
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public void WriteLine(string msg)
        {            
        }
    }
}
