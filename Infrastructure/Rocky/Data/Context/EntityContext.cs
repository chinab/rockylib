using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections;
using System.Text.RegularExpressions;

namespace Rocky.Data
{
    /// <summary>
    /// BusinessObject to PersistentObject Queryable
    /// </summary>
    public abstract class EntityContext<T> : Disposable where T : class, new()
    {
        #region StaticMembers
        internal static readonly bool EnableLog;
        private static Hashtable _stored;
        private static Regex _regWithNoLock = new Regex(@"(] AS \[t\d+\])", RegexOptions.Compiled);

        static EntityContext()
        {
            _stored = Hashtable.Synchronized(new Hashtable());
            EnableLog = bool.TrueString.Equals(System.Configuration.ConfigurationManager.AppSettings["DbEnableLog"], StringComparison.OrdinalIgnoreCase);
        }

        protected static void SetDataTypeMap(Type bizType, Type dataType)
        {
            _stored[bizType] = dataType;
        }
        #endregion

        #region Fields
        private object _boxedContext;
        private DataContext _context;
        private IDictionary _trackerItems;
        #endregion

        #region Properties
        /// <summary>
        /// 延迟提交数量
        /// </summary>
        public byte LazySize { get; set; }
        protected T Context
        {
            get
            {
                base.CheckDisposed();
                return (T)_boxedContext;
            }
        }
        private IDictionary TrackerItems
        {
            get
            {
                if (_trackerItems == null)
                {
                    object services = _context.GetType().GetProperty("Services", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty)
                        .GetValue(_context, null);
                    var fieldFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
                    object tracker = services.GetType().GetField("tracker", fieldFlags).GetValue(services);
                    _trackerItems = (IDictionary)tracker.GetType().GetField("items", fieldFlags).GetValue(tracker);
                }
                return _trackerItems;
            }
        }
        #endregion

        #region Constructor
        public EntityContext(T context = null)
        {
            if (context == null)
            {
                context = new T();
            }
            _boxedContext = context;
            _context = _boxedContext as DataContext;
            if (_context == null)
            {
                throw new InvalidOperationException("The T must be DataContext.");
            }
            if (EnableLog)
            {
                _context.Log = new DbLogger();
            }
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                if (_context.ObjectTrackingEnabled)
                {
                    this.LazySize = 0;
                    this.SaveChanges();
                }
                _context.Dispose();
            }
            //_context = null;
            _boxedContext = null;
        }
        #endregion

        #region Methods
        /// <summary>
        /// BO Type to DO Type
        /// </summary>
        /// <param name="bizType"></param>
        /// <returns></returns>
        protected virtual Type MapDataType(Type bizType)
        {
            Type dataType = (Type)_stored[bizType];
            if (dataType == null)
            {
                throw new InvalidOperationException("bizType");
            }
            return dataType;
        }

        /// <summary>
        /// BO to DO
        /// </summary>
        /// <param name="bizObj"></param>
        /// <returns></returns>
        protected abstract void AssignDataObject(object dataObj, object bizObj);

        protected void Create<TEntity>(TEntity bizObj, Action<object> dataAction = null) where TEntity : class
        {
            Type fromType = typeof(TEntity), toType = MapDataType(fromType);
            var dataObj = Activator.CreateInstance(toType);
            AssignDataObject(dataObj, bizObj);
            _context.GetTable(toType).InsertOnSubmit(dataObj);

            if (dataAction != null)
            {
                dataAction(dataObj);
            }
        }

        protected void Update<TEntity>(TEntity bizObj, Action<object> dataAction = null) where TEntity : class
        {
            Type fromType = typeof(TEntity), toType = MapDataType(fromType);
            var metaType = _context.Mapping.GetMetaType(toType);
            var source = _context.GetTable(toType);
            var parameters = Expression.Parameter(toType, "item");
            PropertyInfo prop = fromType.GetProperty(metaType.IdentityMembers[0].Name, PropertyAccess.PropertyBinding);
            Expression body = Expression.Equal(Expression.Property(parameters, (PropertyInfo)metaType.IdentityMembers[0].Member), Expression.Constant(prop.GetValue(bizObj, null), prop.PropertyType));
            for (int i = 1; i < metaType.IdentityMembers.Count; i++)
            {
                prop = fromType.GetProperty(metaType.IdentityMembers[i].Name, PropertyAccess.PropertyBinding);
                body = Expression.AndAlso(body, Expression.Equal(Expression.Property(parameters, (PropertyInfo)metaType.IdentityMembers[i].Member), Expression.Constant(prop.GetValue(bizObj, null), prop.PropertyType)));
            }
            var predicate = Expression.Lambda(body, parameters);
            var dataObj = source.Provider.CreateQuery(
                Expression.Call(typeof(Queryable), "Where",
                new Type[] { toType },
                new Expression[] { source.Expression, Expression.Quote(predicate) })
                ).Cast<object>().SingleOrDefault();
            AssignDataObject(dataObj, bizObj);

            if (dataAction != null)
            {
                dataAction(dataObj);
            }
        }

        protected void Delete<TEntity>(TEntity bizObj, Action<object> dataAction = null) where TEntity : class
        {
            Type fromType = typeof(TEntity), toType = MapDataType(fromType);
            var dataObj = Activator.CreateInstance(toType);
            AssignDataObject(dataObj, bizObj);
            var table = _context.GetTable(toType);
            table.Attach(dataObj);
            table.DeleteOnSubmit(dataObj);

            if (dataAction != null)
            {
                dataAction(dataObj);
            }
        }

