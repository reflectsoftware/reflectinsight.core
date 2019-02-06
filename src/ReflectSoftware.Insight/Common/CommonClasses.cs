// ReflectInsight.Core
// Copyright (c) 2019 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Drawing;
using Plato.Extensions;
using Plato.Serializers;
using Plato.Serializers.Interfaces;
using ReflectSoftware.Insight.Common.Data;

namespace ReflectSoftware.Insight.Common
{
    public class RIInstalledInfo
	{
        public String AppVersion { get; internal set; }
        public UInt16 AppVersionMajor { get; internal set; }
        public UInt16 AppVersionMinor { get; internal set; }
        public UInt16 BinFileVersionMajor { get; internal set; }
        public UInt16 BinFileVersionMinor { get; internal set; }
        public UInt16 UserBinVersionMajor { get; internal set; }
        public UInt16 UserBinVersionMinor { get; internal set; }
	}


	public class RIAutoSaveInfo : IFastBinarySerializable
	{
		public Boolean SaveOnNewDay;
		public Int32 SaveOnMsgLimit;
        public Int32 SaveOnSize;
		public Int16 RecycleFilesEvery;
        		
		public RIAutoSaveInfo()
		{			
			SaveOnNewDay = false;
			SaveOnMsgLimit = 1000000;
            SaveOnSize = 0;
			RecycleFilesEvery = 0;
		}

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
		{			
			writer.Write(SaveOnNewDay);
			writer.Write(SaveOnMsgLimit);
			writer.Write(RecycleFilesEvery);
            writer.Write(SaveOnSize);
		}

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
		{            
			SaveOnNewDay = reader.ReadBoolean();
			SaveOnMsgLimit = reader.ReadInt32();
			RecycleFilesEvery = reader.ReadInt16();
            SaveOnSize = 0;

            if(reader.AnyObjectDataRemaining())
            {
                SaveOnSize = reader.ReadInt32();
            }
		}
	}

    public class ExtraDataSupport : IFastBinarySerializable
    {
        protected Dictionary<Int32, FastSerializerObjectData> FExtraData;

        public ExtraDataSupport()
        {
            FExtraData = null;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            writer.WriteHashedDirectory<FastSerializerObjectData>(FExtraData);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            FExtraData = reader.ReadHashedDictionary<FastSerializerObjectData>();
        }

        public void ClearExtraData()
        {
            if (FExtraData != null)
                FExtraData = null;
        }

        public void AddExtraData(String dataName, IFastBinarySerializable data)
        {
            if (data == null)
                return;

            if (FExtraData == null)
                FExtraData = new Dictionary<Int32, FastSerializerObjectData>();
            

            FExtraData[(Int32)dataName.ToLower().BKDRHash()] = new FastSerializerObjectData(data);
        }

        public void AddExtraData(IFastBinarySerializable data)
        {
            if (data == null)
                return;

            AddExtraData(data.GetType().Name, data);
        }

        public Boolean HasAnyExtraData
        {
            get { return FExtraData != null && FExtraData.Count > 0; }
        }

        public Boolean HasExtraData(String dataName)
        {
            return FExtraData != null && FExtraData.ContainsKey((Int32)dataName.ToLower().BKDRHash());
        }

        public Boolean HasExtraData<T>()
        {
            return HasExtraData(typeof(T).Name);
        }        

        public T GetExtraData<T>(String dataName) where T : IFastBinarySerializable
        {
            if (!HasExtraData(dataName))
                return default(T);

            return FExtraData[(Int32)dataName.ToLower().BKDRHash()].GetObject<T>();
        }       

        public T GetExtraData<T>() where T : IFastBinarySerializable
        {
            return GetExtraData<T>(typeof(T).Name);
        }

        public virtual Dictionary<Int32, FastSerializerObjectData> CloneExtraData()
        {
            if (FExtraData == null)
                return null;

            Dictionary<Int32, FastSerializerObjectData> rValue = new Dictionary<int, FastSerializerObjectData>();
            foreach (Int32 key in FExtraData.Keys)
            {
                rValue[key] = FExtraData[key];
            }

            return rValue;
        }

        public virtual void CopyExtraData(Dictionary<Int32, FastSerializerObjectData> extraData)
        {
            if (extraData == null)
            {
                FExtraData = null;
                return;
            }

            FExtraData = new Dictionary<Int32, FastSerializerObjectData>(extraData);
        }

