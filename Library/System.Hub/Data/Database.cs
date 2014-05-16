using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;

namespace System.Data
{
    /// <summary>
    /// MultipleActiveResultSets=True;
    /// </summary>
    public class Database : IRequiresFactory
    {
        #region Static
        internal const string ReturnParameterName = "@RETURN_VALUE";
        internal const string DataTableName = "T";
        #endregion

        #region Fields
        private DbFactory _factory;
        protected readonly ObjectCache Cache;
        #endregion

        #region Properties
        public virtual DbFactory Factory
        {
            get { return _factory; }
        }
        public bool SupportStoredProc
        {
            get { return Cache != null; }
        }
        #endregion

        #region Constructors
        public Database(DbFactory factory, int? spCacheMemoryLimitMegabytes = null)
        {
            _factory = factory;
            if (spCacheMemoryLimitMegabytes != null)
            {
                Cache = new MemoryCache(string.Format("Database[{0}]", factory.Name), new System.Collections.Specialized.NameValueCollection() { { "cacheMemoryLimitMegabytes", spCacheMemoryLimitMegabytes.Value.ToString() } });
            }
        }
        #endregion

        #region NativeMethods
        public DbCommand PrepareCommand(string text, CommandType type)
        {
            DbCommand cmd;
            var scope = DbScope.Current;
            if (scope != null)
            {
                cmd = scope.PrepareCommand(this);
                cmd.CommandText = text;
            }
            else
            {
                cmd = _factory.CreateCommand(text);
            }
            cmd.CommandType = type;
            return cmd;
        }

