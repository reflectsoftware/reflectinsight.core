// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using Plato.Serializers.FormatterPools;
using Plato.Miscellaneous;
using Plato.Serializers;

namespace ReflectSoftware.Insight.Common
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileHelper
    {
        public const String REFLECTINSIGHT_APP_VERSION_STR = "5.7";
        public const UInt16 REFLECTINSIGHT_APP_VERSION_MAJOR = 5;
        public const UInt16 REFLECTINSIGHT_APP_VERSION_MINOR = 7; 

        //---------------------------------------------------------------------
        /// <summary>
        /// Binary File version
        /// *** NOTE: DO NOT CHANGE THIS VERSION EVEN IF THE APP VERSION CHANGES.
        /// *** THESE ARE SPECIAL VERSIONS.
        /// </summary>
        //---------------------------------------------------------------------

        public const UInt16 REFLECTINSIGHT_BIN_FILE_VERSION_MAJOR = 5;
        public const UInt16 REFLECTINSIGHT_BIN_FILE_VERSION_MINOR = 3;

        //----------------------------------------------------------------------
        /// <summary>
        /// User Binary File version
        /// *** NOTE: DO NOT CHANGE THIS VERSION EVEN IF THE APP VERSION CHANGES.
        /// *** THESE ARE SPECIAL VERSIONS.
        /// </summary>
        //---------------------------------------------------------------------

        public const UInt16 REFLECTINSIGHT_USER_BIN_VERSION_MAJOR = 5;
        public const UInt16 REFLECTINSIGHT_USER_BIN_VERSION_MINOR = 3;
        
        static public readonly Byte[] FileSignature = new Byte[] { 0x92, 0x20, 0x10, 0x93 };
        static public readonly Byte[] OldFileSignature = new Byte[] { 0x05, 0x00, 0x00, 0x00, 0x01, 0x04, 0x00, 0x00, 0x00, 0x92, 0x20, 0x10, 0x93 };
        static private RIInstalledInfo FInstalledInfo;

        /// <summary>
        /// Initializes the <see cref="FileHelper"/> class.
        /// </summary>
        static FileHelper()
        {
            PrepareInstallInfo();            
        }

        /// <summary>
        /// Prepares the install information.
        /// </summary>
        static private void PrepareInstallInfo()
        {
            FInstalledInfo = new RIInstalledInfo
            {
                AppVersion = REFLECTINSIGHT_APP_VERSION_STR,
                AppVersionMajor = REFLECTINSIGHT_APP_VERSION_MAJOR,
                AppVersionMinor = REFLECTINSIGHT_APP_VERSION_MINOR,
                BinFileVersionMajor = REFLECTINSIGHT_BIN_FILE_VERSION_MAJOR,
                BinFileVersionMinor = REFLECTINSIGHT_BIN_FILE_VERSION_MINOR,
                UserBinVersionMajor = REFLECTINSIGHT_USER_BIN_VERSION_MAJOR,
                UserBinVersionMinor = REFLECTINSIGHT_USER_BIN_VERSION_MINOR,
            };
        }

        /// <summary>
        /// Forces the directories for path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        static public Boolean ForceDirectoriesForPath(String path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Forces the name of the directories for file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        static public Boolean ForceDirectoriesForFileName(String fileName)
        {
            return ForceDirectoriesForPath(Path.GetDirectoryName(fileName));
        }

        /// <summary>
        /// Deletes all logs.
        /// </summary>
        /// <param name="path">The path.</param>
        static public void DeleteAllLogs(String path)
        {
            String fDir = Path.GetDirectoryName(path);
            String fExt = Path.GetExtension(path);
            String fName = Path.GetFileName(path).Replace(fExt, String.Empty);
            String fSpec = String.Format(@"{0}*{1}", fName, fExt);

            String[] files = Directory.GetFiles(fDir, fSpec, SearchOption.TopDirectoryOnly);
            foreach (String file in files)
            {
                File.Delete(file);
            }

            File.Delete(path);
        }

        /// <summary>
        /// Gets the file name from pattern.
        /// </summary>
        /// <param name="patternFileName">Name of the pattern file.</param>
        /// <returns></returns>
        static public string GetFileNameFromPattern(string patternFileName)
        {
            var sStr = new StringBuilder(patternFileName);
            sStr.Replace("{{UserName}}", Environment.UserName)
                .Replace("{{MachineName}}", Environment.MachineName);

            String filePath = RIUtils.DetermineParameterPath(sStr.ToString());
            return filePath;
        }

        /// <summary>
        /// Gets the name of the next file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="aDate">a date.</param>
        /// <returns></returns>
        static public String GetNextFileName(String path, DateTime aDate)
        {
            String fDir = Path.GetDirectoryName(path);
            String fExt = Path.GetExtension(path);            
            String fName = path.Replace(fDir, String.Empty).Replace(@"\", String.Empty).Replace(fExt, String.Empty);
            String fDate = aDate.ToString("yyyyMMdd");
            String fSpec = String.Format(@"{0}.{1}.*{2}", fName, fDate, fExt);
            Int32 fNextNum = 1;

            String fPathNoExt = String.Format("{0}.{1}.", path.Replace(fExt, String.Empty), fDate);

            String[] files = Directory.GetFiles(fDir, fSpec, SearchOption.TopDirectoryOnly);
            foreach (String file in files)
            {
                String fileNum = file.Replace(fPathNoExt, String.Empty).Replace(fExt, String.Empty).Trim();
                if (string.IsNullOrWhiteSpace(fileNum))
                {
                    continue;
                }

                if (Int32.TryParse(fileNum, out int thisNum))
                {
                    if (thisNum >= fNextNum)
                    {
                        fNextNum = thisNum + 1;
                    }
                }
            }

            return String.Format(@"{0}\{1}.{2}.{3:0000}{4}", fDir, fName, fDate, fNextNum, fExt);
        }

        /// <summary>
        /// Recycles the name of the and get next file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="sInfo">The s information.</param>
        /// <param name="fileHeader">The file header.</param>
        /// <returns></returns>
        static public String RecycleAndGetNextFileName(String path, RIAutoSaveInfo sInfo, RIFileHeader fileHeader)
        {
            DateTime lastDate;
            if (fileHeader.FLastDateTime == DateTime.MinValue)
            {
                lastDate = DateTime.Now;
            }
            else
            {
                lastDate = fileHeader.FLastDateTime;
            }

            if (sInfo.RecycleFilesEvery > 0)
            {
                String fDir = Path.GetDirectoryName(path);
                String fExt = Path.GetExtension(path);
                String fName = Path.GetFileName(path).Replace(fExt, String.Empty);
                String fSpec = String.Format(@"{0}.*.*{1}", fName, fExt);

                String[] files = Directory.GetFiles(fDir, fSpec, SearchOption.TopDirectoryOnly);
                if (files.Length >= sInfo.RecycleFilesEvery)
                {
                    StringBuilder sbFile = new StringBuilder();
                    SortedDictionary<UInt64, String> sortedFiles = new SortedDictionary<UInt64, String>();

                    foreach (String file in files)
                    {
                        sbFile.Length = 0;
                        sbFile.Append(file);
                        sbFile.Replace(fDir, String.Empty).Replace(fExt, String.Empty).Replace(fName, String.Empty).Replace(".", String.Empty).Replace(@"\", String.Empty);

                        UInt64 key = UInt64.Parse(sbFile.ToString());
                        sortedFiles[key] = file;                        
                    }
                    
                    foreach (UInt64 key in sortedFiles.Keys.ToArray())
                    {
                        File.Delete(sortedFiles[key]);
                        sortedFiles.Remove(key);

                        if (sortedFiles.Count <= sInfo.RecycleFilesEvery)
                        {
                            break;
                        }
                    }
                }
            }

            return GetNextFileName(path, lastDate);
        }

        /// <summary>
        /// Determines whether [is on new day automatic save required] [the specified automatic save].
        /// </summary>
        /// <param name="autoSave">The automatic save.</param>
        /// <param name="fHeader">The f header.</param>
        /// <param name="dt">The dt.</param>
        /// <returns></returns>
        static public Boolean IsOnNewDayAutoSaveRequired(RIAutoSaveInfo autoSave, RIFileHeader fHeader, DateTime dt)
        {
            // see if this message's time stamp exceeds next day, 
            // if so, force an auto save condition is SaveOnNewDay was set
            return (autoSave.SaveOnNewDay && fHeader.FInitDateTime != DateTime.MinValue
                && (dt.ToLocalTime().Date.Subtract(fHeader.FInitDateTime.ToLocalTime().Date).TotalDays >= 1));
        }

        /// <summary>
        /// Shoulds the automatic save.
        /// </summary>
        /// <param name="fileHeader">The file header.</param>
        /// <param name="autoSave">The automatic save.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="onFileSize">Size of the on file.</param>
        /// <param name="messageDateTime">The message date time.</param>
        /// <param name="messageLength">Length of the message.</param>
        /// <returns></returns>
        static public Boolean ShouldAutoSave(RIFileHeader fileHeader, RIAutoSaveInfo autoSave, Stream stream, Int64 onFileSize, DateTime messageDateTime, Int32 messageLength)
        {
            Boolean save = false;

            if (FileHelper.IsOnNewDayAutoSaveRequired(autoSave, fileHeader, messageDateTime)
            || fileHeader.FMessageCount >= autoSave.SaveOnMsgLimit
            || (fileHeader.FMessageCount > 0 && onFileSize > 0 && (messageLength + stream.Length) > onFileSize))
            {
                save = true;
            }

            return save;
        }

        /// <summary>
        /// Determines whether [is file signature valid] [the specified f header].
        /// </summary>
        /// <param name="fHeader">The f header.</param>
        /// <returns></returns>
        static public Boolean IsFileSignatureValid(RIFileHeader fHeader)
        {
            return ArrayConverter.ByteArraysEqual(FileSignature, fHeader.Signature);
        }

        /// <summary>
        /// Validates the filer header.
        /// </summary>
        /// <param name="fHeader">The f header.</param>
        /// <param name="filePath">The file path.</param>
        /// <exception cref="ReflectInsightException"></exception>
        static private void ValidateFilerHeader(RIFileHeader fHeader, String filePath)
        {
            if (!IsFileSignatureValid(fHeader))
            {
                throw new ReflectInsightException(String.Format(CultureInfo.CurrentCulture, "Incorrect file format for '{0}'", filePath));
            }
        }

        /// <summary>
        /// Reads the header.
        /// </summary>
        /// <param name="fStream">The f stream.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="onNewExtraData">The on new extra data.</param>
        /// <returns></returns>
        /// <exception cref="ReflectInsightException"></exception>
        static public RIFileHeader ReadHeader(Stream fStream, String filePath, Dictionary<Int32, FastSerializerObjectData> onNewExtraData)
        {
            RIFileHeader header = null;
            fStream.Seek(0, SeekOrigin.Begin);
            if (fStream.Length != 0)
            {
                try
                {
                    // reset the file pointer and read file header
                    fStream.Seek(0, SeekOrigin.Begin);
                    using (var pool = FastFormatterPool.Pool.Container())
                    {
                        header = pool.Instance.Deserialize<RIFileHeader>(fStream);
                    }

                    ValidateFilerHeader(header, filePath);
                }
                catch (ReflectInsightException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ReflectInsightException(String.Format(CultureInfo.CurrentCulture, "Incorrect file format for '{0}'", filePath), ex);
                }
            }
            else
            {
                header = new RIFileHeader();
                if (onNewExtraData != null)
                {
                    header.CopyExtraData(onNewExtraData);
                }

                using (var pool = FastFormatterPool.Pool.Container())
                {
                    pool.Instance.Serialize(fStream, header);
                }
            }

            return header;
        }

        /// <summary>
        /// Writes the header.
        /// </summary>
        /// <param name="fStream">The f stream.</param>
        /// <param name="header">The header.</param>
        static public void WriteHeader(Stream fStream, RIFileHeader header)
        {
            fStream.Seek(0, SeekOrigin.Begin);
            using (var pool = FastFormatterPool.Pool.Container())
            {
                pool.Instance.Serialize(fStream, header);
            }
        }

        /// <summary>
        /// Gets the install information.
        /// </summary>
        /// <value>
        /// The install information.
        /// </value>
        static public RIInstalledInfo InstallInfo
        {
            get { return FInstalledInfo; }
        }
    }
}
