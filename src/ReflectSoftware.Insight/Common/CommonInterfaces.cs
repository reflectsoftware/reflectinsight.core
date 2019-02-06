// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

/// <summary>
/// ReflectSoftware.Insight.Common
/// </summary>
namespace ReflectSoftware.Insight.Common
{
    public delegate Boolean ReflectInsightMessageInterceptHandler(IReflectInsight ri, ReflectInsightPackage package);    

    public interface ITraceMethod : IDisposable
    {
        IReflectInsight RI { get; }
        String Message { get; }
        Boolean Disposed { get; }
        Boolean ExceptionHandler(Exception ex, Func<Exception, Boolean> handler);
        T Execute<T>(Func<T> action, TraceMethodExceptionPolicy policy = TraceMethodExceptionPolicy.LogAndSwallowParentsPolicy);
        void Execute(Action action, TraceMethodExceptionPolicy policy = TraceMethodExceptionPolicy.LogAndSwallowParentsPolicy);
    }

    public interface IReflectInsightDispatcher : IDisposable
    {
        Int32 DestinationBindingGroupId { get; set; }
        Boolean Disposed { get; }
        Boolean Enabled { get; set; }
        void SetDestinationBindingGroup(String destinationBindingGroup);
        void ClearDestinationBindingGroup();
        String GetDestinationBindingGroup();
    }

    public interface IReflectInsight : IReflectInsightDispatcher
    {
        event ReflectInsightMessageInterceptHandler OnReflectInsightMessageIntercept;

        Checkmark DefaultCheckmark { get; set; }
        Checkpoint DefaultCheckpoint { set; get; }

        ITraceMethod TraceMethod(String str, params Object[] args);
        ITraceMethod TraceMethod(MethodBase currentMethod, Boolean fullName = false);