        public virtual void CopyExtraData(ExtraDataSupport extraData)
        {
            if (extraData == null)
            {
                FExtraData = null;
                return;
            }

            CopyExtraData(extraData.FExtraData);
        }

        public void AppendExtraData(Dictionary<Int32, FastSerializerObjectData> extraData)
        {
            if (extraData == null)
                return;

            foreach (Int32 key in extraData.Keys)
            {
                FExtraData[key] = extraData[key];
            }
        }
    }


    public class RIFileHeader : ExtraDataSupport, IFastBinarySerializable, ICloneable
	{
		public SerializedVersion FVersion;
		public Byte[] Signature;        
        public String Id;        
        public Int32 FNextSequenceId;        
		public Int32 FMessageCount;        
		public DateTime FInitDateTime;        
		public DateTime FFirstDateTime;        
		public DateTime FLastDateTime;

		public RIFileHeader()
		{
            Clear();            
		}

		public void Clear()
		{
            FVersion = SerializedVersionHelper.CurrentBinFileVersion;
			Signature = FileHelper.FileSignature;
            Reset();

            ClearExtraData();
		}

        public void Reset()
        {
            Id = Guid.NewGuid().ToString();            
            FInitDateTime = DateTime.MinValue;
            FFirstDateTime = DateTime.MinValue;
            FLastDateTime = DateTime.MinValue;
            FNextSequenceId = 1;
            FMessageCount = 0;
        }

        public Int32 GetCurrentSequenceId()
        {
            return FNextSequenceId;
        }

        public Int32 GetNextSequenceId()
        {
            return FNextSequenceId++;
        }

        public override void WriteData(FastBinaryWriter writer, Object additionalInfo)
		{
            base.WriteData(writer, additionalInfo);

            writer.Write(SerializedVersionHelper.CurrentBinFileVersion);
			writer.WriteByteArray(Signature);
            writer.Write(Id);
			writer.Write(FInitDateTime);
			writer.Write(FFirstDateTime);
			writer.Write(FLastDateTime);
            writer.Write(FNextSequenceId);
			writer.Write(FMessageCount);            
		}

        public override void ReadData(FastBinaryReader reader, Object additionalInfo)
		{
            base.ReadData(reader, additionalInfo);

            FVersion = reader.ReadObject<SerializedVersion>();
			Signature = reader.ReadByteArray();
            Id = reader.ReadString();
			FInitDateTime = reader.ReadDateTime();
			FFirstDateTime = reader.ReadDateTime();
			FLastDateTime = reader.ReadDateTime();
            FNextSequenceId = reader.ReadInt32();
			FMessageCount = reader.ReadInt32();            
		}

        public Object Clone()
        {
            RIFileHeader rValue = new RIFileHeader();

            rValue.FVersion = FVersion;
            rValue.Signature = Signature;
            rValue.Id = Id;
            rValue.FInitDateTime = FInitDateTime;
            rValue.FFirstDateTime = FFirstDateTime;
            rValue.FLastDateTime = FLastDateTime;
            rValue.FNextSequenceId = FNextSequenceId;
            rValue.FMessageCount = FMessageCount;
            rValue.FExtraData = CloneExtraData();
            
            return rValue;
        }
	}

	public class RIInstance
	{
		public String Name { get; internal set; }
		public String Category { get; internal set; }
		public String BkColor { get; internal set; }
        public String DestinationBindingGroup { get; internal set; }

        public RIInstance(String name, String category, String bkColor, String destinationBinding)
        {
            Name = name;
            BkColor = bkColor;
            Category = category.IfNullOrEmptyUseDefault(name);
            DestinationBindingGroup = destinationBinding;
        }

        public RIInstance(String name, String category, String bkColor)
            : this(name, category, bkColor, String.Empty)
		{
		}

		public RIInstance(String name, String category): this(name, category, String.Empty)
		{
		}

		public RIInstance(String name): this(name, name)
		{
		}
	}

	public class ReflectInsightPackageArgs : EventArgs
	{
		public readonly ReflectInsightPackage UserPackage;
		public Boolean Cancel;


		public ReflectInsightPackageArgs(ReflectInsightPackage userPackage)
		{
			UserPackage = userPackage;
			Cancel = false;
		}
	}

	static public class RIPastelBackColor
	{
		static public String[] GetColorNames()
		{
			return new String[] 
			{
				"Light Yellow",
				"Dark Yellow",
				"Light Salmon",
				"Dark Salmon",
				"Light Lavender",
				"Dark Lavender",
				"Light Green",
				"Dark Green",
				"Light Blue",
				"Dark Blue",
				"Light Gray",
				"Dark Gray",
				"Light Gold Gray",
				"Dark Gold Gray",
				"Light Orange",
				"Dark Orange"
			};                 
		}

