using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.ComponentModel;

namespace System.Data
{
    public sealed class SqlRowChangeMonitor : Component, INotifyPropertyChanged
    {
        #region Event
        private readonly object Event_Error = new object(),
            Event_Inserted = new object(),
            Event_Updated = new object(),
            Event_Deleted = new object(),
            Event_PropertyChanged = new object();

        public event EventHandler<SqlRowChangedEventArgs> Inserted
        {
            add { base.Events.AddHandler(Event_Inserted, value); }
            remove { base.Events.RemoveHandler(Event_Inserted, value); }
        }
        public event EventHandler<SqlRowChangedEventArgs> Updated
        {
            add { base.Events.AddHandler(Event_Updated, value); }
            remove { base.Events.RemoveHandler(Event_Updated, value); }
        }
        public event EventHandler<SqlRowChangedEventArgs> Deleted
        {
            add { base.Events.AddHandler(Event_Deleted, value); }
            remove { base.Events.RemoveHandler(Event_Deleted, value); }
        }
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { base.Events.AddHandler(Event_PropertyChanged, value); }
            remove { base.Events.RemoveHandler(Event_PropertyChanged, value); }
        }
        public event ErrorEventHandler Error
        {
            add { base.Events.AddHandler(Event_Error, value); }
            remove { base.Events.RemoveHandler(Event_Error, value); }
        }

        private void OnInserted(SqlRowChangedEventArgs e)
        {
            var method = (EventHandler<SqlRowChangedEventArgs>)base.Events[Event_Inserted];
            if (method != null)
            {
                method(this, e);
            }
        }
        private void OnUpdated(SqlRowChangedEventArgs e)
        {
            var method = (EventHandler<SqlRowChangedEventArgs>)base.Events[Event_Updated];
            if (method != null)
            {
                method(this, e);
            }
        }
        private void OnDeleted(SqlRowChangedEventArgs e)
        {
            var method = (EventHandler<SqlRowChangedEventArgs>)base.Events[Event_Deleted];
            if (method != null)
            {
                method(this, e);
            }
        }
        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var method = (PropertyChangedEventHandler)base.Events[Event_PropertyChanged];
            if (method != null)
            {
                method(this, e);
            }
        }
        private void OnError(ErrorEventArgs e)
        {
            var method = (ErrorEventHandler)base.Events[Event_Error];
            if (method != null)
            {
                method(this, e);
            }
        }
        #endregion

        #region Fields
        private const string Sql_CurrentVersion = @"SELECT CHANGE_TRACKING_CURRENT_VERSION()";
        private const string Sql_InsertTracking = @"SELECT t.* FROM {0} t
INNER JOIN CHANGETABLE(CHANGES {0},{2}) ct ON {1}
WHERE ct.SYS_CHANGE_OPERATION = 'I'
AND ct.SYS_CHANGE_VERSION <= {3}";
        private const string Sql_UpdateTracking = @"SELECT t.*{4} FROM {0} t
INNER JOIN CHANGETABLE(CHANGES {0},{2}) ct ON {1}
WHERE ct.SYS_CHANGE_OPERATION = 'U'
AND ct.SYS_CHANGE_VERSION <= {3}";
        private const string Sql_UpdateTracking_Func = @",CHANGE_TRACKING_IS_COLUMN_IN_MASK(COLUMNPROPERTY(OBJECT_ID('{0}'),'{1}','ColumnId'),ct.SYS_CHANGE_COLUMNS) IS_CHANGE_{1}";
        private const string Sql_DeleteTracking = @"SELECT {3} FROM CHANGETABLE(CHANGES {0},{1}) ct
