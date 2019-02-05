using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Plato.Serializers.Interfaces;
using Plato.Serializers;
using Plato.Extensions;
using Plato.Serializers.FormatterPools;

namespace ReflectSoftware.Insight.Common.Data
{
    public enum RICustomDataElementType
    {
        /// <summary>
        /// A field
        /// </summary>
        Field,
        /// <summary>
        /// A row
        /// </summary>
        Row,
        /// <summary>
        /// A category
        /// </summary>
        Category,
        /// <summary>
        /// A container
        /// </summary>
        Container
    }

    public enum RICustomDataColumnJustificationType
    {
        /// <summary>
        /// Left
        /// </summary>
        Left,
        /// <summary>
        /// Center
        /// </summary>
        Center,
        /// <summary>
        /// Right
        /// </summary>
        Right
    }

    public enum RICustomDataFieldIdType: int
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// Integer
        /// </summary>
        Integer,
        /// <summary>
        /// Decimal
        /// </summary>
        Decimal,
        /// <summary>
        /// String
        /// </summary>
        String,
        /// <summary>
        /// DateTime
        /// </summary>
        DateTime,
        /// <summary>
        /// Boolean
        /// </summary>
        Boolean,
        /// <summary>
        /// Object
        /// </summary>
        Object
    }

    public class RICustomDataColumn: IFastBinarySerializable
    {
        public String Caption { get; internal set; }
        public String Format { get; set; }
        public String NullText { get; set; }
        public RICustomDataFieldIdType FieldTypeId { get; protected set; }
        public RICustomDataColumnJustificationType Justification { get; protected set; }
                       
        public RICustomDataColumn(String caption, RICustomDataFieldIdType fieldTypeId, String format, String nullText, RICustomDataColumnJustificationType justification)
        {            
            FieldTypeId = fieldTypeId;
            Justification = justification;
            Caption = caption;
            Format = format ?? "{0}";
            NullText = nullText ?? "(null)";

            if (fieldTypeId == RICustomDataFieldIdType.DateTime && String.Compare(Format, "{0}", false) == 0)
                Format = String.Empty;
        }
       

        public RICustomDataColumn(String caption, RICustomDataFieldIdType fieldTypeId, String format, String nullText): this(caption, fieldTypeId, format, nullText, RICustomDataColumnJustificationType.Left)
        {
        }

        public RICustomDataColumn(String caption, RICustomDataFieldIdType fieldTypeId, String format): this(caption, fieldTypeId, format, null, RICustomDataColumnJustificationType.Left)
        {
        }

        public RICustomDataColumn(String caption, RICustomDataFieldIdType fieldTypeId): this(caption, fieldTypeId, null, null, RICustomDataColumnJustificationType.Left)
        {
        }

        public RICustomDataColumn(String caption): this(caption, RICustomDataFieldIdType.String, null, null, RICustomDataColumnJustificationType.Left)
        {
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {            
            writer.Write(FieldTypeId.GetHashCode());
            writer.Write(Justification.GetHashCode());
            writer.WriteSafeString(Caption);
            writer.WriteSafeString(Format);
            writer.WriteSafeString(NullText);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {            
            FieldTypeId = (RICustomDataFieldIdType)reader.ReadInt32();
            Justification = (RICustomDataColumnJustificationType)reader.ReadInt32();
            Caption = reader.ReadSafeString();
            Format = reader.ReadSafeString();
            NullText = reader.ReadSafeString();
        }
        
        public override String ToString()
        {
            return Caption;
        }
    }


    public abstract class RICustomDataElement: IFastBinarySerializable
    {        
        static protected Int32 RICustomDataFieldTypeHash;     
        static protected Int32 RICustomDataRowTypeHash;        
        static protected Int32 RICustomDataCategoryTypeHash;        
        static protected Int32 RICustomDataContainerTypeHash;

        public RICustomDataElementType CustomDataType { get; internal set; }
        public Int16 Level { get; internal set; }

        // don't serialize
        public RICustomData Root { get; internal set; }
        public Int32 Id { get; internal set; }
        
        static RICustomDataElement()
        {
            RICustomDataFieldTypeHash = (Int32)typeof(RICustomDataField).Name.BKDRHash();
            RICustomDataRowTypeHash = (Int32)typeof(RICustomDataRow).Name.BKDRHash();
            RICustomDataCategoryTypeHash = (Int32)typeof(RICustomDataCategory).Name.BKDRHash();
            RICustomDataContainerTypeHash = (Int32)typeof(RICustomData).Name.BKDRHash();
        }

        internal RICustomDataElement(RICustomData root, RICustomDataElementType cType, Int16 level)
        {            
            Root = root;
            CustomDataType = cType;
            Level = level;
        }

        public virtual void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {            
            writer.Write(CustomDataType.GetHashCode());
            writer.Write(Level);
        }

        public virtual void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            Root = (RICustomData)reader.Tag;            
            CustomDataType = (RICustomDataElementType)reader.ReadInt32();
            Level = reader.ReadInt16();
        }

        public abstract String[] ToStringArray();
    }

    public class RICustomDataField : RICustomDataElement
    {
        public Object Value { get; set; }
        public Int16 ColumnPos { get; private set; }

        // don't serialize
        public RICustomDataColumn Column { get; private set; }
        
        internal RICustomDataField(RICustomData root, Object value, Int16 cPos, Int16 level): base(root, RICustomDataElementType.Field, level)
        {
            Value = value;            
            ColumnPos = cPos;
            Column = root.Columns[cPos];
        }

        public override void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            base.WriteData(writer, additionalInfo);

            // Hint is no longer used. This line is left here for backward compatibility
            writer.WriteSafeString(null);

            writer.Write(ColumnPos);

            if (writer.WriteNullState(Value))
            {
                switch (Column.FieldTypeId)
                {
                    case RICustomDataFieldIdType.Decimal: writer.Write(Decimal.Parse(Value.ToString())); break;                    
                    case RICustomDataFieldIdType.Integer: writer.Write(Int64.Parse(Value.ToString())); break;                    
                    case RICustomDataFieldIdType.DateTime: writer.Write((DateTime)Value); break;                    
                    case RICustomDataFieldIdType.Boolean: writer.Write((Boolean)Value); break;
                    case RICustomDataFieldIdType.String: writer.Write(Value.ToString()); break;
                    case RICustomDataFieldIdType.Object: writer.Write(Value.ToString()); break;
                    case RICustomDataFieldIdType.Unknown: writer.Write(Value.ToString()); break;
                }
            }
        }

        public override void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            Root = (RICustomData)reader.Tag;

            base.ReadData(reader, additionalInfo);

            // Hint is no longer used. This line is left here for backward compatibility
            reader.ReadSafeString(); 

            ColumnPos = reader.ReadInt16();
            Column = Root.Columns[ColumnPos];            
            Value = null;

            if (!reader.IsNull())
            {
                switch (Column.FieldTypeId)
                {
                    case RICustomDataFieldIdType.Decimal: Value = reader.ReadDecimal(); break;
                    case RICustomDataFieldIdType.Integer: Value = reader.ReadInt64(); break;
                    case RICustomDataFieldIdType.DateTime: Value = reader.ReadDateTime(); break;
                    case RICustomDataFieldIdType.Boolean: Value = reader.ReadBoolean(); break;
                    case RICustomDataFieldIdType.String: Value = reader.ReadString(); break;
                    case RICustomDataFieldIdType.Object: Value = reader.ReadString(); break;
                    case RICustomDataFieldIdType.Unknown: Value = reader.ReadString(); break;
                    default: break;
                }
            }
        }

        public override String ToString()
        {
            if (Value != null)
            {
                switch (Column.FieldTypeId)
                {
                    case RICustomDataFieldIdType.DateTime: return DateTime.Parse(Value.ToString()).ToString(Column.Format);
                    default: return String.Format(Column.Format, Value);                        
                }
            }

            return Column.NullText;
        }
        
        public override String[] ToStringArray()
        {
            return new String[] { ToString() };
        }
    }

    public class RICustomDataRow : RICustomDataElement
    {
        private FastSerializerObjectData FExtraData;        
        private Int32 FExtraDataTypeHash;        
        private Object FTag;

        public List<RICustomDataField> Fields { get; internal set; }
        
        // non-serialize 

        internal RICustomDataRow(RICustomData root, Int16 level): base(root, RICustomDataElementType.Row, level)
        {            
            Fields = new List<RICustomDataField>();
            ClearExtraData();
        }        

        public override void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            base.WriteData(writer, additionalInfo);

            writer.Write(FExtraData);
            writer.Write(FExtraDataTypeHash);
            writer.Write(Fields.ToArray());
        }

        public override void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            Root = (RICustomData)reader.Tag;
            base.ReadData(reader, additionalInfo);

            FExtraData = reader.ReadObject<FastSerializerObjectData>();
            FExtraDataTypeHash = reader.ReadInt32();
            Fields = new List<RICustomDataField>();
            Fields.AddRange(reader.ReadEnumerable<RICustomDataField>());
        }

        public void ClearExtraData()
        {
            FExtraData = null;
            FExtraDataTypeHash = 0;
        }

        public void SetTag(Object data)
        {
            FTag = data;
        }

        public T GetTag<T>()
        {
            return (T)FTag;
        }

        public void SetExtraData(IFastBinarySerializable data)
        {
            FExtraData = null;
            FExtraDataTypeHash = 0;

            if (data != null)
            {                
                FExtraData = new FastSerializerObjectData(data);
                FExtraDataTypeHash = (Int32)data.GetType().Name.BKDRHash();
            }
        }

        public void SetExtraData(FastBinaryFormatter ff, IFastBinarySerializable data)
        {
            FExtraData = null;
            FExtraDataTypeHash = 0;

            if (data != null)
            {
                FExtraData = new FastSerializerObjectData(ff, data);
                FExtraDataTypeHash = (Int32)data.GetType().Name.BKDRHash();
            }
        }

        public T GetExtraData<T>() where T: IFastBinarySerializable
        {
            if (FExtraData == null)
                return default(T);

            return FExtraData.GetObject<T>();
        }

        public T GetExtraData<T>(FastBinaryFormatter ff) where T : IFastBinarySerializable
        {
            if (FExtraData == null)
                return default(T);

            return FExtraData.GetObject<T>(ff);
        }

        public Boolean IsExtraDataType(Type oType)
        {
            return FExtraDataTypeHash == (Int32)oType.Name.BKDRHash();
        }

        public Boolean HasExtraData
        {
            get { return FExtraData != null; }
        }

        public void CopyExtraData(RICustomDataRow row)
        {            
            FExtraData = row.FExtraData;
            FExtraDataTypeHash = row.FExtraDataTypeHash;
        }

        public RICustomDataField AddField(Object value) 
        {            
            RICustomDataField field = new RICustomDataField(Root, value, (Int16)Fields.Count, Level);
            Fields.Add(field);

            return field;
        }

        public RICustomDataField[] AddFields(params Object[] values)
        {
            RICustomDataField[] fields = new RICustomDataField[values.Length];
            for (Int32 i = 0; i < values.Length; i++)
            {
                fields[i] = new RICustomDataField(Root, values[i], (Int16)i, Level);
            }

            Fields.AddRange(fields);

            return fields;
        }

        public RICustomDataField this[Int32 fieldIdx]
        {
            get { return Fields[fieldIdx]; }
        }

        public override String[] ToStringArray()
        {
            String[] fields = new String[Fields.Count];
            for (Int32 i = 0; i < Fields.Count; i++)
                fields[i] = Fields[i].ToString();

            return fields;
        }
    }

    public class RICustomDataCategory : RICustomDataElement
    {    
        public String Caption { get; internal set; }
        public Boolean Expanded { get; set; }
        public List<RICustomDataElement> Children { get; internal set; }

        internal RICustomDataCategory(RICustomData root, String caption, RICustomDataElementType cType, Int16 level): base(root, cType, level)
        {
            Root = root;
            Caption = caption;
            Expanded = true;
            Children = new List<RICustomDataElement>();

            if (Root != null)
            {
                if ((level+1) > root.MaxCategoryLevels)
                    root.MaxCategoryLevels = (Int16)(level+1);
            }
        }

        internal RICustomDataCategory(RICustomData root, String caption, Int16 level): this(root, caption, RICustomDataElementType.Category, level)
        {
        }

        public override void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {
            base.WriteData(writer, additionalInfo);

            writer.Write(Expanded);
            writer.WriteSafeString(Caption);

            // write children
            writer.Write(Children.Count);
            foreach (RICustomDataElement element in Children)
            {
                Int32 elementTypeHash = RICustomDataFieldTypeHash;
                if (element.CustomDataType == RICustomDataElementType.Row)
                {
                    elementTypeHash = RICustomDataRowTypeHash;
                }
                else if (element.CustomDataType == RICustomDataElementType.Category)
                {
                    elementTypeHash = RICustomDataCategoryTypeHash;
                }
                else if (element.CustomDataType == RICustomDataElementType.Container)
                {
                    elementTypeHash = RICustomDataContainerTypeHash;
                }

                writer.Write(elementTypeHash);
                writer.Write(element);
            }
        }

        public override void ReadData(FastBinaryReader reader, Object additionalInfo)
        {
            Root = (RICustomData)reader.Tag;
            base.ReadData(reader, additionalInfo);
                        
            Expanded = reader.ReadBoolean();
            Caption = reader.ReadSafeString();

            Children = new List<RICustomDataElement>();
            Int32 elementCount = reader.ReadInt32();
            for (Int32 i = 0; i < elementCount; i++)
            {
                Int32 elementTypeHash = reader.ReadInt32();
                if (elementTypeHash == RICustomDataFieldTypeHash)
                {
                    Children.Add(reader.ReadObject<RICustomDataField>());
                }
                else if (elementTypeHash == RICustomDataRowTypeHash)
                {
                    Children.Add(reader.ReadObject<RICustomDataRow>());
                }
                else if (elementTypeHash == RICustomDataCategoryTypeHash)
                {
                    Children.Add(reader.ReadObject<RICustomDataCategory>());
                }
                else if (elementTypeHash == RICustomDataContainerTypeHash)
                {
                    Children.Add(reader.ReadObject<RICustomData>());
                }
            }
        }

        public RICustomDataCategory AddCategory(String caption)
        {
            RICustomDataCategory cat = new RICustomDataCategory(Root, caption, (Int16)(Level+1));
            Children.Add(cat);

            return cat;
        }

        public RICustomDataRow AddRow()
        {
            RICustomDataRow row = new RICustomDataRow(Root, (Int16)(Level+1));
            Children.Add(row);

            return row;
        }

        public RICustomDataRow AddRow(params Object[] values)
        {
            RICustomDataRow row = new RICustomDataRow(Root, (Int16)(Level+1));
            Children.Add(row);

            row.AddFields(values);

            return row;
        }

        private Boolean RemoveElement(List<RICustomDataElement> children, RICustomDataElement element)
        {
            foreach (RICustomDataElement elm in children)
            {
                if (Object.ReferenceEquals(elm, element))
                {
                    Children.Remove(elm);
                    return true;
                }
                else if (elm.CustomDataType == RICustomDataElementType.Category || elm.CustomDataType == RICustomDataElementType.Container)
                {
                    if (RemoveElement((elm as RICustomDataCategory).Children, element))
                        return true;
                }
            }

            return false;
        }

        public Boolean RemoveElement(RICustomDataElement element)
        {
            return RemoveElement(Children, element);
        }

        public Int32 TopCategoryCount
        {
            get
            {
                Int32 count = 0;
                foreach (RICustomDataElement ce in Children)
                {
                    if (ce.CustomDataType == RICustomDataElementType.Category)
                        count++;
                }

                return count;
            }
        }

        public Int32 TopRowCount
        {
            get
            {
                Int32 count = 0;
                foreach (RICustomDataElement ce in Children)
                {
                    if (ce.CustomDataType == RICustomDataElementType.Row)
                        count++;
                }

                return count;
            }
        }

        public String[][] TopRowToStringArray()
        {
            String[][] rows = new String[TopRowCount][];

            Int32 index = 0;
            foreach(RICustomDataElement ce in Children)
            {
                if (ce.CustomDataType == RICustomDataElementType.Row)
                {
                    rows[index++] = ((RICustomDataRow)ce).ToStringArray();
                }
            }

            return rows;
        }

        public T[] TopRowToExtraDataArray<T>() where T: IFastBinarySerializable
        {
            T[] extraDataArray = new T[TopRowCount];

            using (var pool = FastFormatterPool.Pool.Container())
            {
                Int32 index = 0;
                foreach (RICustomDataElement ce in Children)
                {
                    if (ce.CustomDataType == RICustomDataElementType.Row)
                    {
                        extraDataArray[index++] = ((RICustomDataRow)ce).GetExtraData<T>(pool.Instance);
                    }
                }

                return extraDataArray;
            }
        }

        public override String[] ToStringArray()
        {
            return new String[] { Caption };
        }
    }


    public class RICustomData : RICustomDataCategory
    {
        public RICustomDataColumn[] Columns { get; internal set; }
        public Boolean ShowColumns { get; set; }
        public Boolean AllowSort { get; set; }
        public Boolean IsPropertyGrid { get; internal set; }
        public Boolean HasDetails { get; set; }
        public Int16 MaxCategoryLevels { get; internal set; }

        private void Init(IEnumerable<RICustomDataColumn> columns, Boolean bShowColumns, Boolean isPropertyGrid)
        {
            Root = this;
            Level = -1;
            Columns = columns.ToArray();
            ShowColumns = bShowColumns;
            IsPropertyGrid = isPropertyGrid;
            HasDetails = false;
            AllowSort = true;

            if (Columns.Length == 0)
            {
                Columns = new RICustomDataColumn[] { new RICustomDataColumn(String.Empty) };
            }
        }       

        public RICustomData(String caption, IEnumerable<RICustomDataColumn> columns, Boolean bShowColumns, Boolean isPropertyGrid): base(null, caption, RICustomDataElementType.Container, 0)
        {
            Init(columns.ToArray(), bShowColumns, isPropertyGrid);
        }

        public RICustomData(String caption, IEnumerable<RICustomDataColumn> columns): this(caption, columns, false, false)
        {
        }

        public RICustomData(String caption, String[] columns, Boolean bShowColumns, Boolean isPropertyGrid): base(null, caption, RICustomDataElementType.Container, 0)
        {
            List<RICustomDataColumn> riColumns = null;
            if (columns != null)
            {
                riColumns = new List<RICustomDataColumn>();
                foreach (String column in columns)
                    riColumns.Add(new RICustomDataColumn(column));
            }
            
            Init(riColumns.ToArray(), bShowColumns, isPropertyGrid);
        }

        public RICustomData(String caption, String[] columns): this(caption, columns, false, false)
        {
        }

        public RICustomData(String caption, Boolean bShowColumns, Boolean isPropertyGrid): base(null, caption, RICustomDataElementType.Container, 0)
        {
            RICustomDataColumn[] columns = new RICustomDataColumn[] { new RICustomDataColumn(String.Empty) };
            Init(columns, bShowColumns, isPropertyGrid);
        }

        public RICustomData(String caption): this(caption, false, false)
        {
        }

        private static void GetExpandStates(List<RICustomDataElement> children, Hashtable expandedStates)
        {
            foreach (RICustomDataElement element in children)
            {
                if (element.CustomDataType == RICustomDataElementType.Category)
                {
                    RICustomDataCategory cat = (RICustomDataCategory)element;

                    Int32 hashId = (Int32)String.Format("{0}:{1}", cat.Caption, cat.Level).BKDRHash();
                    expandedStates[hashId] = cat.Expanded;

                    GetExpandStates(cat.Children, expandedStates);
                }
            }
        }
        
        public void GetExpandStates(Hashtable expandedStates)
        {            
            GetExpandStates(Children, expandedStates);
        }
        
        private static void AssignExpandedStates(List<RICustomDataElement> children, Hashtable expandedStates, ref Int32 id)
        {
            foreach (RICustomDataElement element in children)
            {
                element.Id = ++id;
                if (element.CustomDataType == RICustomDataElementType.Category)
                {
                    RICustomDataCategory cat = (RICustomDataCategory)element;

                    if (expandedStates != null)
                    {
                        Int32 hashId = (Int32)String.Format("{0}:{1}", cat.Caption, cat.Level).BKDRHash();
                        if (expandedStates.ContainsKey(hashId))
                            cat.Expanded = (Boolean)expandedStates[hashId];
                    }

                    AssignExpandedStates(cat.Children, expandedStates, ref id);
                }
            }
        }

        public void AssignExpandedStates(Hashtable expandedStates)
        {
            Int32 id = 0;
            AssignExpandedStates(Children, expandedStates, ref id);
        }

        public override void WriteData(FastBinaryWriter writer, Object additionalInfo)
        {            
            writer.Write(Columns);            
            writer.Write(ShowColumns);
            writer.Write(AllowSort);
            writer.Write(IsPropertyGrid);
            writer.Write(HasDetails);
            writer.Write(MaxCategoryLevels);

            base.WriteData(writer, additionalInfo);
        }

        public override void ReadData(FastBinaryReader reader, Object additionalInfo)
        {            
            Root = this;
            Columns = reader.ReadEnumerable<RICustomDataColumn>().ToArray();
            ShowColumns = reader.ReadBoolean();
            AllowSort = reader.ReadBoolean();
            IsPropertyGrid = reader.ReadBoolean();
            HasDetails = reader.ReadBoolean();
            MaxCategoryLevels = reader.ReadInt16();

            Object tag = reader.Tag;
            reader.Tag = Root;

            base.ReadData(reader, additionalInfo);
            reader.Tag = tag;
        }        

        public RICustomData Copy()
        {
            RICustomData copy = new RICustomData(Caption, new List<RICustomDataColumn>(Columns), ShowColumns, IsPropertyGrid)
            { 
                AllowSort = AllowSort,
                HasDetails = HasDetails,
                MaxCategoryLevels = MaxCategoryLevels,
                Expanded = Expanded
            };
            copy.Children.AddRange(Children);

            return copy;
        }
    }
}
