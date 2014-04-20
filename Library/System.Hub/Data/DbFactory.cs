using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.Common;

namespace System.Data
{
    public sealed class DbFactory : IRequiresFactory
    {
        #region StaticMembers
        internal const string OleDbParameterNamePrefix = "@";  //SQLParameterNamePrefix = "?";
        internal const string SqlClientParameterNamePrefix = "@";  //SQLParameterNamePrefix = "@";
        internal const string OracleClientParameterNamePrefix = "";  //SQLParameterNamePrefix = ":";
        private static DbFactory[] stored;

        static DbFactory()
        {
            ConnectionStringSettingsCollection connectionStrings = ConfigurationManager.ConnectionStrings;
            List<DbFactory> factories = new List<DbFactory>(connectionStrings.Count - 1);
            for (int i = 1; i < connectionStrings.Count; i++)
            {
                DbProviderName providerName;
                if (TryConvertTo(connectionStrings[i].ProviderName, out providerName))
                {
                    string connectionString = connectionStrings[i].ConnectionString;
                    DbFactory factory = factories.Find(item => item.Name == connectionStrings[i].Name);
                    if (factory == null)
                    {
                        factory = new DbFactory(connectionStrings[i].Name, connectionString, providerName);
                        factories.Add(factory);
                    }
                    else
                    {
                        factory._connectionString = connectionString;
                        factory._providerName = providerName;
                    }
                }
            }
            stored = factories.ToArray();
        }
        private static bool TryConvertTo(string name, out DbProviderName providerName)
        {
            providerName = DbProviderName.OleDb;
            bool succeed = false;
            switch (name)
            {
                case "System.Data.OleDb":
                    succeed = true;
                    break;
                case "System.Data.SqlClient":
                    providerName = DbProviderName.SQLServer;
                    succeed = true;
                    break;
            }
            return succeed;
        }

        public static DbFactory GetFactory(int index)
        {
            return stored[index];
        }
        public static DbFactory GetFactory(string name)
        {
            return stored.Where(t => t.Name == name).Single();
        }
        public static DbFactory GetFactory(string connectionString, DbProviderName providerName)
        {
            DbFactory factory = null;
            for (int i = 0; i < stored.Length; i++)
            {
                if (stored[i].ConnectionString == connectionString && stored[i].ProviderName == providerName)
                {
                    factory = stored[i];
                    break;
                }
            }
            if (factory == null)
            {
                lock (stored.SyncRoot)
                {
                    int index = stored.Length;
                    Array.Resize<DbFactory>(ref stored, index + 1);
                    stored[index] = factory = new DbFactory(StringHelper.NowDateString, connectionString, providerName);
                }
            }
            return factory;
        }
        #endregion

        #region Fields
        private string _name, _connectionString, _parameterNamePrefix;
        private DbProviderName _providerName;
        private DbProviderFactory _providerFactory;
        #endregion

        #region Properties
        public string Name
        {
            get { return _name; }
        }
        public string ConnectionString
        {
            get { return _connectionString; }
        }
        public string ParameterNamePrefix
        {
            get { return _parameterNamePrefix; }
        }
        public DbProviderName ProviderName
        {
            get { return _providerName; }
        }
        public DbProviderFactory ProviderFactory
        {
            get { return _providerFactory; }
        }
        public DbFactory Factory
        {
            get { return this; }
        }
        #endregion

        #region Methods
        private DbFactory(string name, string connectionString, DbProviderName providerName)
        {
            _name = name;
            _connectionString = connectionString;
            _providerName = providerName;
            string providerInvariantName = null;
            switch (_providerName)
            {
                case DbProviderName.SQLServer:
                    _parameterNamePrefix = SqlClientParameterNamePrefix;
                    providerInvariantName = "System.Data.SqlClient";
                    break;
                case DbProviderName.OleDb:
                    _parameterNamePrefix = OleDbParameterNamePrefix;
                    providerInvariantName = "System.Data.OleDb";
                    break;
                default:
                    throw new NotSupportedException(_providerName.ToString());
            }
            if (providerInvariantName == null)
            {
                throw new NotSupportedException(providerName.ToString());
            }
            _providerFactory = DbProviderFactories.GetFactory(providerInvariantName);
        }

        public DbConnection CreateConnection()
        {
            DbConnection conn = _providerFactory.CreateConnection();
            conn.ConnectionString = _connectionString;
            return conn;
        }

        public DbCommand CreateCommand(string cmdText, DbConnection conn = null)
        {
            if (conn == null)
            {
                conn = CreateConnection();
            }
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = cmdText;
            return cmd;
        }

        public DbDataAdapter CreateDataAdapter(string selectCommandText)
        {
            return CreateDataAdapter(CreateCommand(selectCommandText));
        }
        public DbDataAdapter CreateDataAdapter(DbCommand selectCommand)
        {
            if (selectCommand == null)
            {
                throw new ArgumentNullException("selectCommand");
            }
            DbDataAdapter da = _providerFactory.CreateDataAdapter();
            da.SelectCommand = selectCommand;
            return da;
        }

        public DbCommandBuilder CreateCommandBuilder(DbDataAdapter da)
        {
            if (da == null)
            {
                throw new ArgumentNullException("da");
            }
            DbCommandBuilder cb = _providerFactory.CreateCommandBuilder();
            cb.DataAdapter = da;
            return cb;
        }
        #endregion
    }
}