        protected int ExecuteNonQuery(DbCommand cmd)
        {
            if (cmd.Connection == null)
            {
                cmd.Connection = _factory.CreateConnection();
            }
            bool isClosed = cmd.Connection.State == ConnectionState.Closed;
            try
            {
                if (isClosed)
                {
                    cmd.Connection.Open();
                }
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                if (isClosed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        protected object ExecuteScalar(DbCommand cmd)
        {
            if (cmd.Connection == null)
            {
                cmd.Connection = _factory.CreateConnection();
            }
            bool isClosed = cmd.Connection.State == ConnectionState.Closed;
            try
            {
                if (isClosed)
                {
                    cmd.Connection.Open();
                }
                return cmd.ExecuteScalar();
            }
            finally
            {
                if (isClosed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        protected DbDataReader ExecuteReader(DbCommand cmd)
        {
            if (cmd.Connection == null)
            {
                cmd.Connection = _factory.CreateConnection();
            }
            bool isClosed = cmd.Connection.State == ConnectionState.Closed;
            if (isClosed)
            {
                cmd.Connection.Open();
            }
            return cmd.ExecuteReader(isClosed ? CommandBehavior.CloseConnection : CommandBehavior.Default);
        }

        protected DataTable ExecuteDataTable(DbCommand cmd, int startRecord = -1, int maxRecords = 0)
        {
            var dt = new DataTable(DataTableName);
            if (cmd.Connection == null)
            {
                cmd.Connection = _factory.CreateConnection();
            }
            using (DbDataAdapter da = _factory.CreateDataAdapter(cmd))
            {
                if (startRecord == -1)
                {
                    da.Fill(dt);
                }
                else
                {
                    da.Fill(startRecord, maxRecords, dt);
                }
            }
            return dt;
        }

        protected DataSet ExecuteDataSet(DbCommand cmd)
        {
            var ds = new DataSet();
            if (cmd.Connection == null)
            {
                cmd.Connection = _factory.CreateConnection();
            }
            using (DbDataAdapter da = _factory.CreateDataAdapter(cmd))
            {
                da.Fill(ds, DataTableName);
            }
            return ds;
        }
        #endregion

        #region Methods
        public int ExecuteNonQuery(string formatSql, params object[] paramValues)
        {
            string text = DbUtility.GetFormat(formatSql, paramValues);
            var cmd = this.PrepareCommand(text, CommandType.Text);
            return this.ExecuteNonQuery(cmd);
        }

        public T ExecuteScalar<T>(string formatSql, params object[] paramValues)
        {
            string text = DbUtility.GetFormat(formatSql, paramValues);
            var cmd = this.PrepareCommand(text, CommandType.Text);
            return (T)Convert.ChangeType(this.ExecuteScalar(cmd), typeof(T));
        }

        public DbDataReader ExecuteReader(string formatSql, params object[] paramValues)
        {
            string text = DbUtility.GetFormat(formatSql, paramValues);
            var cmd = this.PrepareCommand(text, CommandType.Text);
            return this.ExecuteReader(cmd);
        }

        public DataTable ExecuteDataTable(string formatSql, params object[] paramValues)
        {
            return this.ExecuteDataTable(-1, 0, formatSql, paramValues);
        }
        public DataTable ExecuteDataTable(int startRecord, int maxRecords, string formatSql, params object[] paramValues)
        {
            string text = DbUtility.GetFormat(formatSql, paramValues);
            var cmd = this.PrepareCommand(text, CommandType.Text);
            return this.ExecuteDataTable(cmd, startRecord, maxRecords);
        }

        public int UpdateDataTable(DataTable dt, params string[] joinSelectSql)
        {
            int affected = 0;
            var cmd = this.PrepareCommand(string.Empty, CommandType.Text);
            using (var da = this.Factory.CreateDataAdapter(cmd))
            using (var cb = this.Factory.CreateCommandBuilder(da))
            {
                da.AcceptChangesDuringUpdate = false;
                affected = da.Update(dt);
                if (!joinSelectSql.IsNullOrEmpty())
                {
                    for (int i = 0; i < joinSelectSql.Length; i++)
                    {
                        cb.RefreshSchema();
                        da.SelectCommand.CommandText = joinSelectSql[i];
                        affected += da.Update(dt);
                    }
                }
                dt.AcceptChanges();
            }
            return affected;
        }
        #endregion

        #region StoredProc
        #region Command
        /// <summary>
        /// cmd.CommandType = CommandType.StoredProcedure;
        /// Always discoveredParameters[0].ParameterName == Database.ReturnParameterName
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected DbParameter[] GetDeriveParameters(DbCommand cmd)
        {
            string spName = cmd.CommandText;
            DbParameter[] discoveredParameters = (DbParameter[])Cache[spName];
            if (discoveredParameters == null)
            {
                string qualifiedName = cmd.GetType().AssemblyQualifiedName;
                Type builderType = Type.GetType(qualifiedName.Insert(qualifiedName.IndexOf(','), "Builder"));
                MethodInfo method = builderType.GetMethod("DeriveParameters", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
                if (method == null)
                {
                    throw new ArgumentException("The specified provider factory doesn't support stored procedures.");
                }
                if (cmd.Connection == null)
                {
                    cmd.Connection = _factory.CreateConnection();
                }
                bool isClosed = cmd.Connection.State == ConnectionState.Closed;
                try
                {
                    if (isClosed)
                    {
                        cmd.Connection.Open();
                    }
                    method.Invoke(null, new object[] { cmd });
                }
                finally
                {
                    if (isClosed)
                    {
                        cmd.Connection.Close();
                    }
                }
                Cache[spName] = discoveredParameters = new DbParameter[cmd.Parameters.Count];
                cmd.Parameters.CopyTo(discoveredParameters, 0);
                cmd.Parameters.Clear();
            }
            return discoveredParameters;
        }

        public void DeriveParameters(DbCommand cmd)
        {
            DbParameter[] originalParameters = GetDeriveParameters(cmd);
            for (int i = 0; i < originalParameters.Length; i++)
            {
                cmd.Parameters.Add(((ICloneable)originalParameters[i]).Clone());
            }
        }

        public void DeriveAssignParameters(DbCommand cmd, object[] values)
        {
            DbParameter[] discoveredParameters = GetDeriveParameters(cmd);
            if (cmd.Parameters.Count > 0 || discoveredParameters.Length - 1 != values.Length)
            {
                throw new ArgumentException("The number of parameters doesn't match number of values for stored procedures.");
            }
            cmd.Parameters.Add(((ICloneable)discoveredParameters[0]).Clone());
            for (int i = 0; i < values.Length; )
            {
                object value = values[i] ?? DBNull.Value;
                DbParameter discoveredParameter = discoveredParameters[++i];
                object cloned = ((ICloneable)discoveredParameter).Clone();
                ((DbParameter)cloned).Value = value;
                cmd.Parameters.Add(cloned);
            }
        }

        public void SetParameterValue(DbCommand cmd, int index, object value)
        {
            int startIndex = cmd.Parameters.Count > 0 && cmd.Parameters[0].ParameterName == ReturnParameterName ? 1 : 0;
            cmd.Parameters[startIndex + index].Value = value;
        }
        public void SetParameterValue(DbCommand cmd, string name, object value)
        {
            cmd.Parameters[_factory.ParameterNamePrefix + name].Value = value;
        }

        public object GetParameterValue(DbCommand cmd, int index)
        {
            int startIndex = cmd.Parameters.Count > 0 && cmd.Parameters[0].ParameterName == ReturnParameterName ? 1 : 0;
            return cmd.Parameters[startIndex + index].Value;
        }
        public object GetParameterValue(DbCommand cmd, string name)
        {
            return cmd.Parameters[_factory.ParameterNamePrefix + name].Value;
        }

        public object GetParameterReturnValue(DbCommand cmd)
        {
            if (cmd.Parameters.Count > 0 && cmd.Parameters[0].ParameterName == ReturnParameterName)
            {
                return cmd.Parameters[0].Value;
            }
            return null;
        }
        #endregion

        #region Execute
        protected virtual void FillOutputValue(DbCommand cmd, object[] values)
        {
            for (int i = 1; i < cmd.Parameters.Count; i++)
            {
                var param = cmd.Parameters[i];
                if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                {
                    values[i - 1] = param.Value;
                }
            }
        }

        public int ExecuteStoredProcNonQuery(string spName, params object[] values)
        {
            var cmd = this.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            FillOutputValue(cmd, values);
            return ExecuteNonQuery(cmd);
        }

        public object ExecuteStoredProcScalar(string spName, params object[] values)
        {
            var cmd = this.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            FillOutputValue(cmd, values);
            return ExecuteScalar(cmd);
        }

        public DbDataReader ExecuteStoredProcReader(string spName, params object[] values)
        {
            var cmd = this.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            FillOutputValue(cmd, values);
            return ExecuteReader(cmd);
        }

        public DataTable ExecuteStoredProcDataTable(string spName, params object[] values)
        {
            var cmd = this.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            FillOutputValue(cmd, values);
            return ExecuteDataTable(cmd);
        }

        public DataSet ExecuteStoredProcDataSet(string spName, params object[] values)
        {
            var cmd = this.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            FillOutputValue(cmd, values);
            return ExecuteDataSet(cmd);
        }
        #endregion
        #endregion
    }
}