using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace Rocky.Data
{
    public sealed class SqlRowChangedEventArgs : EventArgs
    {
        #region Fields
        internal readonly DbDataReader DataReader;
        private const string Sql_Func = @"IS_CHANGE_{0}";
        private MetaTable _tableModel;
        private SqlRowChangedTypes _changeType;
        #endregion

        #region Properties
        public MetaTable Table
        {
            get { return _tableModel; }
        }
        public SqlRowChangedTypes ChangeType
        {
            get { return _changeType; }
        }
        #endregion

        #region Constructors
        internal SqlRowChangedEventArgs(MetaTable tableModel, SqlRowChangedTypes changeType, DbCommand cmd)
        {
            _tableModel = tableModel;
            _changeType = changeType;
            DataReader = cmd.ExecuteReader();
        }
        #endregion

        #region Methods
        private void CheckDataReader()
        {
            if (DataReader.IsClosed)
            {
                throw new InvalidOperationException("DataReader's closed.");
            }
        }

        public IDictionary<MetaColumn, object> GetPrimaryKey()
        {
            this.CheckDataReader();

            var result = new Dictionary<MetaColumn, object>(_tableModel.PrimaryKey.Count);
            foreach (var col in _tableModel.PrimaryKey)
            {
                result.Add(col, DataReader[col.MappedName]);
            }
            return result;
        }

        public IDictionary<MetaColumn, object> GetChangeColumns()
        {
            this.CheckDataReader();
            if (_changeType != SqlRowChangedTypes.Updated)
            {
                return new Dictionary<MetaColumn, object>(0);
            }

            var result = new Dictionary<MetaColumn, object>();
            foreach (var col in _tableModel.Columns)
            {
                if (col._Attribute.IsDbGenerated)
                {
                    continue;
                }
                if (Convert.ToBoolean(DataReader[string.Format(Sql_Func, col.MappedName)]))
                {
                    result.Add(col, DataReader[col.MappedName]);
                }
            }
            return result;
        }

        public TEntity GetRow<TEntity>()
        {
            this.CheckDataReader();
            if (_changeType == SqlRowChangedTypes.Deleted)
            {
                return default(TEntity);
            }

            var converter = EntityUtility.CreateConverter<TEntity>();
            return converter(DataReader);
        }
        #endregion
    }
}