		static public Color GetColorByName(String colorName)
		{
            Color rValue = Color.White;
			colorName = colorName.Replace(" ", String.Empty).ToLower();
            if (string.IsNullOrWhiteSpace(colorName))
            {
                return rValue;
            }

            if (String.Compare(colorName[0].ToString(), '#'.ToString(), false) == 0)
			{
                colorName = colorName.Replace("#", String.Empty);

                if (Int32.TryParse(colorName, System.Globalization.NumberStyles.HexNumber, null, out int argb))
                {
                    rValue = Color.FromArgb(argb);
                }
            }
			else
			{
				switch (colorName)
				{
					case "lightyellow": rValue = Color.FromArgb(255, 255, 206); break;
                    case "darkyellow": rValue = Color.FromArgb(255, 255, 166); break;
                    case "lightsalmon": rValue = Color.FromArgb(255, 234, 234); break;
                    case "darksalmon": rValue = Color.FromArgb(255, 213, 213); break;
                    case "lightlavender": rValue = Color.FromArgb(236, 236, 255); break;
                    case "darklavender": rValue = Color.FromArgb(215, 215, 255); break;
                    case "lightgreen": rValue = Color.FromArgb(234, 255, 234); break;
                    case "darkgreen": rValue = Color.FromArgb(183, 255, 183); break;
                    case "lightblue": rValue = Color.FromArgb(241, 253, 254); break;
                    case "darkblue": rValue = Color.FromArgb(196, 247, 251); break;
                    case "lightgray": rValue = Color.FromArgb(243, 243, 243); break;
                    case "darkgray": rValue = Color.FromArgb(232, 232, 232); break;
                    case "lightgoldgray": rValue = Color.FromArgb(241, 241, 226); break;
                    case "darkgoldgray": rValue = Color.FromArgb(232, 232, 206); break;
                    case "lightorange": rValue = Color.FromArgb(251, 235, 206); break;
                    case "darkorange": rValue = Color.FromArgb(247, 241, 153); break;
					case "black": rValue = Color.Black; break;
					default:
						{
							Color tmpColor = Color.FromName(colorName);
							if (tmpColor.ToArgb() != Color.Black.ToArgb())
							{
								// any other color than black would mean that the color name doesn't exist
								rValue = tmpColor;
							}
						}
						break;
				}
			}

			return rValue;
		}

        static public Color[] GetColors()
		{
			List<Color> colors = new List<Color>();
			foreach (String name in GetColorNames())
			{
				colors.Add(GetColorByName(name));
			}

			return colors.ToArray();
		}

		static public Int32[] GetInt32Colors()
		{
			List<Int32> colors = new List<Int32>();
            foreach (Color color in GetColors())
			{
                colors.Add(color.ToArgb());
			}

			return colors.ToArray();            
		}
        
        static public Int32 ToNonArgb(this Color clr)
        {
            return (clr.R << 16) + (clr.G << 8) + clr.B;
        }

		static public Color LightYellow { get { return GetColorByName("LightYellow"); } }
		static public Color DarkYellow { get { return GetColorByName("DarkYellow"); } }
		static public Color LightSalmon { get { return GetColorByName("LightSalmon"); } }
		static public Color DarkSalmon { get { return GetColorByName("DarkSalmon"); } }
		static public Color LightLavender { get { return GetColorByName("LightLavender"); } }
		static public Color DarkLavender { get { return GetColorByName("DarkLavender"); } }
		static public Color LightGreen { get { return GetColorByName("LightGreen"); } }
		static public Color DarkGreen { get { return GetColorByName("DarkGreen"); } }
		static public Color LightBlue { get { return GetColorByName("LightBlue"); } }
		static public Color DarkBlue { get { return GetColorByName("DarkBlue"); } }
		static public Color LightGray { get { return GetColorByName("LightGray"); } }
		static public Color DarkGray { get { return GetColorByName("DarkGray"); } }
		static public Color LightGoldGray { get { return GetColorByName("LightGoldGray"); } }
		static public Color DarkGoldGray { get { return GetColorByName("DarkGoldGray"); } }
		static public Color LightOrange { get { return GetColorByName("LightOrange"); } }
		static public Color DarkOrange { get { return GetColorByName("DarkOrange"); } }
	}
}
