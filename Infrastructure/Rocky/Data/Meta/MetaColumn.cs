using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Reflection;

namespace Rocky.Data
{
    public sealed class MetaColumn : PropertyAccess
    {
        #region Properties
        public MetaTable Table
        {
            get { return MetaTable.GetTable(base.EntityProperty.ReflectedType); }
        }
        public ColumnAttribute _Attribute { get; private set; }
        public int Ordinal { get; private set; }
        public string FullName { get; private set; }
        #endregion

        #region Methods
        public MetaColumn(string tableName, ColumnAttribute attr, int ordinal, PropertyInfo property)
            : base(attr.Name ?? property.Name, attr.CanBeNull, property)
        {
            if (!property.CanWrite || !property.CanRead)
            {
                throw new MappingException("This property isn't supported.");
            }
            this._Attribute = attr;
            this.Ordinal = ordinal;
            this.FullName = string.Format("{0}.{1}", tableName, base.MappedName);
        }
        #endregion
    }
}