using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Data
{
    #region EntityConverter
    public static class EntityConverter<T>
    {
        public static Converter<DbDataReader, T> CreateReadSingle(IPropertyConvertible[] propertys)
        {
            DynamicMethod method = new DynamicMethod(string.Empty, typeof(T), new Type[] { typeof(DbDataReader) }, typeof(EntityConverter<T>));
            ILGenerator il = method.GetILGenerator();
            LocalBuilder item = il.DeclareLocal(typeof(T));
            BuildItem(il, item, propertys);
            il.Emit(OpCodes.Ldloc_S, item);
            il.Emit(OpCodes.Ret);
            return (Converter<DbDataReader, T>)method.CreateDelegate(typeof(Converter<DbDataReader, T>));
        }

        public static Converter<DbDataReader, T> CreateTranslateSingle(IPropertyConvertible[] propertys)
        {
            DynamicMethod method = new DynamicMethod(string.Empty, typeof(T), new Type[] { typeof(DbDataReader) }, typeof(EntityConverter<T>));
            ILGenerator il = method.GetILGenerator();
            LocalBuilder item = il.DeclareLocal(typeof(T));
            Label labNotRead = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, DataReaderMethods.Read);
            il.Emit(OpCodes.Brfalse, labNotRead);
            BuildItem(il, item, propertys);
            il.MarkLabel(labNotRead);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, DataReaderMethods.Close);
            il.Emit(OpCodes.Ldloc_S, item);
            il.Emit(OpCodes.Ret);
            return (Converter<DbDataReader, T>)method.CreateDelegate(typeof(Converter<DbDataReader, T>));
        }

        public static Converter<DbDataReader, List<T>> CreateTranslateList(IPropertyConvertible[] propertys)
        {
            DynamicMethod method = new DynamicMethod(string.Empty, typeof(List<T>), new Type[] { typeof(DbDataReader) }, typeof(EntityConverter<T>));
            ILGenerator il = method.GetILGenerator();
            LocalBuilder list = il.DeclareLocal(typeof(List<T>));
            LocalBuilder item = il.DeclareLocal(typeof(T));
            Label labLoop = il.DefineLabel();
            Label labNotRead = il.DefineLabel();
            il.Emit(OpCodes.Newobj, typeof(List<T>).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_S, list);
            il.MarkLabel(labLoop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, DataReaderMethods.Read);
            il.Emit(OpCodes.Brfalse, labNotRead);
            BuildItem(il, item, propertys);
            il.Emit(OpCodes.Ldloc_S, list);
            il.Emit(OpCodes.Ldloc_S, item);
            il.Emit(OpCodes.Callvirt, typeof(List<T>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod));
            il.Emit(OpCodes.Br, labLoop);
            il.MarkLabel(labNotRead);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, DataReaderMethods.Close);
            il.Emit(OpCodes.Ldloc_S, list);
            il.Emit(OpCodes.Ret);
            return (Converter<DbDataReader, List<T>>)method.CreateDelegate(typeof(Converter<DbDataReader, List<T>>));
        }

        private static void BuildItem(ILGenerator il, LocalBuilder item, IPropertyConvertible[] propertys)
        {
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_S, item);
            for (int i = 0; i < propertys.Length; i++)
            {
                IPropertyConvertible property = propertys[i];
                LocalBuilder ordinal = il.DeclareLocal(typeof(int));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, property.MappedName);
                il.Emit(OpCodes.Callvirt, DataReaderMethods.GetOrdinal);
                il.Emit(OpCodes.Stloc_S, ordinal);
                MethodInfo method;
                if (DataReaderMethods.TryGetValueTypeMethod(property.EntityProperty.PropertyType, out method))
                {
                    Type underlyingType = Nullable.GetUnderlyingType(property.EntityProperty.PropertyType);
                    if (underlyingType != null)
                    {
                        LocalBuilder local = il.DeclareLocal(property.EntityProperty.PropertyType);
                        Label isDBNulllabel = il.DefineLabel();
                        Label hasValuelabel = il.DefineLabel();
                        il.Emit(OpCodes.Ldloca, local);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldloc_S, ordinal);
                        il.Emit(OpCodes.Callvirt, DataReaderMethods.IsDBNull);
                        il.Emit(OpCodes.Brtrue_S, isDBNulllabel);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldloc_S, ordinal);
                        il.Emit(OpCodes.Callvirt, method);
                        il.Emit(OpCodes.Call, property.EntityProperty.PropertyType.GetConstructor(new Type[] { underlyingType }));
                        il.Emit(OpCodes.Br_S, hasValuelabel);
                        il.MarkLabel(isDBNulllabel);
                        il.Emit(OpCodes.Initobj, property.EntityProperty.PropertyType);
                        il.MarkLabel(hasValuelabel);
                        il.Emit(OpCodes.Ldloc_S, item);
                        il.Emit(OpCodes.Ldloc, local);
                        il.Emit(OpCodes.Callvirt, property.EntityProperty.GetSetMethod(false));
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc_S, item);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldloc_S, ordinal);
                        il.Emit(OpCodes.Callvirt, method);
                        il.Emit(OpCodes.Callvirt, property.EntityProperty.GetSetMethod(false));
                    }
                }
                else
                {
                    Label isNotDBNullLabel = il.DefineLabel();
                    il.Emit(OpCodes.Ldloc_S, item);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_S, ordinal);
                    il.Emit(OpCodes.Callvirt, DataReaderMethods.GetValue);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Call, typeof(Convert).GetMethod("IsDBNull", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod));
                    il.Emit(OpCodes.Brfalse_S, isNotDBNullLabel);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldnull);
                    il.MarkLabel(isNotDBNullLabel);
                    il.Emit(OpCodes.Unbox_Any, property.EntityProperty.PropertyType);
                    il.Emit(OpCodes.Callvirt, property.EntityProperty.GetSetMethod(false));
                }
            }
        }
    }
    #endregion

    #region DataReaderMethods
    internal static class DataReaderMethods
    {
        public static readonly MethodInfo Read;
        public static readonly MethodInfo GetOrdinal;
        public static readonly MethodInfo GetValue;
        public static readonly MethodInfo IsDBNull;
        public static readonly MethodInfo Close;
        private static readonly Hashtable GetMethods;

        static DataReaderMethods()
        {
            Type dataReaderType = typeof(DbDataReader);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
            Read = dataReaderType.GetMethod("Read", flags);
            GetOrdinal = dataReaderType.GetMethod("GetOrdinal", flags);
            GetValue = dataReaderType.GetMethod("GetValue", flags);
            IsDBNull = dataReaderType.GetMethod("IsDBNull", flags);
            Close = dataReaderType.GetMethod("Close", flags);
            GetMethods = new Hashtable(11);
            GetMethods.Add(typeof(Boolean), dataReaderType.GetMethod("GetBoolean", flags));
            GetMethods.Add(typeof(Byte), dataReaderType.GetMethod("GetByte", flags));
            GetMethods.Add(typeof(Char), dataReaderType.GetMethod("GetChar", flags));
            GetMethods.Add(typeof(DateTime), dataReaderType.GetMethod("GetDateTime", flags));
            GetMethods.Add(typeof(Decimal), dataReaderType.GetMethod("GetDecimal", flags));
            GetMethods.Add(typeof(Double), dataReaderType.GetMethod("GetDouble", flags));
            GetMethods.Add(typeof(float), dataReaderType.GetMethod("GetFloat", flags));
            GetMethods.Add(typeof(Guid), dataReaderType.GetMethod("GetGuid", flags));
            GetMethods.Add(typeof(Int16), dataReaderType.GetMethod("GetInt16", flags));
            GetMethods.Add(typeof(Int32), dataReaderType.GetMethod("GetInt32", flags));
            GetMethods.Add(typeof(Int64), dataReaderType.GetMethod("GetInt64", flags));
        }

        public static bool TryGetValueTypeMethod(Type type, out MethodInfo method)
        {
            method = (MethodInfo)GetMethods[type];
            return method != null;
        }
    }
    #endregion
}