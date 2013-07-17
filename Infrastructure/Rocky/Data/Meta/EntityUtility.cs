using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Reflection;

namespace System.Data
{
    public static class EntityUtility
    {
        public static IEnumerable<PropertyAccess> GetEnumerable(Type entityType)
        {
            do
            {
                MetaTable table;
                if (MetaTable.TryGetTable(entityType, out table))
                {
                    foreach (var item in table.Columns)
                    {
                        yield return item;
                    }
                }
                else
                {
                    PropertyInfo[] array = entityType.GetProperties(PropertyAccess.PropertyBinding);
                    for (int i = 0; i < array.Length; i++)
                    {
                        yield return PropertyAccess.WrapProperty(array[i]);
                    }
                }
            }
            while ((entityType = entityType.BaseType) != typeof(object));
        }

        public static Converter<DbDataReader, T> CreateConverter<T>()
        {
            Type entityType = typeof(T);
            TypeCode code = Type.GetTypeCode(entityType);
            if (code == TypeCode.Object && entityType != typeof(Guid))
            {
                return dr =>
                {
                    T entity = Activator.CreateInstance<T>();
                    IEnumerable<PropertyAccess> enumerable = GetEnumerable(entity.GetType());
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        foreach (PropertyAccess property in enumerable)
                        {
                            if (String.Compare(dr.GetName(i), property.MappedName, true) == 0)
                            {
                                if (dr.IsDBNull(i))
                                {
                                    if (!property.IsNullable)
                                    {
                                        throw new InvalidOperationException(string.Format("{0} can't be null.", property.MappedName));
                                    }
                                    property.SetValue(entity, null);
                                }
                                else
                                {
                                    if (property.EntityProperty.PropertyType.IsEnum)
                                    {
                                        property.SetValue(entity, property.ChangeType(dr.GetValue(i)));
                                    }
                                    else
                                    {
                                        property.SetValue(entity, dr.GetValue(i));
                                    }
                                }
                                break;
                            }
                        }
                    }
                    return entity;
                };
            }
            else
            {
                return dr => (T)dr.GetValue(0);
            }
        }
    }
}