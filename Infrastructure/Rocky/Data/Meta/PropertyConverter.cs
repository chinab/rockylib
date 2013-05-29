using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rocky.Data
{
    public class PropertyConverter : IPropertyConvertible
    {
        public string MappedName { private set; get; }
        public bool IsNullable { private set; get; }
        public PropertyInfo EntityProperty { private set; get; }

        public PropertyConverter(PropertyInfo property)
            : this(property.Name, TypeHelper.IsStringOrNullableType(property.PropertyType), property)
        {

        }
        public PropertyConverter(string name, bool nullable, PropertyInfo property)
        {
            this.MappedName = name;
            this.IsNullable = nullable;
            this.EntityProperty = property;
        }
    }

    #region PropertyAccess
    /// <summary>
    /// 作   者：wangxiaoming
    /// 时   间：2010-6-29
    /// 公   司：上海盛大网络发展有限公司
    /// 说   明：本代码版权归上海盛大网络发展有限公司所有
    /// 标   识：3ab3a91b-9c38-4845-92e3-6344f872db42
    /// CLR版本：2.0.50727.3603
    /// </summary>
    public class PropertyAccess : PropertyConverter
    {
        public const BindingFlags PropertyBinding = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;
        private static readonly int _cacheCapacity;
        private static readonly Hashtable _cache;
        private static readonly Func<object, object> EmptyGetter;
        private static readonly Action<object, object> EmptySetter;

        static PropertyAccess()
        {
            _cacheCapacity = 2000;
            _cache = Hashtable.Synchronized(new Hashtable(_cacheCapacity));
            EmptyGetter = instance => null;
            EmptySetter = (instance, value) => { };
        }

        public static PropertyAccess WrapProperty(PropertyInfo property)
        {
            PropertyAccess access = (PropertyAccess)_cache[property];
            if (access == null)
            {
                if (_cache.Count > _cacheCapacity)
                {
                    _cache.Clear();
                }
                _cache[property] = access = new PropertyAccess(property);
            }
            return access;
        }

        private Delegate getDelegate, setDelegate;

        protected PropertyAccess(PropertyInfo property)
            : base(property)
        {
            Initialize(property);
        }
        protected PropertyAccess(string name, bool nullable, PropertyInfo property)
            : base(name, nullable, property)
        {
            Initialize(property);
        }
        private void Initialize(PropertyInfo property)
        {
            Contract.Requires(property != null);

            if (property.CanRead)
            {
                Type genericType = typeof(Func<,>);
                Type delegateType = genericType.MakeGenericType(new Type[] { property.ReflectedType, property.PropertyType });
                getDelegate = Delegate.CreateDelegate(delegateType, property.GetGetMethod() ?? property.GetGetMethod(true));
            }
            else
            {
                getDelegate = EmptyGetter;
            }
            if (property.CanWrite)
            {
                Type genericType = typeof(Action<,>);
                Type delegateType = genericType.MakeGenericType(new Type[] { property.ReflectedType, property.PropertyType });
                setDelegate = Delegate.CreateDelegate(delegateType, property.GetSetMethod() ?? property.GetSetMethod(true));
            }
            else
            {
                setDelegate = EmptySetter;
            }
        }

        public object GetValue(object instance)
        {
            return getDelegate.DynamicInvoke(instance);
        }
        public void SetValue(object instance, object value)
        {
            setDelegate.DynamicInvoke(instance, value);
        }

        public object ChangeType(object value)
        {
            Type propertyType = base.EntityProperty.PropertyType;
            if (propertyType.IsEnum)
            {
                return Enum.ToObject(propertyType, Convert.ToInt32(value));
            }
            else
            {
                return Convert.ChangeType(value, Nullable.GetUnderlyingType(propertyType) ?? propertyType);
            }
        }
    }
    #endregion
}