        protected TEntity QuerySingle<TEntity>(params object[] identityValues) where TEntity : class
        {
            if (identityValues.IsNullOrEmpty())
            {
                throw new ArgumentException("identityValues");
            }
            Type fromType = typeof(TEntity), toType = MapDataType(fromType);
            var metaType = _context.Mapping.GetMetaType(toType);
            if (metaType.IdentityMembers.Count != identityValues.Length)
            {
                throw new InvalidProgramException("IdentityMembers");
            }
            IQueryable<TEntity> source = Queryable<TEntity>();
            var parameters = Expression.Parameter(fromType, "item");
            Expression body = Expression.Equal(Expression.Property(parameters, fromType.GetMethod("get_" + metaType.IdentityMembers[0].Name)), Expression.Constant(identityValues[0], identityValues[0].GetType()));
            for (int i = 1; i < metaType.IdentityMembers.Count; i++)
            {
                body = Expression.AndAlso(body, Expression.Equal(Expression.Property(parameters, fromType.GetMethod("get_" + metaType.IdentityMembers[i].Name)), Expression.Constant(identityValues[i], identityValues[i].GetType())));
            }
            var predicate = Expression.Lambda(body, parameters);
            return source.Provider.CreateQuery<TEntity>(
                Expression.Call(typeof(Queryable), "Where",
                new Type[] { fromType },
                new Expression[] { source.Expression, Expression.Quote(predicate) })
                ).SingleOrDefault();
        }

        protected IQueryable<TEntity> Queryable<TEntity>(DataContext context = null) where TEntity : class
        {
            if (context == null)
            {
                context = _context;
            }

            Type fromType = typeof(TEntity), toType = MapDataType(fromType);
            string key = context.GetType().Name + fromType.FullName;
            Expression selector = (Expression)_stored[key];
            if (selector == null)
            {
                var parameters = Expression.Parameter(toType, "item");
                var q = from fromProperty in fromType.GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)
                        join toProperty in toType.GetProperties() on fromProperty.Name equals toProperty.Name
                        let b = fromProperty.PropertyType.IsAssignableFrom(toProperty.PropertyType)
                        let p = fromProperty.DeclaringType == fromType ? fromProperty.GetSetMethod() : fromProperty.DeclaringType.GetProperty(fromProperty.Name).GetSetMethod()
                        select b ?
                            Expression.Bind(p, Expression.Property(parameters, toProperty.GetGetMethod())) :
                            Expression.Bind(p, Expression.Convert(Expression.Property(parameters, toProperty.GetGetMethod()), fromProperty.PropertyType));

                var body = Expression.MemberInit(Expression.New(fromType), q.ToArray());
                _stored[key] = selector = Expression.Lambda(body, parameters);
            }
            IQueryable source = context.GetTable(toType);
            return source.Provider.CreateQuery<TEntity>(
                Expression.Call(typeof(Queryable), "Select",
                new Type[] { toType, fromType },
                new Expression[] { source.Expression, Expression.Quote(selector) })
                );
        }

        protected virtual void SaveChanges()
        {
            base.CheckDisposed();

            byte lazySize = this.LazySize;
            if (lazySize > 0)
            {
                var trackerItems = this.TrackerItems;
                PropertyInfo prop = null;
                foreach (object value in trackerItems.Values)
                {
                    if (prop == null)
                    {
                        prop = value.GetType().GetProperty("IsNew", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
                    }
                    if (Convert.ToBoolean(prop.GetValue(value, null)))
                    {
                        goto save;
                    }
                }
                if (lazySize > trackerItems.Count)
                {
                    return;
                }
            }
        save:
            try
            {
                _context.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            //catch (DuplicateKeyException ex)
            //{
            //    this.TrackerItems.Remove(ex.Object);
            //    goto save;
            //}
            catch (ChangeConflictException)
            {
                foreach (var conflict in _context.ChangeConflicts)
                {
                    // 使用当前数据库中的值，覆盖Linq缓存中实体对象的值
                    //conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                    // 使用Linq缓存中实体对象的值，覆盖当前数据库中的值
                    //conflict.Resolve(RefreshMode.KeepCurrentValues);
                    // 只更新实体对象中改变的字段的值，其他的保留不变
                    conflict.Resolve(RefreshMode.KeepChanges);
                }
                goto save;
            }
        }
        #endregion

        #region ExecuteQuery
        protected DbCommand PrepareCommand(IQueryable query, bool withNoLock)
        {
            var command = _context.GetCommand(query) as System.Data.SqlClient.SqlCommand;
            if (command == null)
            {
                throw new NotSupportedException("PrepareCommand WithNoLock");
            }
            if (withNoLock)
            {
                string cmdText = command.CommandText;
                IEnumerable<Match> matches = _regWithNoLock.Matches(cmdText).Cast<Match>().OrderByDescending(m => m.Index);
                foreach (Match m in matches)
                {
                    int splitIndex = m.Index + m.Value.Length;
                    cmdText =
                        cmdText.Substring(0, splitIndex) + " WITH (NOLOCK)" +
                        cmdText.Substring(splitIndex);
                }
                command.CommandText = cmdText;
            }
            return command;
        }

        /// <summary>
        /// LoadOptions
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="withNoLock"></param>
        /// <returns></returns>
        public List<TEntity> ExecuteQuery<TEntity>(IQueryable query, bool withNoLock = false)
        {
            var cmd = PrepareCommand(query, withNoLock);
            bool isClosed = cmd.Connection.State == System.Data.ConnectionState.Closed;
            try
            {
                if (isClosed)
                {
                    cmd.Connection.Open();
                }
                using (var dr = cmd.ExecuteReader())
                {
                    return _context.Translate<TEntity>(dr).ToList();
                }
            }
            finally
            {
                if (isClosed)
                {
                    cmd.Connection.Close();
                }
            }
        }
        #endregion
    }
}