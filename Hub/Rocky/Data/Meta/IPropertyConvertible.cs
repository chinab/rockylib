using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace System.Data
{
    public interface IPropertyConvertible : IMapping
    {
        bool IsNullable { get; }
        PropertyInfo EntityProperty { get; }
    }
}