        void Clear();
        void ViewerClearAll();
        void ViewerClearWatches();        
        void Send(MessageType mType, String str, String details, params object[] args);
        void Send(MessageType mType, String str, params object[] args);
        void ResetCheckpoint(Checkpoint cType);
        void ResetAllCheckpoints();
        void ResetCheckpoint();
        void ResetCheckpoint(String name, Checkpoint cType);
        void ResetCheckpoint(String name);
        void AddCheckpoint(Checkpoint cType);
        void AddCheckpoint();
        void AddCheckpoint(String name, Checkpoint cType);
        void AddCheckpoint(String name);
        void AddSeparator();
        void EnterMethod(String str, params Object[] args);
        void EnterMethod(MethodBase currentMethod, Boolean fullName = true);
        void ExitMethod(String str, params Object[] args);
        void ExitMethod(MethodBase currentMethod, Boolean fullName = true);
        void SendCustomData(String str, RICustomData cData, params object[] args);
        void SendMessage(String str, params Object[] args);        
        void SendComment(String str, params Object[] args);
        void SendNote(String str, params Object[] args);
        void SendInformation(String str, params Object[] args);
        void SendWarning(String str, params Object[] args);
        void SendError(String str, params Object[] args);
        void SendFatal(String str, params Object[] args);
        void SendFatal(String str, Exception excep, params object[] args);
        void SendDebug(String str, params Object[] args);
        void SendTrace(String str, params Object[] args);
        void SendStart(String str, params Object[] args);
        void SendStop(String str, params Object[] args);
        void SendSuspend(String str, params Object[] args);
        void SendResume(String str, params Object[] args);
        void SendTransfer(String str, params Object[] args);
        void SendVerbose(String str, params Object[] args);
        void SendAuditSuccess(String str, params Object[] args);
        void SendAuditFailure(String str, params Object[] args);
        void SendLevel(String str, LevelType level, params object[] args);
        void SendReminder(String str, params Object[] args);
        void SendStream(String str, Byte[] stream, params object[] args);
        void SendStream(String str, Stream stream, params object[] args);
        void SendStream(String str, String fileName, params object[] args);
        void SendMemory(String str, Byte[] stream, params object[] args);
        void SendMemory(String str, Stream stream, params object[] args);
        void SendMemory(String str, String fileName, params object[] args);
        void SendLoadedAssemblies(String str, params Object[] args);
        void SendLoadedAssemblies();
        void SendLoadedProcesses(String str, params Object[] args);
        void SendLoadedProcesses();
        void SendCollection(String str, IEnumerable enumerator, ObjectScope scope, params object[] args);
        void SendCollection(String str, IEnumerable enumerator, params object[] args);
        void SendTextFile(String str, String fileName, params object[] args);
        void SendTextFile(String str, Stream stream, params object[] args);
        void SendTextFile(String str, TextReader reader, params object[] args);
        void SendXML(String str, XmlNode node, params object[] args);
        void SendXMLFile(String str, String fileName, params object[] args);
        void SendXML(String str, Stream stream, params object[] args);
        void SendXML(String str, TextReader reader, params object[] args);
        void SendXML(String str, XmlReader reader, params object[] args);
        void SendXMLString(String str, String xmlString, params object[] args);
        void SendHTMLFile(String str, String fileName, params object[] args);
        void SendHTML(String str, Stream stream, params object[] args);
        void SendHTML(String str, TextReader reader, params object[] args);
        void SendHTMLString(String str, String htmlString, params object[] args);
        void SendJSON(String str, Object json, params object[] args);
        void SendJSON(String str, String json, params object[] args);
        void SendJSONFile(String str, String fileName, params object[] args);
        void SendJSON(String str, Stream stream, params object[] args);
        void SendJSON(String str, TextReader reader, params object[] args);
        void SendSQLString(String str, String sql, params object[] args);
        void SendSQLScript(String str, String fileName, params object[] args);
        void SendSQLScript(String str, Stream stream, params object[] args);
        void SendSQLScript(String str, TextReader reader, params object[] args);
        void SendGeneration(String str, Object obj, params object[] args);
        void SendGeneration(String str, WeakReference wRef, params object[] args);
        void SendObject(String str, Object obj, ObjectScope scope, params object[] args);
        void SendObject(String str, Object obj, params object[] args);
        void SendObject(String str, Object obj, Boolean bIgnoreStandard, params object[] args);        
        void ViewerSendWatch(String labelID, String str, params Object[] args);
        void ViewerSendWatch(String labelID, Object obj);
        void SendException(String str, Exception excep, params object[] args);
        void SendException(Exception excep);
        void SendCurrency(String str, Decimal? val, CultureInfo ci, params object[] args);
        void SendCurrency(String str, Decimal? val, params object[] args);
        void SendDateTime(String str, DateTime? dt, String format, CultureInfo ci, params object[] args);
        void SendDateTime(String str, DateTime? dt, String format, params object[] args);
        void SendDateTime(String str, DateTime? dt, CultureInfo ci, params object[] args);
        void SendDateTime(String str, DateTime? dt, params object[] args);
        void SendTimestamp(String str, String timeZoneId, params object[] args);
        void SendTimestamp(String str, TimeZoneInfo tz, params object[] args);
        void SendTimestamp(TimeZoneInfo tz);
        void SendTimestamp(String str, params object[] args);
        void SendTimestamp();
        void SendPoint(String str, Point pt, params object[] args);
        void SendRectangle(String str, Rectangle rect, params object[] args);
        void SendSize(String str, Size sz, params object[] args);
        void SendColor(String str, Color clrObj, params object[] args);
        void SendCheckmark(String str, Checkmark cmType, params object[] args);
        void SendCheckmark(String str, params Object[] args);
        void SendStackTrace(String str, params Object[] args);
        void SendStackTrace();
        void SendProcessInformation(Process aProcess);
        void SendProcessInformation();
        void SendDataSet(String str, DataSet dSet, params object[] args);
        void SendDataSet(DataSet dSet);
        void SendDataSetSchema(String str, DataSet dSet, params object[] args);
        void SendDataSetSchema(DataSet dSet);
        void SendDataTable(String str, DataTable table, params object[] args);
        void SendDataTable(DataTable table);
        void SendDataTableSchema(String str, DataTable table, params object[] args);
        void SendDataTableSchema(DataTable table);
        void SendDataView(String str, DataView view, params object[] args);
        void SendDataView(DataView view);
        void SendTypedCollection<T>(String str, params IEnumerable<T>[] enumerables);
        void SendTypedCollection<T>(IEnumerable<T> enumerable);
        void SendAttachment(String str, String fileName, params object[] args);
        void SendString(String str, String theString, params object[] args);
        void SendString(String str, StringBuilder theString, params object[] args);
        void PurgeLogFile();
        Boolean SendAssert(Boolean condition, String str, params Object[] args);
        Boolean SendAssigned(String str, Object obj, params object[] args);
        String Category { get; set; }
        Color BackColor { set; get; }
    }    
    
    public interface IListenerInfo
    {
        String Name { get; }
        String Details { get; }
        SafeNameValueCollection Params { get; }
    }

	public interface IReflectInsightListener
	{
        void Receive(ReflectInsightPackage[] messages);
        void UpdateParameterVariables(IListenerInfo listener);
	}
}


