using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data.Linq.Mapping;
using System.Reflection;

namespace System.Data
{
    public sealed class MetaTable : IMapping
    {
        #region StaticMembers
        private static readonly int _cacheCapacity;
        private static readonly Hashtable _cache;

        static MetaTable()
        {
            _cacheCapacity = 100;
            _cache = Hashtable.Synchronized(new Hashtable(_cacheCapacity));
        }

        public static MetaTable GetTable(Type entityType)
        {
            MetaTable metaTable = (MetaTable)_cache[entityType];
            if (metaTable == null)
            {
                if (_cache.Count > _cacheCapacity)
                {
                    _cache.Clear();
                }
                _cache[entityType] = metaTable = new MetaTable(entityType);
            }
            return metaTable;
        }

        public static bool TryGetTable(Type entityType, out MetaTable metaTable)
        {
            try
            {
                metaTable = GetTable(entityType);
                return true;
            }
            catch (MappingException)
            {
                metaTable = null;
                return false;
            }
        }
        #endregion

        #region Properties
        public TableAttribute _Attribute { get; private set; }
        public Type EntityType { get; private set; }
        public string MappedName { get; private set; }
        public ReadOnlyCollection<MetaColumn> PrimaryKey { get; private set; }
        public ReadOnlyCollection<MetaColumn> Columns { get; private set; }
        #endregion

        #region Methods
        public MetaTable(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException("entityType");
            }
            var attr = (TableAttribute)Attribute.GetCustomAttribute(entityType, typeof(TableAttribute));
            if (attr == null)
            {
                throw new MappingException(string.Format("{0} isn't decorated with the TableAttribute.", entityType.FullName));
            }
            this._Attribute = attr;
            this.EntityType = entityType;
            if (!string.IsNullOrEmpty(attr.Name))
            {
                this.MappedName = attr.Name.Replace("dbo.", string.Empty);
            }
            else
            {
                this.MappedName = this.EntityType.Name;
            }
            Type colAttrType = typeof(ColumnAttribute);
            var list = new List<MetaColumn>();
            var arr = this.EntityType.GetProperties(PropertyAccess.PropertyBinding);
            foreach (var prop in arr)
            {
                var colAttr = (ColumnAttribute)Attribute.GetCustomAttribute(prop, colAttrType);
                if (colAttr != null)
                {
                    list.Add(new MetaColumn(this.MappedName, colAttr, list.Count, prop));
                }
            }
            this.PrimaryKey = list.Where(t => t._Attribute.IsPrimaryKey).ToList().AsReadOnly();
            if (this.PrimaryKey.Count == 0)
            {
                throw new MappingException("The metaTable has no identity members, need to mark a PrimaryKey Attribute first.");
            }
            this.Columns = list.AsReadOnly();
        }
        #endregion
    }
}