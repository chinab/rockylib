using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocky.Data;
using Newtonsoft.Json.Linq;

namespace NoSQL
{
    internal sealed class PrimaryKeyMapper : CacheKeyGenerator, ICacheKeyMapper
    {
        public static Array ResolveKey(string key, out MetaTable model)
        {
            var json = JObject.Parse(key);
            model = MetaTable.GetTable(Type.GetType(json["QualifiedName"].Value<string>().Replace("&nbsp;", " "), true));
            return json["ColumnValues"].Values<object>().ToArray();
        }

        private MetaTable _tableModel;
        private MetaColumn[] _columns;

        public MetaColumn[] PrimaryKeys
        {
            get { return _columns; }
        }

        internal PrimaryKeyMapper(IEnumerable<MetaColumn> primaryKeys)
        {
            _tableModel = primaryKeys.First().Table;
            _columns = primaryKeys.ToArray();
        }

        public override string GenerateKey(object entity)
        {
            if (_tableModel.EntityType != entity.GetType())
            {
                throw new ArgumentException("The entity's invalid.");
            }

            var json = new JObject();
            json["QualifiedName"] = _tableModel.EntityType.AssemblyQualifiedName.Replace(" ", "&nbsp;");
            var array = new JArray();
            foreach (var col in _columns)
            {
                array.Add(JValue.FromObject(PropertyAccess.WrapProperty(col.EntityProperty).GetValue(entity)));
            }
            json["ColumnValues"] = array;
            return json.ToString(Newtonsoft.Json.Formatting.None);
        }

        public string GenerateKey(Array value)
        {
            if (_columns.Length != value.Length)
            {
                throw new ArgumentException("value");
            }

            var json = new JObject();
            json["QualifiedName"] = _tableModel.EntityType.AssemblyQualifiedName.Replace(" ", "&nbsp;");
            var array = new JArray();
            for (int i = 0; i < _columns.Length; i++)
            {
                array.Add(JValue.FromObject(value.GetValue(i)));
            }
            json["ColumnValues"] = array;
            return json.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}