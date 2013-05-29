using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Rocky
{
    public static partial class Extensions
    {
        #region ValueType
        public static bool HasFlag<T>(this T instance, T flags) where T : struct
        {
            var eType = instance.GetType();
            if (!eType.IsPrimitive)
            {
                throw new InvalidOperationException("Instance's invalid.");
            }
            long iVal = Convert.ToInt64(instance), fVal = Convert.ToInt64(flags);
            return (iVal & fVal) == fVal;
        }

        public static string ToDescription(this Enum instance)
        {
            Contract.Requires(instance != null);

            Type type = instance.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField;
            if (Attribute.IsDefined(type, typeof(FlagsAttribute)))
            {
                var desc = new StringBuilder();
                foreach (string perMemberName in System.Text.RegularExpressions.Regex.Split(instance.ToString(), ", "))
                {
                    desc.Append(GetEnumDescription(type.GetField(perMemberName, flags)) ?? perMemberName).Append(", ");
                }
                desc.Length -= 2;
                return desc.ToString();
            }
            string memberName = instance.ToString();
            return GetEnumDescription(type.GetField(memberName, flags)) ?? memberName;
        }
        private static string GetEnumDescription(FieldInfo field)
        {
            var attr = (EnumMemberAttribute)Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute));
            if (attr != null)
            {
                return attr.Value;
            }
            var attr2 = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            if (attr2 != null)
            {
                return attr2.Description;
            }
            return null;
        }
        #endregion

        #region String
        public static void AppendJoin(this StringBuilder instance, string separator, System.Collections.IEnumerable values, int startIndex = 0, int count = -1)
        {
            Contract.Requires(separator != null);
            Contract.Requires(values != null);

            int skip = startIndex;
            var tor = values.GetEnumerator();
            while (skip < startIndex)
            {
                if (tor.MoveNext())
                {
                    skip++;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("startIndex");
                }
            }

            bool first = true;
            while ((count == -1 || count-- > 0) && tor.MoveNext())
            {
                if (first)
                {
                    instance.Append(tor.Current);
                    first = false;
                }
                else
                {
                    instance.Append(separator).Append(tor.Current);
                }
            }
        }
        #endregion

        #region Methods
        public static string GetEndPoint(this Uri instance)
        {
            Contract.Requires(instance != null);

            string endPoint = instance.Authority;
            if (endPoint.LastIndexOf(":") == -1)
            {
                endPoint += ":80";
            }
            return endPoint;
        }

        [Pure]
        public static bool IsNullOrEmpty(this System.Collections.ICollection instance)
        {
            return instance == null || instance.Count == 0;
        }
        #endregion
    }
}