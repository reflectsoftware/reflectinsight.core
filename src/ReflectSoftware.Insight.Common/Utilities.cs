using Plato.Extensions;
using Plato.Security.Cryptography;
using ReflectSoftware.Insight.Common.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace ReflectSoftware.Insight.Common
{
    static public class RIUtils
	{
        private readonly static Dictionary<String, TimeZoneInfo> SystemTimeZoneLookup;
        private readonly static Dictionary<MessageType, String> MessageTypeStringLookup;
        
        public static MessageType SimpleMessageTypeStartRange { get; private set; }
        public static MessageType SimpleMessageTypeEndRange { get; private set; }
        public static MessageType ComplexMessageTypeStartRange { get; private set; }
        public static MessageType ComplexMessageTypeEndRange { get; private set; }
        public static MessageType ViewerMessageTypeStartRange { get; private set; }
        public static MessageType ViewerMessageTypeEndRange { get; private set; }
        public static MessageType LogMessageTypeStartRange { get; private set; }
        public static MessageType LogMessageTypeEndRange { get; private set; }
        public static MessageType DataAnalyticsTypeStartRange { get; private set; }
        public static MessageType DataAnalyticsTypeEndRange { get; private set; }
        
        static RIUtils()
        {
            SystemTimeZoneLookup = new Dictionary<String, TimeZoneInfo>();
            MessageTypeStringLookup = new Dictionary<MessageType, String>();

            ConstructMessageTypeStringLookup();

            SimpleMessageTypeStartRange = MessageType.Clear;
            ComplexMessageTypeStartRange = MessageType.SendImage;
            ViewerMessageTypeStartRange = MessageType.ViewerClearAll;
            LogMessageTypeStartRange = MessageType.PurgeLogFile;
            
            // 3/29/14 MRC
            DataAnalyticsTypeStartRange = MessageType.SendData_XY;
            
            SimpleMessageTypeEndRange = SimpleMessageTypeStartRange;
            ComplexMessageTypeEndRange = ComplexMessageTypeStartRange;
            ViewerMessageTypeEndRange = ViewerMessageTypeStartRange;
            LogMessageTypeEndRange = LogMessageTypeStartRange;

            // 3/29/14 MRC
            DataAnalyticsTypeEndRange = DataAnalyticsTypeStartRange;

            // we need to iterate through the complete MessageType enum to determine 
            // the ends of the SimpleType and ComplexType Ranges
            foreach(MessageType mt in Enum.GetValues(typeof(MessageType)))
            {
                if (mt > SimpleMessageTypeEndRange && mt < MessageType.SendImage)
                {
                    SimpleMessageTypeEndRange = mt;
                }
                else if (mt > ComplexMessageTypeEndRange && mt < MessageType.ViewerClearAll)
                {
                    ComplexMessageTypeEndRange = mt;
                }
                else if (mt > ViewerMessageTypeEndRange && mt < MessageType.PurgeLogFile)
                {
                    ViewerMessageTypeEndRange = mt;
                }
                else if (mt > LogMessageTypeEndRange && mt < MessageType.SendData_XY)
                {
                    LogMessageTypeEndRange = mt;
                }
                else if (mt > DataAnalyticsTypeEndRange && mt < MessageType._End)
                {
                    DataAnalyticsTypeEndRange = mt;
                }
            }
        }
        
        static private void ConstructMessageTypeStringLookup()
        {
            foreach (MessageType mType in Enum.GetValues(typeof(MessageType)))
            {
                MessageTypeStringLookup[mType] = mType.ToString();
            }
        }
        
        public static Boolean IsPrimitiveType(Type typ)
        {
            if (typ == typeof(Byte)) return true;
            if (typ == typeof(SByte)) return true;
            if (typ == typeof(Char)) return true;
            if (typ == typeof(Decimal)) return true;
            if (typ == typeof(Double)) return true;
            if (typ == typeof(Single)) return true;
            if (typ == typeof(Int32)) return true;
            if (typ == typeof(UInt32)) return true;
            if (typ == typeof(Int64)) return true;
            if (typ == typeof(UInt64)) return true;
            if (typ == typeof(Int16)) return true;
            if (typ == typeof(UInt16)) return true;
            if (typ == typeof(String)) return true;
            if (typ == typeof(StringBuilder)) return true;
            if (typ == typeof(DateTime)) return true;

            return false;
        }

        public static String GetUserApplicationDirectory()
		{
			return String.Format(@"{0}\ReflectSoftware\ReflectInsight", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
		}

        public static String GetDefaultApplicationLogDirectory()
		{
			return String.Format(@"{0}\My ReflectInsight Files\Logs", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
		}        
        
		public static String HashString(String str)
		{
			Random rn = new Random((Int32)DateTime.Now.Ticks);
			return CryptoServices.RNGBase64String(rn.Next(10) + str.Length);
		}

        public static String Reverse(String str)
		{            
            Int32 len = str.Length;
            if (len <= 1) return str;

			StringBuilder sb = new StringBuilder(str);
			for (Int32 i = 0; i < (len / 2); i++)
			{
				Char tmpChar = sb[i];
				sb[i] = sb[len - 1 - i];
				sb[len - 1 - i] = tmpChar;
			}

			return sb.ToString();
		}

		public static Byte[] Reverse(Byte[] bytes)
		{            
            Int32 len = bytes.Length;
            if (len <= 1) return bytes;

			for (Int32 i = 0; i < (len / 2); i++)
			{
				Byte tmpByte = bytes[i];
				bytes[i] = bytes[len - 1 - i];
				bytes[len - 1 - i] = tmpByte;
			}

			return bytes;
		}

        public static String ConvertBytesToHexString(Byte[] bytes)
		{
			return BitConverter.ToInt64(bytes, 0).ToString("X");
		}

        public static String ConvertBytesToHexStringTruncIfBigger(Byte[] bytes, Int32 fixedSize, String defaultText)
		{
			if (bytes == null) return "(null)";

			if (bytes.Length < 8)
			{
				Byte[] bytes2 = bytes;
				bytes = new Byte[8];
				Array.Copy(bytes2, 0, bytes, 0, bytes2.Length);
			}

			String sHex = defaultText;
			if (bytes.Length == fixedSize)
			{
				sHex = ConvertBytesToHexString(Reverse(bytes));
				sHex = String.Format("0x{0}", sHex.PadLeft(16, '0'));
			}

			return sHex;
		}

		public static Int32 StreamValueType(Stream ms, ValueType vType)
		{
			Byte[] tmpBytes = new Byte[Marshal.SizeOf(vType)];
			IntPtr mBuffer = Marshal.AllocCoTaskMem(tmpBytes.Length);
			try
			{
				Marshal.StructureToPtr(vType, mBuffer, true);
				Marshal.Copy(mBuffer, tmpBytes, 0, tmpBytes.Length);
				ms.Write(tmpBytes, 0, tmpBytes.Length);

				return tmpBytes.Length;
			}
			finally
			{
				Marshal.FreeCoTaskMem(mBuffer);
			}
		}

        public static T UnStreamValueType<T>(Byte[] stream)
		{
			IntPtr mBuffer = Marshal.AllocCoTaskMem(stream.Length);
			try
			{
				Marshal.Copy(stream, 0, mBuffer, stream.Length);
				return (T)Marshal.PtrToStructure(mBuffer, typeof(T));
			}
			finally
			{
				Marshal.FreeCoTaskMem(mBuffer);
			}
		}

        public static Byte[] StreamValueType(ValueType vType)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				StreamValueType(ms, vType);
				return ms.ToArray();
			}
		}
        
		public static Int32 WriteBytesToStream(Stream ms, Byte[] bStream)
		{
			ms.Write(bStream, 0, bStream.Length);
			return bStream.Length;
		}

		public static Int32 WriteUnicodeStringToStream(Stream ms, String str)
		{
			if (str == null) return 0;

			Int32 sizeOfNull = Marshal.SizeOf((UInt16)0);
			Byte[] bStr = Encoding.Unicode.GetBytes(str);
			ms.Write(bStr, 0, bStr.Length);
			ms.Write(BitConverter.GetBytes((UInt16)0), 0, sizeOfNull);

			return bStr.Length + sizeOfNull;
		}

        public static Byte[] UnicodeStringToBytes(String str)
		{
			using(MemoryStream ms = new MemoryStream())
			{
				WriteUnicodeStringToStream(ms, str);
				return ms.ToArray();
			}
		}

        public static Int32 ModifyColorPart(Int32 currentValue, Int32 offset)
		{
			Int32 newValue = currentValue + offset;
			if (newValue > 255) newValue = 255;
			if (newValue < 0) newValue = 0;

			return newValue;
		}

        public static TimeZoneInfo FindSystemTimeZoneById(String timeZoneId)
        {
            lock (SystemTimeZoneLookup)
            {
                if (SystemTimeZoneLookup.ContainsKey(timeZoneId))
                    return SystemTimeZoneLookup[timeZoneId];

                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                SystemTimeZoneLookup[timeZoneId] = tz;

                return tz;
            }
        }

        public static Boolean IsValidMessageType(MessageType mType)
        {
            // Do not replace this logic to use the Enum.IsDefined(typeof(MessageType)
            // approach as it uses reflection and is 8 times slower than 
            // checking ranges.
            // This method is called at very high speeds and must be as fast as possible

            return (mType >= SimpleMessageTypeStartRange) && (mType <= SimpleMessageTypeEndRange)
                || (mType >= ComplexMessageTypeStartRange) && (mType <= ComplexMessageTypeEndRange)
                || (mType >= ViewerMessageTypeStartRange) && (mType <= ViewerMessageTypeEndRange)
                || (mType >= LogMessageTypeStartRange) && (mType <= LogMessageTypeEndRange)
                || (mType >= DataAnalyticsTypeStartRange) && (mType <= DataAnalyticsTypeEndRange);  
        }

        public static Boolean IsMessageTypeSimple(MessageType mType)
        {
            return (mType >= SimpleMessageTypeStartRange) && (mType <= SimpleMessageTypeEndRange);
        }

        public static Boolean IsMessageTypeComplex(MessageType mType)
        {
            return (mType >= ComplexMessageTypeStartRange) && (mType <= ComplexMessageTypeEndRange);
        }

        static public Boolean IsViewerSpecificMessageType(MessageType mType)
        {
            return (mType == MessageType.ViewerClearAll || mType == MessageType.ViewerClearWatches || mType == MessageType.ViewerSendWatch);
        }

        static public Boolean IsLogSpecificMessageType(MessageType mType)
        {
            return (mType >= LogMessageTypeStartRange) && (mType <= LogMessageTypeEndRange);
        }

        static public Boolean IsMessageTypeAnalytics(MessageType mType)
        {
            return (mType >= DataAnalyticsTypeStartRange) && (mType <= DataAnalyticsTypeEndRange);
        }

        public static void HandleUnknownMessage(ReflectInsightPackage message)
        {
            if(!IsValidMessageType(message.FMessageType))
            {
                if (message.FMessageType > SimpleMessageTypeEndRange)
                {
                    // for complex type messages we must clear the Details 
                    // because we have no clue what object type the 
                    // details are and have no way to serialized them
                    message.FDetails = null;
                    message.FSubDetails = null;
                }

                message.FMessageType = MessageType.Unknown;
                message.FMessageSubType = 0;
            }
        }

        public static String GetMessageTypeString(MessageType mType)
        {
            return MessageTypeStringLookup[mType];
        }

        public static Int32 GetStringHash(String value)
        {            
            return (Int32)value.BKDRHash();
        }

        private static void GetListOfTimePatterns(List<String> timePatterns, String pattern, String timeType, String zone)
        {            
            const String defaultTimeFormat = "hh:mm:ss, yyyy-MM-dd";

            String indexedString = String.Format("%{0}", timeType);
            Int32 sidx = pattern.IndexOf(indexedString);
            while (sidx >= 0)
            {
                Int32 eidx = pattern.IndexOf("%", sidx + 1);
                if (eidx < 0)
                    break;

                String timePatternSection = pattern.Substring(sidx, eidx - sidx + 1);
                Int32 braceSidx = timePatternSection.IndexOf("{");
                if (braceSidx >= 0)
                {
                    Int32 braceEidx = timePatternSection.IndexOf("}");
                    if (braceEidx >= 0)
                    {
                        String timePattern = timePatternSection.Substring(braceSidx + 1, braceEidx - braceSidx - 1);
                        timePatterns.Add(String.Format("{0}|{1}|{2}", timePatternSection, timePattern, zone));
                    }
                }
                else
                {
                    timePatterns.Add(String.Format("{0}|{1}|{2}", timePatternSection, defaultTimeFormat, zone));
                }

                sidx = pattern.IndexOf(indexedString, eidx + 1);
            }
        }

        public static List<String> GetListOfTimePatterns(String pattern)
        {
            List<String> timePatterns = new List<String>();

            GetListOfTimePatterns(timePatterns, pattern, "time", "L");
            GetListOfTimePatterns(timePatterns, pattern, "utctime", "U");

            return timePatterns;
        }
        public static String PrepareString(String pattern, ReflectInsightPackage package, String details, List<String> timePatterns)
        {
            StringBuilder sStr = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(pattern))
            {                                
                sStr.Append(pattern);
                sStr.Replace("%message%", package.FMessage);
                sStr.Replace("%messagetype%", RIUtils.GetMessageTypeString(package.FMessageType));
                sStr.Replace("%sessionid%", package.FSessionID.ToString());
                sStr.Replace("%requestid%", package.FRequestID.ToString());
                sStr.Replace("%machine%", package.FMachineName);
                sStr.Replace("%category%", package.FCategory);
                sStr.Replace("%processid%", package.FProcessID.ToString());
                sStr.Replace("%threadid%", package.FThreadID.ToString());
                sStr.Replace("%requestid%", package.FRequestID.ToString());
                sStr.Replace("%domainid%", package.FDomainID.ToString());
                sStr.Replace("%application%", package.FApplication);
                sStr.Replace("%userdomain%", package.FUserDomainName);
                sStr.Replace("%username%", package.FUserName);                
                sStr.Replace("%details%", details);

                if (timePatterns != null)
                {
                    foreach (String timePattern in timePatterns)
                    {
                        String[] parts = timePattern.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        DateTime dt = String.Compare(parts[2], "U", false) == 0 ? package.FDateTime.ToUniversalTime() : package.FDateTime.ToLocalTime();

                        sStr.Replace(parts[0], dt.ToString(parts[1]));
                    }
                }
            }

            return sStr.ToString();
        }

        public static String PrepareString(String pattern, ReflectInsightPackage package, String details)
        {
            return PrepareString(pattern, package, details, null);
        }

        public static string FormatBytes(long bytes)
        {
            try
            {
                const int scale = 1024;
                string[] scales = new string[] { "GB", "MB", "KB", "Bytes" };
                long max = (long)Math.Pow(scale, scales.Length - 1);

                foreach (string s in scales)
                {
                    if (bytes > max)
                        return (String.Format("{0:##.##} {1}", decimal.Divide(bytes, max), s));

                    max /= scale;
                }
                return "0 Bytes";
            }
            catch
            {
                return "0 Bytes";
            }
        }

        //---------------------------------------------------------------------
        public static string GetDiskSpace(string strDrive)
        {
            const string strTotal = "0";
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady && drive.Name.Equals(strDrive, StringComparison.CurrentCulture))
                    {
                        // Get the total size first
                        long bytes = Math.Abs(drive.TotalSize);
                        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                        string strTemp = (Math.Sign(drive.TotalSize) * num).ToString(CultureInfo.InvariantCulture) + suf[place];

                        if (drive.TotalFreeSpace == 0)
                            return (String.Format(CultureInfo.CurrentCulture, "0{0} of {1}", suf[0], strTemp));

                        bytes = Math.Abs(drive.TotalFreeSpace);
                        place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                        num = Math.Round(bytes / Math.Pow(1024, place), 1);

                        return (String.Format(CultureInfo.CurrentCulture, "{0}{1} of {2}", Math.Sign(drive.TotalFreeSpace) * num, suf[place], strTemp));
                    }
                }
            }
            catch 
            { 
            }

            return (strTotal);
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            return (string.Concat(ts.Days > 1 ? string.Concat(ts.Days, " Days ") : (ts.Days == 1) ? string.Concat(ts.Days, " Day ") : "",
                ts.Hours > 1 ? string.Concat(ts.Hours, " Hours ") : (ts.Hours == 1) ? string.Concat(ts.Hours, " Hour ") : "",
                ts.Minutes > 1 ? string.Concat(ts.Minutes, " Minutes ") : (ts.Minutes == 1) ? string.Concat(ts.Minutes, " Minute ") : "",
                ts.Seconds > 1 ? string.Concat(ts.Seconds, " Seconds ") : (ts.Seconds == 1) ? string.Concat(ts.Seconds, " Second ") : (ts.Hours > 0 || ts.Days > 0 || ts.Minutes > 0) ? "" : "< 1 Second"));
        }

        public static void CopyFile(Stream source, Stream destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            source.Seek(0, SeekOrigin.Begin);
            destination.Seek(0, SeekOrigin.Begin);
            
            const Int32 _CHUCK_SIZE = 0x100000; // 1 MB
            Byte[] workingArray = new Byte[_CHUCK_SIZE];

            // control variables                    
            Int32 remaining = (Int32)source.Length;
            Int32 chunk = remaining < _CHUCK_SIZE ? remaining : _CHUCK_SIZE;

            while (remaining > 0)
            {
                source.Read(workingArray, 0, chunk);
                destination.Write(workingArray, 0, chunk);               

                // update control variables
                remaining -= chunk;
                chunk = remaining < _CHUCK_SIZE ? remaining : _CHUCK_SIZE;
            }
        }

        public static String DetermineParameterPath(String path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // determine correct path
            path = path.Replace("$(workingdir)", AppDomain.CurrentDomain.BaseDirectory);
            path = path.Replace("$(mydocuments)", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            // handle UNC path and at the same time 
            // get rid of double slashes (\\) if they are 
            // not part of the UNC
            if (path.Length >= 2 && String.Compare(path.Substring(0, 2), @"\\", false, CultureInfo.InvariantCulture) == 0)
            {
                path = String.Format(CultureInfo.InvariantCulture, "[&&]{0}", path.Substring(2, path.Length - 2));
                path = path.Replace(@"\\", @"\");
            }

            path = path.Replace("[&&]", @"\\");

            try
            {
                path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
            }
            catch
            {
                // just swallow and return the path as is
            }

            return path;
        }

        public static String FormatXml(String xmlString, Boolean bIndent)
        {
            var xd = new XmlDocument()
            {
                PreserveWhitespace = true
            };
            xd.LoadXml(xmlString);

            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            {
                using (XmlTextWriter xtw = new XmlTextWriter(sw))
                {
                    xtw.Formatting = bIndent ? Formatting.Indented : Formatting.None;
                    xd.WriteTo(xtw);
                }
            }

            return sb.ToString();
        }

        public static void GCCollect()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        public static double ConvertMillisecondsToDays(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds).TotalDays;
        }

        public static double ConvertSecondsToDays(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).TotalDays;
        }

        public static double ConvertMinutesToDays(double minutes)
        {
            return TimeSpan.FromMinutes(minutes).TotalDays;
        }

        public static double ConvertHoursToDays(double hours)
        {
            return TimeSpan.FromHours(hours).TotalDays;
        }

        public static double ConvertMillisecondsToHours(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds).TotalHours;
        }

        public static double ConvertSecondsToHours(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).TotalHours;
        }

        public static double ConvertMinutesToHours(double minutes)
        {
            return TimeSpan.FromMinutes(minutes).TotalHours;
        }

        public static double ConvertDaysToHours(double days)
        {
            return TimeSpan.FromHours(days).TotalHours;
        }

        public static double ConvertMillisecondsToMinutes(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds).TotalMinutes;
        }

        public static double ConvertSecondsToMinutes(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).TotalMinutes;
        }

        public static double ConvertHoursToMinutes(double hours)
        {
            return TimeSpan.FromHours(hours).TotalMinutes;
        }

        public static double ConvertDaysToMinutes(double days)
        {
            return TimeSpan.FromDays(days).TotalMinutes;
        }

        public static double ConvertMillisecondsToSeconds(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds).TotalSeconds;
        }

        public static double ConvertMinutesToSeconds(double minutes)
        {
            return TimeSpan.FromMinutes(minutes).TotalSeconds;
        }

        public static double ConvertHoursToSeconds(double hours)
        {
            return TimeSpan.FromHours(hours).TotalSeconds;
        }

        public static double ConvertDaysToSeconds(double days)
        {
            return TimeSpan.FromDays(days).TotalSeconds;
        }

        public static double ConvertSecondsToMilliseconds(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).TotalMilliseconds;
        }

        public static double ConvertMinutesToMilliseconds(double minutes)
        {
            return TimeSpan.FromMinutes(minutes).TotalMilliseconds;
        }

        public static double ConvertHoursToMilliseconds(double hours)
        {
            return TimeSpan.FromHours(hours).TotalMilliseconds;
        }

        public static double ConvertDaysToMilliseconds(double days)
        {
            return TimeSpan.FromDays(days).TotalMilliseconds;
        }
    }	
}
