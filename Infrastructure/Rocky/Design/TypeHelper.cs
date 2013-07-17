using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace System
{
    public static class TypeHelper
    {
        public static bool IsStringOrValueType(Type type)
        {
            Contract.Requires(type != null);

            return type == typeof(string) || type.IsValueType;
        }

        public static bool IsStringOrNullableType(Type type)
        {
            Contract.Requires(type != null);

            return type == typeof(string) || IsNullableType(type);
        }

        public static bool IsNullableType(Type type)
        {
            Contract.Requires(type != null);

            Type underlyingType;
            return IsNullableType(type, out underlyingType);
        }
        public static bool IsNullableType(Type type, out Type underlyingType)
        {
            Contract.Requires(type != null);

            return (underlyingType = Nullable.GetUnderlyingType(type)) != null;
        }
    }
}