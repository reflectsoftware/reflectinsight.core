// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Serializers.FormatterPools;
using ReflectSoftware.Insight.Common;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ReflectSoftware.Insight
{
    internal class DataSetTypeMapProperty
    {
        public Type DataType { get; set; }
        public PropertyInfo Property { get; set; }
    }

    internal class DataSetTypeMap
    {        
        public Type ElementType { get; set; }
        public List<DataSetTypeMapProperty> Properties { get; set; }
    }
            
    static internal class SimpleAPIHelper
	{
        private readonly static string FLine;
        private readonly static Dictionary<Type, DataSetTypeMap> DataSetTypeMaps;
				
		static SimpleAPIHelper()
		{
			FLine = string.Format("{0,40}", string.Empty).Replace(" ", "-");
            DataSetTypeMaps = new Dictionary<Type, DataSetTypeMap>();
		}	
		
		static public string GetIdentedCallStack(List<string> ignoreUptoAnyLine)
		{
			Int32 highestIgnore = -1;
			StringBuilder sbFrames = new StringBuilder();
			string[] frames = Environment.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			
			for (Int32 i = 0; i < frames.Length; i++)
			{
				foreach (string ignore in ignoreUptoAnyLine)
				{
					if (frames[i].Contains(ignore))
					{
						if (i > highestIgnore)
							highestIgnore = i;

						ignoreUptoAnyLine.Remove(ignore);
						break;
					}
				}
			}

			sbFrames.AppendLine("\tCall Stack Details:");
			sbFrames.AppendFormat("\t{0}{1}", FLine, Environment.NewLine);

			highestIgnore++;
			for (Int32 i = highestIgnore; i < frames.Length; i++)
			{
				sbFrames.AppendFormat("\t{0}{1}", frames[i], Environment.NewLine);
			}

			return sbFrames.ToString();
		}
        
        static public string GetCallStack(List<string> ignoreUptoAnyLine)
        {
            return GetIdentedCallStack(ignoreUptoAnyLine).Replace("\t", string.Empty);
        }
		
		static public RICustomData GetThreadInformation(Thread aThread)
		{
            List<RICustomDataColumn> columns = new List<RICustomDataColumn>
            {
                new RICustomDataColumn("Property"),
                new RICustomDataColumn("Value")
            };

            RICustomData cData = new RICustomData( "Thread Information", columns, false, true);

			cData.AddRow("Name", aThread.Name);
			cData.AddRow("HashCode", aThread.GetHashCode().ToString());
			cData.AddRow("IsAlive", aThread.IsAlive.ToString());
			cData.AddRow("IsBackground", aThread.IsBackground.ToString());
			cData.AddRow("IsThreadPoolThread", aThread.IsThreadPoolThread.ToString());
			cData.AddRow("Priority", aThread.Priority.ToString());
			cData.AddRow("ThreadState", aThread.ThreadState.ToString());
			cData.AddRow("ApartmentState", aThread.GetApartmentState().ToString());
			cData.AddRow("CurrentCulture", aThread.CurrentCulture.ToString());
			cData.AddRow("CurrentUICulture", aThread.CurrentUICulture.ToString());
			
			return cData;
		}
        
        static private RICustomData _GetProcessInformation(RICustomData cData, Process process)
        {
            RICustomDataCategory cat = cData.AddCategory("Process Information");
            cat.AddRow("Process Id", process.Id.ToString());
            cat.AddRow("Process Name", process.ProcessName);
            cat.AddRow("Image", process.MainModule.FileName);
            cat.AddRow("Machine Name", process.MachineName == "." ? Environment.MachineName : process.MachineName);
            cat.AddRow("Main Window Handle", process.MainWindowHandle != null ? process.MainWindowHandle.ToString() : null);
            cat.AddRow("Main Window Title", process.MainWindowTitle);
            cat.AddRow("Handle", process.Handle != null ? process.Handle.ToString() : null);
            cat.AddRow("Open Handles", process.HandleCount.ToString());
            cat.AddRow("Number of Modules", process.Modules.Count.ToString());

            cat = cData.AddCategory("Priority Information");
            cat.AddRow("Base Priority", process.BasePriority.ToString());
            cat.AddRow("Priority", process.PriorityClass.ToString());
            cat.AddRow("Priority Boost", process.PriorityBoostEnabled.ToString());

            cat = cData.AddCategory("Memory Information");
            cat.AddRow("Non-Paged System Memory", string.Format("{0} KB", (process.NonpagedSystemMemorySize64 / 1024).ToString("N0")));
            cat.AddRow("Paged System Memory", string.Format("{0} KB", (process.PagedSystemMemorySize64 / 1024).ToString("N0")));
            cat.AddRow("Paged Memory", string.Format("{0} KB", (process.PagedMemorySize64 / 1024).ToString("N0")));
            cat.AddRow("Peak Paged Memory", string.Format("{0} KB", (process.PeakPagedMemorySize64 / 1024).ToString("N0")));
            cat.AddRow("Peak Virtual Memory", string.Format("{0} KB", (process.PeakVirtualMemorySize64 / 1024).ToString("N0")));
            cat.AddRow("Private Memory", string.Format("{0} KB", (process.PrivateMemorySize64 / 1024).ToString("N0")));

            return cData;
        }
		
		static public RICustomData GetProcessInformation(Process process)
		{
            List<RICustomDataColumn> columns = new List<RICustomDataColumn>();
            columns.Add(new RICustomDataColumn("Property"));
            columns.Add(new RICustomDataColumn("Value"));

            return _GetProcessInformation(new RICustomData("Process Information", columns, false, true), process);
		}
		
		static public RICustomData GetLoadedAssemblies()
		{
            List<RICustomDataColumn> columns = new List<RICustomDataColumn>();
            columns.Add(new RICustomDataColumn("Assembly"));
            columns.Add(new RICustomDataColumn("Version"));
            columns.Add(new RICustomDataColumn("Culture"));
            columns.Add(new RICustomDataColumn("Strong Named"));
            columns.Add(new RICustomDataColumn("GAC"));
            
            RICustomData cData = new RICustomData(string.Format("{0}: Loaded Assemblies", AppDomain.CurrentDomain.FriendlyName), columns, true, false) { AllowSort = true };
			
			string tmpStr = string.Empty;
			foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
			{
				RICustomDataRow row = cData.AddRow();
				
				Int32 j = 0;
				string[] fns1 = assem.FullName.Split(',');
				foreach (string s1 in fns1)
				{
					if (j != 0)
					{
						string[] fns2 = s1.Split('=');
						tmpStr = fns2[1].Trim();

						switch (j)
						{
							case 1: row.AddField(tmpStr); break;
							case 2: row.AddField(tmpStr); break;
							case 3: row.AddField((tmpStr != "null").ToString()); break;
						}
					}
					else
					{
						row.AddField(s1.Trim());
					}

					j++;
				}

				row.AddField(assem.GlobalAssemblyCache.ToString());
			}

			return cData;
		}
        
        static private Int32 CompareProcess(Process p1, Process p2)
        {
            return p1.ProcessName.CompareTo(p2.ProcessName);
        }
        
        static public RICustomData GetLoadedProcesses()
        {
            List<RICustomDataColumn> columns = new List<RICustomDataColumn>();
            columns.Add(new RICustomDataColumn("Image Name"));
            columns.Add(new RICustomDataColumn("Mem Usage", RICustomDataFieldIdType.Integer, "{0:N0} KB", null, RICustomDataColumnJustificationType.Right));
            columns.Add(new RICustomDataColumn("Virtual Mem Usage", RICustomDataFieldIdType.Integer, "{0:N0} KB", null, RICustomDataColumnJustificationType.Right));
            columns.Add(new RICustomDataColumn("Peak Mem Usage", RICustomDataFieldIdType.Integer, "{0:N0} KB", null, RICustomDataColumnJustificationType.Right));
            columns.Add(new RICustomDataColumn("Thread Count", RICustomDataFieldIdType.Integer, "{0:N0}", null, RICustomDataColumnJustificationType.Right));
            
            RICustomData cData = new RICustomData("Loaded Processes", columns, true, false) { AllowSort = true };
            List<Process> processes = new List<Process>(Process.GetProcesses());
            processes.Sort(CompareProcess);

            foreach (Process p in processes)
            {
                using (p)
                {
                    if (p.ProcessName.Trim().ToLower() == "idle")
                        continue;

                    RICustomDataRow row = cData.AddRow();

                    row.AddField(p.ProcessName);
                    row.AddField((Int32)(p.WorkingSet64 / 1024));
                    row.AddField((Int32)(p.PrivateMemorySize64 / 1024));
                    row.AddField((Int32)(p.PeakWorkingSet64 / 1024));
                    row.AddField((Int32)p.Threads.Count);
                }
            }

            processes.Clear();
            processes.Capacity = 0;

            return cData;
        }
        
		static public RICustomData GetCollection(IEnumerable enumerator, ObjectScope scope)
		{
            RICustomDataFieldIdType fType = RICustomDataFieldIdType.Integer;
            RICustomDataColumnJustificationType sIndexJustification = RICustomDataColumnJustificationType.Right;
            string sIndex = "Index";
            
            if (enumerator is IDictionary)
            {
                sIndex = "Key";                
                fType = RICustomDataFieldIdType.String;
                sIndexJustification = RICustomDataColumnJustificationType.Left;
            }

            List<RICustomDataColumn> columns = new List<RICustomDataColumn>();
            columns.Add(new RICustomDataColumn(sIndex, fType, null, null, sIndexJustification));
            columns.Add(new RICustomDataColumn("Value"));
            columns.Add(new RICustomDataColumn("Type"));

            RICustomData cData = new RICustomData(enumerator.GetType().FullName, columns, true, false);
            cData.AllowSort = true;
			cData.HasDetails = scope != ObjectScope.None;

            using (var pool = FastFormatterPool.Pool.Container())
            {
                if (enumerator is IDictionary)
                {
                    IDictionary collection = (IDictionary)enumerator;
                    foreach (Object key in collection.Keys)
                    {
                        Object value = collection[key];
                        RICustomDataRow row = cData.AddRow(key != null ? key.ToString() : null, value != null ? value.ToString() : null, value != null ? value.GetType().FullName : null);
                        if (!cData.HasDetails || value == null)
                            continue;

                        row.SetExtraData(pool.Instance, ObjectBuilder.BuildObjectPropertyMap(value, scope));
                    }
                }
                else
                {
                    Int32 index = 0;
                    foreach (Object value in enumerator)
                    {
                        RICustomDataRow row = cData.AddRow(index++, value != null ? value.ToString() : null, value != null ? value.GetType().FullName : null);
                        if (!cData.HasDetails || value == null)
                            continue;

                        row.SetExtraData(pool.Instance, ObjectBuilder.BuildObjectPropertyMap(value, scope));
                    }
                }
            }
			
			return cData;
		}
        
		static public void CreateDataTableSchema(DataSet dSet, DataTable fromTable)
		{            
			DataTable schemaTable = new DataTable(fromTable.TableName);
			dSet.Tables.Add(schemaTable);

            Type sType = typeof(String);
            schemaTable.Columns.Add("Name", sType);
            schemaTable.Columns.Add("Type", sType);
            schemaTable.Columns.Add("Primary Key", sType);
            schemaTable.Columns.Add("Auto Increment", sType);
            schemaTable.Columns.Add("Max Length", sType);
            schemaTable.Columns.Add("Default Value", sType);
            schemaTable.Columns.Add("Allow Null", sType);
            schemaTable.Columns.Add("Unique", sType);
            schemaTable.Columns.Add("Read Only", sType);

			foreach (DataColumn dCol in fromTable.Columns)
			{				
                string[] itemArray = new String[schemaTable.Columns.Count];
				itemArray[0] = dCol.Caption;
				itemArray[1] = dCol.DataType.FullName;
                itemArray[2] = Boolean.FalseString;
                itemArray[3] = dCol.AutoIncrement ? Boolean.TrueString : Boolean.FalseString;
				itemArray[4] = dCol.MaxLength.ToString();
				itemArray[5] = dCol.DefaultValue != null ? dCol.DefaultValue.ToString() : string.Empty;
                itemArray[6] = dCol.AllowDBNull ? Boolean.TrueString : Boolean.FalseString;
                itemArray[7] = dCol.Unique ? Boolean.TrueString : Boolean.FalseString;
                itemArray[8] = dCol.ReadOnly ? Boolean.TrueString : Boolean.FalseString;

				// see if the column is a primary key column
				foreach (DataColumn keyCol in fromTable.PrimaryKey)
				{
					if (keyCol.Caption == dCol.Caption)
					{
                        itemArray[2] = Boolean.TrueString;
						break;
					}
				}

				schemaTable.Rows.Add(itemArray);
			}
		}
        
        static public Boolean DataTableFieldAddQuotesIfNeeded(Type fType)
        {
            if (fType == typeof(DateTime) || fType == typeof(DateTime?)) return true;
            if (fType == typeof(TimeSpan) || fType == typeof(TimeSpan?)) return true;
            if (fType == typeof(DateTimeOffset) || fType == typeof(DateTimeOffset?)) return true;
            if (fType == typeof(Guid) || fType == typeof(Guid?)) return true;
            if (fType == typeof(String)) return true;            

            return false;
        }
        
		static public Boolean IsValidDataTableFieldType(Type fType)
		{
			if (fType == typeof(Boolean) || fType == typeof(Boolean?)) return true;
			if (fType == typeof(Byte) || fType == typeof(Byte?)) return true;
            if (fType == typeof(Char) || fType == typeof(Char?)) return true;
			if (fType == typeof(SByte) || fType == typeof(SByte?)) return true;
			if (fType == typeof(Decimal) || fType == typeof(Decimal?)) return true;
			if (fType == typeof(Double) || fType == typeof(Double?)) return true;
			if (fType == typeof(Single) || fType == typeof(Single?)) return true;
			if (fType == typeof(Int32) || fType == typeof(Int32?)) return true;
			if (fType == typeof(Int64) || fType == typeof(Int64?)) return true;
			if (fType == typeof(Int16) || fType == typeof(Int16?)) return true;
			if (fType == typeof(DateTime) || fType == typeof(DateTime?)) return true;
			if (fType == typeof(TimeSpan) || fType == typeof(TimeSpan?)) return true;
			if (fType == typeof(DateTimeOffset) || fType == typeof(DateTimeOffset?)) return true;
			if (fType == typeof(Guid) || fType == typeof(Guid?)) return true;
			if (fType == typeof(String)) return true;
			if (fType == typeof(Byte[])) return true;

			return false;
		}
        
        static private Type GetBestAllowableTableType(Type type)
        {
            Type dataType = typeof(String);
            if (IsValidDataTableFieldType(type))
            {
                dataType = Nullable.GetUnderlyingType(type);
                if (dataType == null)
                    dataType = type;
            }

            return dataType;
        }
        
        static public DataSetTypeMap GetDataSetTypeMap(Type type)
        {            
            lock (DataSetTypeMaps)
            {
                // add to cache to avoid recreating type properties via .NET Reflection
                if (DataSetTypeMaps.ContainsKey(type))
                    return DataSetTypeMaps[type];

                DataSetTypeMap dataTypeMap = new DataSetTypeMap() { ElementType = type, Properties = new List<DataSetTypeMapProperty>() };

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
                {
                    // avoid indexers
                    if (property.GetIndexParameters().Length > 0)
                        continue;

                    dataTypeMap.Properties.Add(new DataSetTypeMapProperty() { Property = property, DataType = GetBestAllowableTableType(property.PropertyType) });
                }

                DataSetTypeMaps.Add(type, dataTypeMap);

                return dataTypeMap;
            }
        }   
        
        static public DataSet PopulateDataSet(params IEnumerable[] enumerables)
        {
            // 4/13/14 MRC - added check for null enumerables
            if (enumerables == null || enumerables.Length == 0)
            {
                return new DataSet("Empty");
            }

            HashSet<string> tableNames = new HashSet<string>();
            DataSet ds = new DataSet();
            try
            {
                Int32 idx = 0;
                foreach (IEnumerable enumerable in enumerables)
                {
                    Type dictionarykeyType = null;
                    Object[] dictionaryKeys = null;
                    Boolean isDictionary = (enumerable is IDictionary);
                    if (isDictionary)
                    {
                        IDictionary dictionary = enumerable as IDictionary;
                        dictionarykeyType = GetBestAllowableTableType((dictionary.Keys as IEnumerable).AsQueryable().ElementType);
                        dictionaryKeys = new Object[dictionary.Keys.Count];
                        dictionary.Keys.CopyTo(dictionaryKeys, 0);
                    }

                    IEnumerable items = isDictionary ? (enumerable as IDictionary).Values : enumerable;
                    Type elementType = items.AsQueryable().ElementType;
                    DataSetTypeMap typeMap = GetDataSetTypeMap(elementType);

                    if (typeMap.Properties.Count == 0)
                        continue;
                    
                    if (idx++ == 0)
                    {
                        // this is the first item
                        ds.DataSetName = items.AsQueryable().ElementType.Name;
                    }
                    
                    // ensure that table names are unique
                    Int32 nameIdx = 2;
                    string tableName = elementType.Name;
                    do
                    {
                        if (tableNames.Contains(tableName))
                        {
                            tableName = string.Format("{0} ({1})", elementType.Name, nameIdx++);
                            continue;
                        }

                        tableNames.Add(tableName);
                        break;

                    } while (true);
                    
                    // create the table and it's columns
                    DataTable table = ds.Tables.Add(tableName);
                    if (isDictionary)
                    {
                        table.Columns.Add("Key", dictionarykeyType);
                    }

                    foreach (DataSetTypeMapProperty typeProperty in typeMap.Properties)
                    {
                        table.Columns.Add(typeProperty.Property.Name, typeProperty.DataType);
                    }
                    
                    // now fill the rows for this table
                    Int32 itemIdx = 0;
                    Int32 valueOffSet = isDictionary ? 1 : 0;

                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            Object[] values = new Object[table.Columns.Count];
                            if (isDictionary)
                            {
                                values[0] = dictionarykeyType == typeof(String) ? dictionaryKeys[itemIdx].ToString() : dictionaryKeys[itemIdx];
                            }

                            for (Int32 i = 0; i < typeMap.Properties.Count; i++)
                            {
                                values[i + valueOffSet] = typeMap.Properties[i].Property.GetValue(item, null);
                            }

                            table.Rows.Add(values);
                        }

                        itemIdx++;
                    }
                }

                return ds;
            }
            catch (Exception)
            {
                ds.Dispose();
                throw;
            }
        }
	}	
}
