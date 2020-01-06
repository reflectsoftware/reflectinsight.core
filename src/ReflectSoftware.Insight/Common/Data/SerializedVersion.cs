// ReflectInsight.Core
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Plato.Serializers;
using Plato.Serializers.Interfaces;
using System;

namespace ReflectSoftware.Insight.Common.Data
{
    public class SerializedVersion : IFastBinarySerializable
    {
        public UInt16 VersionMajor { get; set; }
        public UInt16 VersionMinor { get; set; }

        public SerializedVersion(UInt16 major, UInt16 minor)
        {
            VersionMajor = major;
            VersionMinor = minor;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.Write(VersionMajor);
            writer.Write(VersionMinor);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            VersionMajor = reader.ReadUInt16();
            VersionMinor = reader.ReadUInt16();
        }

        public Boolean IsVersionEqualTo(SerializedVersion version)
        {
            return VersionMajor == version.VersionMajor && VersionMinor == version.VersionMinor;
        }

        public Boolean IsVersionGreaterThan(SerializedVersion version)
        {
            return (VersionMajor > version.VersionMajor) || (VersionMajor == version.VersionMajor && VersionMinor > version.VersionMinor);
        }

        public Boolean IsVersionLessThan(SerializedVersion version)
        {
            return !IsVersionEqualTo(version) && !IsVersionGreaterThan(version);
        }
    }


    public class SerializedVersionHelper
    {
        static public SerializedVersion CurrentAppVersion { get; private set; }
        static public SerializedVersion CurrentBinFileVersion { get; private set; }
        static public SerializedVersion CurrentUserBinVersion { get; private set; }

        static SerializedVersionHelper()
        {
            CurrentAppVersion = new SerializedVersion(FileHelper.REFLECTINSIGHT_APP_VERSION_MAJOR, FileHelper.REFLECTINSIGHT_APP_VERSION_MINOR);
            CurrentBinFileVersion = new SerializedVersion(FileHelper.REFLECTINSIGHT_BIN_FILE_VERSION_MAJOR, FileHelper.REFLECTINSIGHT_BIN_FILE_VERSION_MINOR);
            CurrentUserBinVersion = new SerializedVersion(FileHelper.REFLECTINSIGHT_USER_BIN_VERSION_MAJOR, FileHelper.REFLECTINSIGHT_USER_BIN_VERSION_MINOR);
        }
    }
}
