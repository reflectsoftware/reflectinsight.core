// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;

/// <summary>
/// ReflectSoftware.Insight.Common 
/// </summary>
namespace ReflectSoftware.Insight.Common
{
    /// <summary>
    /// 
    /// </summary>
    public enum MessageSendType
    {
        /// <summary>
        /// Message
        /// </summary>
        Message = 0,
        /// <summary>
        /// Watch
        /// </summary>
        Watch = 1
    }

    /// <summary>
    /// 
    /// </summary>
    public enum MessageType
    {        
        // start of simple types        
        /// <summary>
        /// Clear
        /// </summary>
        Clear = 0,
        /// <summary>
        /// Add a separator
        /// </summary>
        AddSeparator = 1,
        /// <summary>
        /// Add a checkpoint
        /// </summary>
        AddCheckpoint = 2,
        /// <summary>
        /// Enter a method
        /// </summary>
        EnterMethod = 3,
        /// <summary>
        /// Exit a method
        /// </summary>
        ExitMethod = 4,
        /// <summary>
        /// Send a messageType
        /// </summary>
        SendMessage = 5,
        /// <summary>
        /// Send a note
        /// </summary>
        SendNote = 6,
        /// <summary>
        /// Send information
        /// </summary>
        SendInformation = 7,
        /// <summary>
        /// Send a warning
        /// </summary>
        SendWarning = 8,
        /// <summary>
        /// Send an error
        /// </summary>
        SendError = 9,
        /// <summary>
        /// Send fatal
        /// </summary>
        SendFatal = 10,
        /// <summary>
        /// Send debug
        /// </summary>
        SendDebug = 11,
        /// <summary>
        /// Send a level
        /// </summary>
        SendLevel = 12,
        /// <summary>
        /// Send a reminder
        /// </summary>
        SendReminder = 13,
        /// <summary>
        /// Send a text file
        /// </summary>
        SendTextFile = 14,
        /// <summary>
        /// Send XML
        /// </summary>
        SendXML = 15,
        /// <summary>
        /// Send HTML
        /// </summary>
        SendHTML = 16,
        /// <summary>
        /// Send SQL
        /// </summary>
        SendSQL = 17,
        /// <summary>
        /// Send a generation
        /// </summary>
        SendGeneration = 18,
        /// <summary>
        /// Send a serialized object
        /// </summary>
        SendSerializedObject = 19,
        /// <summary>
        /// Send an exception
        /// </summary>
        SendException = 20,
        /// <summary>
        /// Send a Date/Time
        /// </summary>
        SendDateTime = 21,
        /// <summary>
        /// Send a Time stamp
        /// </summary>
        SendTimestamp = 22,
        /// <summary>
        /// Send currency
        /// </summary>
        SendCurrency = 23,
        /// <summary>
        /// Send a point structure
        /// </summary>
        SendPoint = 24,
        /// <summary>
        /// Send a rectangle
        /// </summary>
        SendRectangle = 25,
        /// <summary>
        /// Send a size structure
        /// </summary>
        SendSize = 26,
        /// <summary>
        /// Send a Check Mark
        /// </summary>
        SendCheckmark = 27,
        /// <summary>
        /// Send an assert
        /// </summary>
        SendAssert = 28,
        /// <summary>
        /// Send an indication that a variable has been assigned
        /// </summary>
        SendAssigned = 29,
        /// <summary>
        /// Send a stack trace
        /// </summary>
        SendStackTrace = 30,
        /// <summary>
        /// Send audit success
        /// </summary>
        SendAuditSuccess = 31,
        /// <summary>
        /// Send audit failure
        /// </summary>
        SendAuditFailure = 32,
        /// <summary>
        /// Send an internal error
        /// </summary>
        SendInternalError = 33,
        /// <summary>
        /// Send a comment
        /// </summary>
        SendComment = 34,
        /// <summary>
        /// Send an enumeration
        /// </summary>
        SendEnum = 35,
        /// <summary>
        /// Send a boolean
        /// </summary>
        SendBoolean = 36,
        /// <summary>
        /// Send a byte
        /// </summary>
        SendByte = 37,
        /// <summary>
        /// Send a character
        /// </summary>
        SendChar = 38,
        /// <summary>
        /// Send a decimal
        /// </summary>
        SendDecimal = 39,
        /// <summary>
        /// Send a double
        /// </summary>
        SendDouble = 40,
        /// <summary>
        /// Send a single
        /// </summary>
        SendSingle = 41,
        /// <summary>
        /// Send an integer
        /// </summary>
        SendInteger = 42,
        /// <summary>
        /// Send a string
        /// </summary>
        SendString = 43,
        /// <summary>
        /// Send a LINQ query
        /// </summary>
        SendLinqQuery = 44,
        /// <summary>
        /// Send a trace
        /// </summary>
        SendTrace = 45,
        /// <summary>
        /// An unknown message type
        /// </summary>
        Unknown = 46,
        /// <summary>
        /// Send a start message
        /// </summary>
        SendStart = 47,
        /// <summary>
        /// Send a stop message
        /// </summary>
        SendStop = 48,
        /// <summary>
        /// Send a suspend message
        /// </summary>
        SendSuspend = 49,
        /// <summary>
        /// Send a resume message
        /// </summary>
        SendResume = 50,
        /// <summary>
        /// Send a transfer message
        /// </summary>
        SendTransfer = 51,
        /// <summary>
        /// Send a verbose message
        /// </summary>
        SendVerbose = 52,
        /// <summary>
        /// Send a mini dump file
        /// </summary>
        SendMiniDumpFile = 53,
        /// <summary>
        /// Send a typed JSON
        /// </summary>
        SendJSON = 54,                
        // start of complex types
        /// <summary>
        /// Send an image
        /// </summary>
        SendImage = 1000,
        /// <summary>
        /// Send a stream
        /// </summary>
        SendStream = 1001,
        /// <summary>
        /// Send a memory dump
        /// </summary>
        SendMemory = 1002,
        /// <summary>
        /// Send memory status
        /// </summary>
        SendMemoryStatus = 1003,
        /// <summary>
        /// Send an object
        /// </summary>
        SendObject = 1004,
        /// <summary>
        /// Send color
        /// </summary>
        SendColor = 1005,
        /// <summary>
        /// Send an attachment
        /// </summary>
        SendAttachment = 1006,
        /// <summary>
        /// Send loaded assemblies
        /// </summary>
        SendLoadedAssemblies = 1007,
        /// <summary>
        /// Send a collection
        /// </summary>
        SendCollection = 1008,
        /// <summary>
        /// Send process information
        /// </summary>
        SendProcessInformation = 1009,
        /// <summary>
        /// Send the application domain information
        /// </summary>
        SendAppDomainInformation = 1010,
        /// <summary>
        /// Send thread information
        /// </summary>
        SendThreadInformation = 1011,
        /// <summary>
        /// Send system information
        /// </summary>
        SendSystemInformation = 1012,
        /// <summary>
        /// Send custom data
        /// </summary>
        SendCustomData = 1013,
        /// <summary>
        /// Send a data set
        /// </summary>
        SendDataSet = 1014,
        /// <summary>
        /// Send a data table
        /// </summary>
        SendDataTable = 1015,
        /// <summary>
        /// Send a data view
        /// </summary>
        SendDataView = 1016,
        /// <summary>
        /// Send  data set schema
        /// </summary>
        SendDataSetSchema = 1017,
        /// <summary>
        /// Send a data table schema
        /// </summary>
        SendDataTableSchema = 1018,
        /// <summary>
        /// Send HTTP module information
        /// </summary>
        SendHttpModuleInformation = 1019,
        /// <summary>
        /// Send LINQ results
        /// </summary>
        SendLinqResults = 1020,
        /// <summary>
        /// Send loaded processes
        /// </summary>
        SendLoadedProcesses = 1021,
        /// <summary>
        /// Send am image of the current desktop
        /// </summary>
        SendDesktopImage = 1022,
        /// <summary>
        /// Send a typed collection
        /// </summary>
        SendTypedCollection = 1023,
        /// <summary>
        /// Send a HTTP Request
        /// </summary>
        SendHttpRequest = 1024,                
                