WHERE ct.SYS_CHANGE_OPERATION='D'
AND ct.SYS_CHANGE_VERSION <= {2}";

        private DbFactory _factory;
        private string _databaseName;
        private List<MetaTable> _model;
        private long _currentVersion;
        private JobTimer _timer;
        #endregion

        #region Properties
        public string DatabaseName
        {
            get { return _databaseName; }
        }
        public ReadOnlyCollection<MetaTable> Model
        {
            get { return _model.AsReadOnly(); }
        }
        public long CurrentVersion
        {
            get { return _currentVersion; }
        }
        #endregion

        #region Constructors
        public SqlRowChangeMonitor(string connectionString, IEnumerable<Type> modelTypes, long version = -1L, TimeSpan? period = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(connectionString));
            Contract.Requires(modelTypes != null && modelTypes.Any());
            Contract.Requires(version >= -1L);

            _factory = DbFactory.GetFactory(connectionString, DbProviderName.SqlClient);
            var resolver = new SqlConnectionStringBuilder(connectionString);
            _databaseName = resolver.InitialCatalog;
            _model = modelTypes.Select(t => MetaTable.GetTable(t)).ToList();

            _currentVersion = version == -1L ? this.QueryCurrentVersion() : version;
            _timer = new JobTimer(JobContent, period.GetValueOrDefault(TimeSpan.FromSeconds(60D)));
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
            _timer = null;
            base.Dispose(disposing);
        }
        #endregion

        #region Methods
        private long QueryCurrentVersion()
        {
            var cmd = _factory.CreateCommand(Sql_CurrentVersion);
            try
            {
                cmd.Connection.Open();
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            finally
            {
                cmd.Connection.Close();
            }
        }

        private void JobContent(object state)
        {
            DbCommand cmd = null;
            try
            {
                // 因为没有锁表，所以在查出CurrentVersion后再继续查询对应的增删改可能会出现CurrentVersion不同步的情况
                // 所以在查询条件中加了范围限制
                long lastVersion = this.QueryCurrentVersion();
                if (_currentVersion == lastVersion)
                {
                    return;
                }

                cmd = _factory.CreateCommand(string.Empty);
                cmd.Connection.Open();
                var buffer = new StringBuilder();
                #region Insert
                foreach (var tableModel in _model)
                {
                    string pkWhere = CreateSqlWhere(buffer, tableModel);
                    cmd.CommandText = string.Format(Sql_InsertTracking, tableModel.MappedName, pkWhere, _currentVersion, lastVersion);
                    App.LogDebug("Sql_InsertTracking:{0}.", cmd.CommandText);
                    var e = new SqlRowChangedEventArgs(tableModel, SqlRowChangedTypes.Inserted, cmd);
                    while (e.DataReader.Read())
                    {
                        OnInserted(e);
                    }
                    e.DataReader.Close();
                }
                #endregion

                #region Update
                foreach (var tableModel in _model)
                {
                    string pkWhere = CreateSqlWhere(buffer, tableModel);
                    buffer.Length = 0;
                    foreach (var col in tableModel.Columns)
                    {
                        if (!col._Attribute.IsDbGenerated)
                        {
                            buffer.AppendFormat(Sql_UpdateTracking_Func, tableModel.MappedName, col.MappedName);
                        }
                    }
                    cmd.CommandText = string.Format(Sql_UpdateTracking, tableModel.MappedName, pkWhere, _currentVersion, lastVersion, buffer.ToString());
                    App.LogDebug("Sql_UpdateTracking:{0}.", cmd.CommandText);
                    var e = new SqlRowChangedEventArgs(tableModel, SqlRowChangedTypes.Updated, cmd);
                    while (e.DataReader.Read())
                    {
                        OnUpdated(e);
                    }
                    e.DataReader.Close();
                }
                #endregion

                #region Delete
                foreach (var tableModel in _model)
                {
                    buffer.Length = 0;
                    buffer.AppendJoin(",", tableModel.PrimaryKey.Select(t => t.MappedName));
                    cmd.CommandText = string.Format(Sql_DeleteTracking, tableModel.MappedName, _currentVersion, lastVersion, buffer.ToString());
                    App.LogDebug("Sql_DeleteTracking:{0}.", cmd.CommandText);
                    var e = new SqlRowChangedEventArgs(tableModel, SqlRowChangedTypes.Deleted, cmd);
                    while (e.DataReader.Read())
                    {
                        OnDeleted(e);
                    }
                    e.DataReader.Close();
                }
                #endregion

                _currentVersion = lastVersion;
                this.OnPropertyChanged(new PropertyChangedEventArgs("CurrentVersion"));
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Connection.Close();
                }
            }
        }

        private string CreateSqlWhere(StringBuilder buffer, MetaTable model)
        {
            buffer.Length = 0;
            buffer.AppendFormat("t.{0}=ct.{0}", model.PrimaryKey[0].MappedName);
            for (int i = 1; i < model.PrimaryKey.Count; i++)
            {
                buffer.AppendFormat("AND t.{0}=ct.{0}", model.PrimaryKey[i].MappedName);
            }
            return buffer.ToString();
        }
        #endregion
    }
}