using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using System.Drawing;
using ReflectSoftware.Insight.Common.Data;

namespace ReflectSoftware.Insight.Common
{
    [Flags]
    public enum MessageTextFlag
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0x00,
        /// <summary>
        /// The message
        /// </summary>
        Message = 0x01,
        /// <summary>
        /// The properties
        /// </summary>
        Properties = 0x02,
        /// <summary>
        /// The details
        /// </summary>
        Details = 0x04,
        /// <summary>
        /// The Extended Properties
        /// </summary>
        ExtendedProperties = 0x08
    }

    static public class MessageText
    {        
        private readonly static String FLine;        
        private readonly static String FSeparator;
                
        static MessageText()
        {
            FLine = String.Format("{0,40}", String.Empty).Replace(" ", "-");
            FSeparator = String.Format("{0,80}", String.Empty).Replace(" ", "_");
        }

        static private Boolean IsNumericType(Type objType)
        {
            if (objType == typeof(Byte)) return true;
            if (objType == typeof(SByte)) return true;
            if (objType == typeof(Decimal)) return true;
            if (objType == typeof(Double)) return true;
            if (objType == typeof(Single)) return true;
            if (objType == typeof(Int32)) return true;
            if (objType == typeof(UInt32)) return true;
            if (objType == typeof(Int64)) return true;
            if (objType == typeof(UInt64)) return true;
            if (objType == typeof(Int16)) return true;
            if (objType == typeof(UInt16)) return true;

            return false;
        }

        static public void AppendColor(StringBuilder sb, ReflectInsightColorInfo ci)
        {
            Color clr = Color.FromArgb(ci.FColor);

            sb.AppendFormat("{0,5}: 0x{1:X08}{2}", "Hex", ci.FColor, Environment.NewLine);
            sb.AppendFormat("{0,5}: {1,3}: 0x{1:X02}{2}", "Alpha", clr.A, Environment.NewLine);
            sb.AppendFormat("{0,5}: {1,3}: 0x{1:X02}{2}", "Red", clr.R, Environment.NewLine);
            sb.AppendFormat("{0,5}: {1,3}: 0x{1:X02}{2}", "Green", clr.G, Environment.NewLine);
            sb.AppendFormat("{0,5}: {1,3}: 0x{1:X02}{2}", "Blue", clr.B, Environment.NewLine);
            sb.AppendLine("------------------");
            sb.AppendFormat("{0,5}: {1,3}{2}", "Hue", ci.FHue, Environment.NewLine);
            sb.AppendFormat("{0,5}: {1,3}{2}", "Sat", ci.FSaturation, Environment.NewLine);
            sb.AppendFormat("{0,5}: {1,3}{2}", "Lum", ci.FBrightness, Environment.NewLine);
        }

        static public void AppendColor(StringBuilder sb, ReflectInsightPackage package)
        {
            AppendColor(sb, package.GetDetails<ReflectInsightColorInfo>());
        }

        static public void AppendMemory(StringBuilder sb, Byte[] memory, Int32 startPos, Int32 endPos)
        {
            Int32 maxRows = (endPos+1) / 16;
            Int32 startRow = startPos / 16;

            Int32 remainerBytes = (endPos+1) % 16;
            String largestHex = String.Format("{0:X}", maxRows * 16);
            String hexFormat = String.Format("0x{{0:X{0}}}  ", largestHex.Length);
            String topFiller = String.Format(String.Format("{{0,{0}}}", largestHex.Length + 3), String.Empty);
            Int32 maxHexRowLength = 8 * 5;

            sb.AppendFormat("{0} 0001 0203 0405 0607 0809 0A0B 0C0D 0E0F  0123456789ABCDEF{1}", topFiller, Environment.NewLine);
            sb.AppendFormat("{0} ---------------------------------------------------------{1}", topFiller, Environment.NewLine);

            for (Int32 i = startRow; i < maxRows + 1; i++)
            {
                Int32 atRow = i * 16;
                
                StringBuilder characterRow = new StringBuilder(" ");
                StringBuilder hexRow = new StringBuilder();
                
                // write starting row address
                sb.AppendFormat(hexFormat, atRow);

                Int32 maxRowBytes;
                if (i == maxRows)
                    maxRowBytes = remainerBytes;
                else
                    maxRowBytes = 16;

                for (Int32 j = atRow; j < (atRow + maxRowBytes); j++)
                {
                    if (j >= startPos)
                    {
                        characterRow.Append(Char.IsControl((Char)memory[j]) ? '.' : (Char)memory[j]);

                        if ((j % 2) != 0)
                        {
                            if (j - 1 >= startPos)
                            {
                                Int32 hex = (memory[j - 1] << 8) | memory[j];                                
                                hexRow.AppendFormat("{0:X4} ", hex);
                            }
                            else
                            {
                                Int32 hex = memory[j];
                                hexRow.AppendFormat("{0:X2} ", hex);
                            }
                        }
                        else if (j == (atRow + maxRowBytes) - 1)
                        {
                            hexRow.AppendFormat("{0:X2} ", memory[j]);
                        }
                    }
                    else
                    {
                        characterRow.Append(" ");
                        hexRow.Append("  ");

                        if ((j % 2) != 0)
                            hexRow.Append(" ");
                    }
                }

                sb.Append(hexRow);
                if (hexRow.Length < maxHexRowLength)
                {
                    sb.Append(String.Format(String.Format("{{0,{0}}}", maxHexRowLength - hexRow.Length), String.Empty));
                }

                sb.Append(characterRow);
                sb.AppendLine();
            }
        }

        static public void AppendMemory(StringBuilder sb, Byte[] memory)
        {
            AppendMemory(sb, memory, 0, memory.Length-1);
        }

        static public void AppendMemory(StringBuilder sb, ReflectInsightPackage package)
        {
            AppendMemory(sb, package.GetDetails<DetailContainerByteArray>().FData);
        }

        static private void AppendCustomDataChild(StringBuilder sb, RICustomData cData, List<RICustomDataElement> children, Int32 indent)
        {
            foreach (RICustomDataElement ce in children)
            {                
                if (ce.CustomDataType ==  RICustomDataElementType.Category)
                {
                    String catFmt = String.Format("{{0,{0}}}{{1}}{{2}}", indent);
                    
                    RICustomDataCategory cat = (RICustomDataCategory)ce;
                    sb.AppendFormat(catFmt, " ", String.Format("[{0}]", cat.Caption), Environment.NewLine);

                    AppendCustomDataChild(sb, cData, cat.Children, indent + 3);
                }
                else // must be Row
                {
                    String sIndentFmt = String.Format("{{0,{0}}}{{1}}", indent);

                    RICustomDataRow row = (RICustomDataRow)ce;
                    Int32 maxFieldsAllowed = row.Fields.Count < cData.Columns.Length ? row.Fields.Count : cData.Columns.Length;
                    String separator = cData.IsPropertyGrid ? " " : " | ";

                    for (Int32 i = 0; i < maxFieldsAllowed; i++)
                    {
                        if (i > 0)
                        {
                            sb.AppendFormat("{0}{1}", separator, row.Fields[i]);
                        }
                        else
                        {
                            sb.AppendFormat(sIndentFmt, " ", row.Fields[i]);
                            if (cData.IsPropertyGrid)
                                sb.Append(":");
                        }
                    }

                    sb.AppendFormat(Environment.NewLine);
                }
            }
        }

        static public void AppendCustomData(StringBuilder sb, RICustomData cData)
        {
            if (cData != null)
            {
                if (cData.TopCategoryCount != 0)
                {
                    sb.AppendFormat("[{0}]{1}", cData.Caption, Environment.NewLine);
                    AppendCustomDataChild(sb, cData, cData.Children, 5);
                }
                else
                {
                    AppendCollection(sb, cData);
                }
            }
            else
            {
                sb.AppendFormat("[Type: (null)]{0}", Environment.NewLine);
            }
        }

        static private void AppendCustomData(StringBuilder sb, ReflectInsightPackage package)
        {
            AppendCustomData(sb, package.GetDetails<RICustomData>());
        }

        static public void AppendDataTableCSV(StringBuilder sb, DataTable table)
        {
            // write out column header
            for (Int32 i = 0; i < table.Columns.Count; i++)
            {
                sb.AppendFormat("\"{0}\",", table.Columns[i].Caption);
            }

            // remove last comma
            sb.Remove(sb.Length - 1, 1);
            sb.AppendLine();

            Type strType = typeof(String);

            // write out rows
            for (Int32 i = 0; i < table.Rows.Count; i++)
            {
                for (Int32 j = 0; j < table.Columns.Count; j++)
                {
                    if (table.Columns[j].DataType == strType)
                    {
                        sb.AppendFormat("\"{0}\",", table.Rows[i][j] ?? String.Empty);
                    }
                    else
                    {
                        sb.AppendFormat("{0},", table.Rows[i][j] ?? String.Empty);
                    }
                }

                // remove last comma
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
            }
        }

        static public void AppendDataTable(StringBuilder sb, DataTable table)
        {
            // get the largest column width for all columns
            Int32[] largestColumnWidthSize = new Int32[table.Columns.Count];
            for(Int32 i=0; i< table.Columns.Count; i++)
            {
                if (table.Columns[i].Caption.Length > largestColumnWidthSize[i])
                    largestColumnWidthSize[i] = table.Columns[i].Caption.Length;

                for (Int32 j = 0; j < table.Rows.Count; j++)
                {
                    if (table.Rows[j][i] != null)
                    {
                        if (table.Rows[j][i].ToString().Length > largestColumnWidthSize[i])
                            largestColumnWidthSize[i] = table.Rows[j][i].ToString().Length;
                    }
                }
            }

            StringBuilder line = new StringBuilder();
            StringBuilder row = new StringBuilder();

            // prepare column formats and line
            String[] stringColumnFmt = new String[table.Columns.Count];
            for (Int32 i = 0; i < largestColumnWidthSize.Length; i++)
            {
                stringColumnFmt[i] = String.Format("{{0,-{0}}}  ", largestColumnWidthSize[i]);
                row.AppendFormat(stringColumnFmt[i], table.Columns[i].Caption);
                line.AppendFormat(String.Format(stringColumnFmt[i], " ").Replace(' ', '-'));

                // use right justification for numeric fields
                if (IsNumericType(table.Columns[i].DataType))
                    stringColumnFmt[i] = stringColumnFmt[i].Replace("-", String.Empty);
            }

            sb.AppendLine(row.ToString().TrimEnd(null));
            sb.AppendLine(line.ToString());

            // write data            
            for (Int32 i = 0; i < table.Rows.Count; i++)
            {
                row = new StringBuilder();
                for (Int32 j = 0; j < table.Columns.Count; j++)
                    row.AppendFormat(String.Format(stringColumnFmt[j], table.Rows[i][j] ?? "<NULL>"));
                
                sb.AppendLine(row.ToString().TrimEnd(null));
            }
        }

        static public void AppendDataSet(StringBuilder sb, DataSet ds)
        {
            Int32 tableCount = ds.Tables.Count;
            foreach (DataTable table in ds.Tables)
            {
                tableCount--;
                sb.AppendFormat("[{0}]{1}", table.TableName, Environment.NewLine);
                AppendDataTable(sb, table);

                if (tableCount != 0)
                    sb.AppendLine();
            }
        }

        static public void AppendData(StringBuilder sb, ReflectInsightPackage package)
        {
            if (package.IsDetail<DetailContainerDataSet>())
            {
                using (DetailContainerDataSet dsc = package.GetDetails<DetailContainerDataSet>())
                {
                    AppendDataSet(sb, dsc.FData);
                }
            }
            else 
            {
                using (DetailContainerDataTable tc = package.GetDetails<DetailContainerDataTable>())
                {
                    sb.AppendFormat("[{0}]{1}", tc.FData.TableName, Environment.NewLine);
                    AppendDataTable(sb, tc.FData);
                }
            }
        }

        static public void AppendCollection(StringBuilder sb, RICustomData cData)
        {
            if (!string.IsNullOrWhiteSpace(cData.Caption))
                sb.AppendFormat("[{0}]{1}", cData.Caption, Environment.NewLine);

            RICustomDataColumn[] cols = cData.Columns;
            String[][] rows = cData.TopRowToStringArray();

            // get the largest column width for all columns
            Int32[] largestColumnWidthSize = new Int32[cols.Length];
            for (Int32 i = 0; i < cols.Length; i++)
            {
                if (cols[i].Caption.Length > largestColumnWidthSize[i])
                    largestColumnWidthSize[i] = cols[i].Caption.Length;

                for (Int32 j = 0; j < rows.Length; j++)
                {
                    if (rows[j][i].Length > largestColumnWidthSize[i])
                        largestColumnWidthSize[i] = rows[j][i].Length;
                }
            }

            StringBuilder line = new StringBuilder();
            StringBuilder row = new StringBuilder();

            // prepare column formats and line
            String[] stringColumnFmt = new String[cols.Length];
            for (Int32 i = 0; i < largestColumnWidthSize.Length; i++)
            {
                stringColumnFmt[i] = String.Format("{{0,-{0}}}  ", largestColumnWidthSize[i]);
                row.AppendFormat(stringColumnFmt[i], cols[i].Caption);
                line.AppendFormat(String.Format(stringColumnFmt[i], " ").Replace(' ', '-'));

                // use right justification for numeric fields
                if (cols[i].Justification == RICustomDataColumnJustificationType.Right)
                    stringColumnFmt[i] = stringColumnFmt[i].Replace("-", String.Empty);
            }

            if (!string.IsNullOrWhiteSpace(cols[0].Caption))
            {
                sb.AppendLine(row.ToString().TrimEnd(null));
                sb.AppendLine(line.ToString());
            }

            // write fields
            for (Int32 i = 0; i < rows.Length; i++)
            {
                row = new StringBuilder();
                for (Int32 j = 0; j < cols.Length; j++)
                    row.AppendFormat(String.Format(stringColumnFmt[j], rows[i][j]));

                sb.AppendLine(row.ToString().TrimEnd(null));
            }

            if (!cData.HasDetails)
                return;

            RICustomData[] extraData = cData.TopRowToExtraDataArray<RICustomData>();
            for (Int32 i = 0; i < extraData.Length; i++)
            {
                sb.AppendLine();
                AppendCustomData(sb, extraData[i]);
            }
        }

        static public void AppendCollection(StringBuilder sb, ReflectInsightPackage package)
        {
            AppendCollection(sb, package.GetDetails<RICustomData>());
        }

        static public void AppendAttachment(StringBuilder sb, ReflectInsightPackage package)
        {
            ReflectInsightAttachmentInfo aInfo = package.GetSubDetails<ReflectInsightAttachmentInfo>();
            sb.AppendFormat("File name: {0}{1}", Path.GetFileName(aInfo.FileName), Environment.NewLine);
            sb.AppendFormat("File size: {0,7:0,0} bytes{1}", aInfo.FileSize, Environment.NewLine);
        }

        static public void AppendMessageProperties(StringBuilder sb, ReflectInsightPackage package, MessageTextFlag flags)
        {
            if ((flags & MessageTextFlag.Properties) != MessageTextFlag.Properties)
            {
                return;
            }

            if ((flags & MessageTextFlag.Properties) != MessageTextFlag.Details)
            {
                sb.AppendLine();
            }

            sb.AppendLine("Message Properties");
            sb.AppendLine(FLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Category", package.FCategory, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Computer", package.FMachineName, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Application", package.FApplication, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "User Name", package.FUserName, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Process Id", package.FProcessID, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Thread Id", package.FThreadID, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Request Id", package.FRequestID, Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Time", package.FDateTime.ToLocalTime(), Environment.NewLine);
            sb.AppendFormat("{0,11}: {1}{2}", "Session Id", package.FSessionID, Environment.NewLine);                        
            sb.AppendFormat("{0,11}: {1}{2}", "Domain Id", package.FDomainID, Environment.NewLine);            
            sb.AppendFormat("{0,11}: {1}{2}", "User Domain", package.FUserDomainName, Environment.NewLine);
            sb.AppendLine();
        }

        static public void AppendMessageExtendedProperties(StringBuilder sb, ReflectInsightPackage package, MessageTextFlag flags)
        {
            if ((flags & MessageTextFlag.ExtendedProperties) != MessageTextFlag.ExtendedProperties || !package.HasExtendedProperties)
            {
                return;
            }

            if ((flags & MessageTextFlag.ExtendedProperties) != MessageTextFlag.Details)
            {
                sb.AppendLine();
            }

            foreach (ReflectInsightExtendedProperties exProps in package.FExtPropertyContainer.ExtendedProperties)
            {
                if (exProps.Properties.Count == 0)
                    continue;

                foreach (String key in exProps.Properties.AllKeys)
                {
                    sb.AppendFormat("{0,11}: {1}: {2}{3}", exProps.Caption, key, exProps.Properties[key], Environment.NewLine);
                }
            }

            sb.AppendLine();
        }

        static public void AppendMessageSubDetails(StringBuilder sb, ReflectInsightPackage package, MessageTextFlag flags)
        {
            if (((flags & MessageTextFlag.Details) != MessageTextFlag.Details) || !package.HasSubDetails)
            {
                return;
            }

            switch (package.FMessageType)
            {
                case MessageType.SendAttachment:
                    sb.AppendLine();
                    AppendAttachment(sb, package);
                    sb.AppendLine();
                    break;
            }
        }

        static public void AppendMessageDetails(StringBuilder sb, ReflectInsightPackage package, MessageTextFlag flags)
        {
            if (((flags & MessageTextFlag.Details) != MessageTextFlag.Details) || !package.HasDetails)
            {
                return;
            }

            if (package.IsDetail<DetailContainerString>())
            {
                sb.AppendLine();
                sb.AppendLine(package.GetDetails<DetailContainerString>().FData);
                sb.AppendLine();

                return;
            }
            
            switch (package.FMessageType)
            {
                case MessageType.EnterMethod:
                case MessageType.SendObject:
                case MessageType.SendProcessInformation:
                case MessageType.SendThreadInformation:
                case MessageType.SendSystemInformation:
                case MessageType.SendLoadedAssemblies:
                case MessageType.SendLoadedProcesses:
                case MessageType.SendAppDomainInformation:
                case MessageType.SendMemoryStatus:
                case MessageType.SendHttpModuleInformation:
                case MessageType.SendMiniDumpFile:
                case MessageType.SendCustomData:
                case MessageType.SendHttpRequest:
                    sb.AppendLine();
                    AppendCustomData(sb, package);
                    sb.AppendLine();
                    break;

                case MessageType.SendCollection:                
                    sb.AppendLine();
                    AppendCollection(sb, package);
                    sb.AppendLine();
                    break;

                case MessageType.SendDataSet:
                case MessageType.SendDataTable:
                case MessageType.SendDataView:
                case MessageType.SendDataSetSchema:
                case MessageType.SendDataTableSchema:                
                case MessageType.SendLinqResults:
                case MessageType.SendTypedCollection:
                    sb.AppendLine();
                    AppendData(sb, package);
                    sb.AppendLine();
                    break;

                case MessageType.SendMemory:
                case MessageType.SendStream:
                    sb.AppendLine();
                    AppendMemory(sb, package);
                    sb.AppendLine();
                    break;

                case MessageType.SendColor:
                    sb.AppendLine();
                    AppendColor(sb, package);
                    sb.AppendLine();
                    break;

                default:
                    break;
            }
        }

        static public void AppendMessage(StringBuilder sb, ReflectInsightPackage package, String messagePattern, List<String> timePatterns, MessageTextFlag flags)
        {
            if ((flags & MessageTextFlag.Message) != MessageTextFlag.Message)
            {
                return;
            }

            String message = package.FMessage;
            switch (package.FMessageType)
            {
                case MessageType.AddCheckpoint: message = message.Replace("Checkpoint:", String.Format("[Checkpoint.{0}]: ", (Checkpoint)package.FMessageSubType)); break;
                case MessageType.SendCheckmark: message = message.Replace("Checkmark:", String.Format("[Checkmark.{0}]: ", (Checkmark)package.FMessageSubType)); break;
                case MessageType.SendLevel: message = String.Format("[Level.{0}]: {1}", (LevelType)package.FMessageSubType, message); break;
            }
                        
            if (!string.IsNullOrWhiteSpace(messagePattern))
            {
                sb.AppendFormat("{0}", RIUtils.PrepareString(messagePattern, package, String.Empty, timePatterns));
            }
            else
            {
                sb.AppendFormat("{0}", message);
            }
            
            sb.AppendLine();            
        }

        static private void AppendPackage(StringBuilder sb, ReflectInsightPackage package, MessageTextFlag flags, String messagePattern, List<String> timePatterns)
        {
            AppendMessage(sb, package, messagePattern, timePatterns, flags);
            AppendMessageSubDetails(sb, package, flags);
            AppendMessageDetails(sb, package, flags);
            AppendMessageProperties(sb, package, flags);
            AppendMessageExtendedProperties(sb, package, flags);
        }

        static public String Convert(ReflectInsightPackage package, MessageTextFlag flags, String messagePattern, List<String> timePatterns)
        {
            if (package.FMessageType == MessageType.Clear || RIUtils.IsViewerSpecificMessageType(package.FMessageType))
            {
                return String.Empty;
            }

            StringBuilder sb = new StringBuilder();
            switch (package.FMessageType)
            {
                case MessageType.AddSeparator:
                    sb.AppendLine(FSeparator);
                    break;

                default:
                    AppendPackage(sb, package, flags, messagePattern, timePatterns);
                    break;
            }

            return sb.ToString();
        }

        static public String Convert(ReflectInsightPackage package, MessageTextFlag flags, String messagePattern)
        {
            return Convert(package, flags, messagePattern, null);
        }


        static public String Convert(ReflectInsightPackage package, MessageTextFlag flags)
        {
            return Convert(package, flags, null, null);
        }
    }
}
