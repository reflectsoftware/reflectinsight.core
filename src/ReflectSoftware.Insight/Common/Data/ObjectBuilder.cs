using Plato.Serializers.FormatterPools;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectSoftware.Insight.Common.Data
{
    [Flags]
    public enum ObjectFieldStates
    {
        /// <summary>
        /// The is none
        /// </summary>
        IsNone = 0,
        /// <summary>
        /// The is static
        /// </summary>
        IsStatic = 1,
        /// <summary>
        /// The is a property
        /// </summary>
        IsProperty = 2,
        /// <summary>
        /// The is an indexer
        /// </summary>
        IsIndexer = 4,
        /// <summary>
        /// The is public
        /// </summary>
        IsPublic = 8,
        /// <summary>
        /// The is private
        /// </summary>
        IsPrivate = 16,
        /// <summary>
        /// The is protected
        /// </summary>
        IsProtected = 32,
    }

    public class ObjectBuilder
    {
        class ListNode
        {
            public String FName;
            public String FValue;
            public ObjectFieldStates FStates;
                
            protected static String GetObjectValue(Object obj)
            {
                String rValue = "(null)";
                if (obj != null)
                {
                    Type oType = obj.GetType();
                    rValue = obj.ToString();

                    // 4/18/14 MRC Changed to String.Compare vs. ==
                    if (String.Compare(rValue, oType.Name, false) == 0)
                        rValue = oType.FullName;
                }

                return rValue;
            }
            
            public ListNode(FieldInfo field, Object obj)
            {
                FName = field.Name;
                FStates = ObjectFieldStates.IsNone;
                if (field.IsStatic)
                {
                    FStates |= ObjectFieldStates.IsStatic;
                }

                if (field.IsPublic)
                {
                    FStates |= ObjectFieldStates.IsPublic;
                }
                else if (field.IsPrivate)
                {
                    FStates |= ObjectFieldStates.IsPrivate;
                }
                else
                {
                    FStates |= ObjectFieldStates.IsProtected;
                }

                FValue = GetObjectValue(field.GetValue(obj));                
            }

            public ListNode(PropertyInfo prop, Object obj)
            {
                FName = prop.Name;
                FStates = ObjectFieldStates.IsProperty;

                MethodInfo mInfo = prop.GetGetMethod(true);
                if (mInfo.IsStatic)
                {
                    FStates |= ObjectFieldStates.IsStatic;
                }

                if (mInfo.IsPublic)
                {
                    FStates |= ObjectFieldStates.IsPublic;
                }
                else if (mInfo.IsPrivate)
                {
                    FStates |= ObjectFieldStates.IsPrivate;
                }
                else
                {
                    FStates |= ObjectFieldStates.IsProtected;
                }

                ParameterInfo[] pInfos = prop.GetIndexParameters();
                if (pInfos.Length > 0)
                {
                    FValue = String.Format("[{0}] : {1}", pInfos[0].ParameterType.FullName, mInfo.ReturnType.FullName);
                    FStates |= ObjectFieldStates.IsIndexer;
                }
                else
                {
                    try
                    {
                        FValue = GetObjectValue(prop.GetValue(obj, null));
                    }
                    catch (Exception)
                    {
                        FValue = "<not accessible>";
                    }
                }
            }
        }
    
        internal ObjectBuilder() {}

        private static void UpdateCustomData(RICustomData cData, String groupName, List<ListNode> list)
        {
            if (list.Count == 0) return;

            RICustomDataCategory cat = cData.AddCategory(groupName);

            using (var pool = FastFormatterPool.Pool.Container())
            {
                foreach (ListNode node in list)
                {
                    RICustomDataRow row = cat.AddRow();
                    row.AddField(node.FName);                    
                    row.AddField(node.FValue);

                    row.SetExtraData(pool.Instance, new DetailContainerInt32(node.FStates.GetHashCode()));
                }
            }
        }
        
        public static RICustomData BuildObjectPropertyMap(Object obj, ObjectScope scope)
        {
            if (obj == null)
            {
                throw new NullReferenceException("BuildObjectPropertyMap: Object cannot be null.");
            }

            // Scope must contain one or more of the following
            // enumerated ObjectScope values: Public, Protected, Private and/or All            
            if (scope > ObjectScope.All)
            {
                throw new ArgumentException("BuildObjectPropertyMap: Invalid ObjectScope defined. Scope must have one or more of the following: Public, Protected, Private and/or All");
            }

            BindingFlags bindings = BindingFlags.Static | BindingFlags.Instance;
            if ((scope & ObjectScope.Public) != ObjectScope.None)
                bindings |= BindingFlags.Public;

            if (((scope & ObjectScope.Protected) != ObjectScope.None)
            || ((scope & ObjectScope.Private) != ObjectScope.None))
                bindings |= BindingFlags.NonPublic;

            List<ListNode> privateList = new List<ListNode>();
            List<ListNode> protectedList = new List<ListNode>();
            List<ListNode> publicList = new List<ListNode>();

            Type typ = obj.GetType();

            // Get FieldInfo
            foreach (FieldInfo field in typ.GetFields(bindings))
            {
                if (field.IsPublic)
                {
                    publicList.Add(new ListNode(field, obj));
                }
                else if (field.IsPrivate && (scope & ObjectScope.Private) != ObjectScope.None)
                {
                    privateList.Add(new ListNode(field, obj));
                }
                else if ((scope & ObjectScope.Protected) != ObjectScope.None)
                {
                    protectedList.Add(new ListNode(field, obj));
                }
            }

            // Get PropertyInfo with at least a GetMethod
            foreach (PropertyInfo prop in typ.GetProperties(bindings))
            {
                if (!prop.CanRead) continue;

                MethodInfo mInfo = prop.GetGetMethod(true);
                if (mInfo.IsPublic)
                {
                    publicList.Add(new ListNode(prop, obj));
                }
                else if (mInfo.IsPrivate && (scope & ObjectScope.Private) != ObjectScope.None)
                {
                    privateList.Add(new ListNode(prop, obj));
                }
                else if ((scope & ObjectScope.Protected) != ObjectScope.None)
                {
                    protectedList.Add(new ListNode(prop, obj));
                }
            }
            
            if (privateList.Count > 0) privateList.Sort((a, b) => { return String.Compare(a.FName, b.FName, true); });
            if (protectedList.Count > 0) protectedList.Sort((a, b) => { return String.Compare(a.FName, b.FName, true); });
            if (publicList.Count > 0) publicList.Sort((a, b) => { return String.Compare(a.FName, b.FName, true); });

            List<RICustomDataColumn> columns = new List<RICustomDataColumn>();
            columns.Add(new RICustomDataColumn("Property"));
            columns.Add(new RICustomDataColumn("Value"));

            RICustomData cData = new RICustomData(String.Format("Type: {0}", typ.FullName), columns, true, true);

            // Only do this if necessary.
            if (privateList.Count > 0) UpdateCustomData(cData, "Private", privateList);
            if (protectedList.Count > 0) UpdateCustomData(cData, "Protected", protectedList);
            if (publicList.Count > 0) UpdateCustomData(cData, "Public", publicList);

            return (cData);
        }        
        
        public static RICustomData BuildObjectPropertyMap(Object obj)
        {
            return BuildObjectPropertyMap(obj, ObjectScope.All);
        }
    }
}