        // Start of Viewer types
        /// <summary>
        /// Clear everything in the viewer
        /// </summary>
        ViewerClearAll = 2000,
        /// <summary>
        /// Clear all watches in the Viewer
        /// </summary>
        ViewerClearWatches = 2001,
        /// <summary>
        /// Send a watch to the viewer
        /// </summary>
        ViewerSendWatch = 2002,
        
        /// <summary>
        /// The purge log file
        /// </summary>
        PurgeLogFile = 2500,
        
        //// Data Analytics range will be 3000 - 3300

        // General data supporting up to 5 dimensions
        /// <summary>
        /// Send 2 dimension data analytics data
        /// </summary>
        SendData_XY = 3000,
        /// <summary>
        /// Send up to 5 dimension data analytics data
        /// </summary>
        SendData_XYZ = 3001,

        // Financial Data
        /// <summary>
        /// Send stock prices
        /// </summary>
        SendData_StockPrices = 3100,

        // Geographical (Surface) Data (can be also up to 5 dimensions.
        /// <summary>
        /// Send up to 5 dimension geographical surface data
        /// </summary>
        SendData_Geo = 3200,

        /// <summary>
        /// The end
        /// </summary>
        _End = 3301
    }

    /// <summary>
    /// 
    /// </summary>
    public enum LevelType
    {
        /// <summary>
        /// Red
        /// </summary>
        Red = 1,
        /// <summary>
        /// Orange
        /// </summary>
        Orange = 2,
        /// <summary>
        /// Yellow
        /// </summary>
        Yellow = 3,
        /// <summary>
        /// Green
        /// </summary>
        Green = 4,
        /// <summary>
        /// Blue
        /// </summary>
        Blue = 5,
        /// <summary>
        /// Cyan
        /// </summary>
        Cyan = 6,
        /// <summary>
        /// Purple
        /// </summary>
        Purple = 7,
        /// <summary>
        /// Magenta
        /// </summary>
        Magenta = 8,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum Checkpoint
    {
        /// <summary>
        /// Red
        /// </summary>
        Red = 1,
        /// <summary>
        /// Orange
        /// </summary>
        Orange = 2,
        /// <summary>
        /// Yellow
        /// </summary>
        Yellow = 3,
        /// <summary>
        /// Green
        /// </summary>
        Green = 4,
        /// <summary>
        /// Blue
        /// </summary>
        Blue = 5,
        /// <summary>
        /// Purple
        /// </summary>
        Purple = 6,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum Checkmark
    {
        /// <summary>
        /// Red
        /// </summary>
        Red = 1,
        /// <summary>
        /// Orange
        /// </summary>
        Orange = 2,
        /// <summary>
        /// Yellow
        /// </summary>
        Yellow = 3,
        /// <summary>
        /// Green
        /// </summary>
        Green = 4,
        /// <summary>
        /// Blue
        /// </summary>
        Blue = 5,
        /// <summary>
        /// Purple
        /// </summary>
        Purple = 6,
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum ObjectScope
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0x00,
        /// <summary>
        /// Public
        /// </summary>
        Public = 0x01,
        /// <summary>
        /// Protected
        /// </summary>
        Protected = 0x02,
        /// <summary>
        /// Private
        /// </summary>
        Private = 0x04,
        /// <summary>
        /// All
        /// </summary>
        All = Public | Protected | Private
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum MethodDisplayFlags
    {
        /// <summary>
        /// The method name
        /// </summary>
        MethodName = 0x01,
        /// <summary>
        /// The parameters
        /// </summary>
        Parameters = 0x03,
        /// <summary>
        /// The hashed parameters
        /// </summary>
        HashedParameters = 0x07
    }

    /// <summary>
    /// TraceMethod Action's Exception Policies
    /// </summary>
    /// <remarks>
    /// Regardless of policy, the exception will always propagate (bubble) to the parent calling method. 
    /// It's just a matter where they get logged or not.
    /// </remarks>    
    public enum TraceMethodExceptionPolicy
    {
        /// <summary>Don't log the exception and allow the execution of the parent's exception policy.</summary>
        Ignore,

        /// <summary>Log the exception and allow the execution of the parent's exception policy.</summary>
        LogAndAllowParentsPolicy,

        /// <summary>Log the exception but don't allow the execution of the parent's exception policy.</summary>
        LogAndSwallowParentsPolicy,
    }

}
