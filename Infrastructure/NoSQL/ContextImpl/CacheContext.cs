using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.IO;
using System.Reflection;
using System.Linq.Expressions;
using Rocky;
using Rocky.Data;
using Rocky.Caching;

namespace NoSQL
{
    /// <summary>
    /// PS: DataContext用的是PO，但DistributedCache用的是DTO；CacheContext这里是使用的无主外键关系的PO。
    /// </summary>
    /// <typeparam name="TDataContext"></typeparam>
    public sealed class CacheContext<TDataContext> : EntityContext<TDataContext>, ICacheContext where TDataContext : DataContext, new()
    {
        #region Fields
        private static readonly MethodInfo _LinqWhere = typeof(Queryable).GetMethods().Where(item => item.Name == "Where").First();
        private TDataContext _context;
        private DistributedCache _cache;
        private ICacheKeyGenerator _queryKeyGenerator;
        #endregion

        #region Properties
        /// <summary>
        /// DataAccess DLL名称，随意更改会出异常
        /// <example>Soubisc.DataAccess.{0}, Soubisc.DataAccess</example>
        /// </summary>
        public string DataAccessFormat { get; private set; }
        public TDataContext Linq
        {
            get { return _context; }
        }
        DataContext ICacheContext.Database
        {
            get { return _context; }
        }
        DistributedCache ICacheContext.Cache
        {
            get { return _cache; }
        }
        public bool ReadOnly
        {
            get { return !_context.ObjectTrackingEnabled; }
            set { _context.ObjectTrackingEnabled = !value; }
        }
        public bool QueryNoLock { get; set; }
        #endregion

        #region Constructors
        public CacheContext(string dataAccessFormat)
        {
            this.DataAccessFormat = dataAccessFormat;
            _context = base.Context;
            this.ReadOnly = true;
            _cache = NoSQLHelper.CreateCache(_context.Mapping.DatabaseName);
            _queryKeyGenerator = new CacheKeyGenerator();
            _queryKeyGenerator.AutoHash = true;
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _cache.Dispose();
            }
            _cache = null;
            _queryKeyGenerator = null;
            base.DisposeInternal(disposing);
        }
        #endregion

        #region BaseCore
        protected override Type MapDataType(Type bizType)
        {
            try
            {
                return base.MapDataType(bizType);
            }
            catch (InvalidOperationException)
            {
                int i = bizType.Name.LastIndexOf("Entity");
                if (i == -1)
                {
                    throw new InvalidOperationException("BO Mapping");
                }
                Type dataType = Type.GetType(string.Format(DataAccessFormat, bizType.Name.Remove(i)), false);
                if (dataType == null)
                {
                    Type objType = typeof(object);
                    while (dataType == null && bizType.BaseType != objType)
                    {
                        bizType = bizType.BaseType;
                        i = bizType.Name.LastIndexOf("Entity");
                        if (i == -1)
                        {
                            continue;
                        }
                        dataType = Type.GetType(string.Format(DataAccessFormat, bizType.Name.Remove(i)), false);
                    }

                    if (dataType == null)
                    {
                        throw new InvalidOperationException("BO Mapping");
                    }
                }
                SetDataTypeMap(bizType, dataType);
                return dataType;
            }
        }
        protected override void AssignDataObject(object dataObj, object bizObj)
        {
            Type fromType = bizObj.GetType(), toType = MapDataType(fromType);
            var mapper = NoSQLHelper.MapperManager.GetMapperImpl(fromType, toType, EmitMapper.MappingConfiguration.DefaultMapConfig.Instance);
            mapper.Map(bizObj, dataObj, null);
        }

        public TTO MapEntity<TFrom, TTO>(TFrom from, TTO to)
        {
            return MapEntity(from, to, null);
        }
        /// <summary>
        /// http://emitmapper.codeplex.com/wikipage?title=Customization%20using%20default%20configurator
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTO"></typeparam>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="ignoreNames">"RowID" int</param>
        /// <returns></returns>
        public TTO MapEntity<TFrom, TTO>(TFrom from, TTO to, string[] ignoreNames)
        {
            Type tFrom = typeof(TFrom), tTo = typeof(TTO);
            EmitMapper.Mappers.ObjectsMapperBaseImpl mapper = null;
            if (ignoreNames.IsNullOrEmpty())
            {
                mapper = NoSQLHelper.MapperManager.GetMapperImpl(tFrom, tTo, EmitMapper.MappingConfiguration.DefaultMapConfig.Instance);
            }
            else
            {
                mapper = NoSQLHelper.MapperManager.GetMapperImpl(tFrom, tTo, new EmitMapper.MappingConfiguration.DefaultMapConfig().IgnoreMembers(tFrom, tTo, ignoreNames));
            }
            return (TTO)mapper.Map(from, to, null);
        }
        #endregion

        #region Methods
        private string GenerateQueryKey(DbCommand cmd)
        {
            string longKey = cmd.CommandText;
            if (cmd.Parameters.Count > 0)
            {
                var buffer = new StringBuilder(longKey).AppendLine();
                foreach (DbParameter param in cmd.Parameters)
                {
                    switch (param.Direction)
                    {
                        case ParameterDirection.Input:
                        case ParameterDirection.InputOutput:
                            if (param.Value != null)
                            {
                                buffer.AppendFormat("{0}:{1},", param.ParameterName, param.Value);
                            }
                            break;
                    }
                }
                buffer.Length--;
                longKey = buffer.ToString();
            }
            _queryKeyGenerator.HashKey(ref longKey);

            Runtime.LogDebug("GenerateQueryKey:{0}.", longKey);

            return longKey;
        }

        public ICacheKeyMapper CreateKeyMapper<TEntity>()
        {
            var model = MetaTable.GetTable(typeof(TEntity));
            return new PrimaryKeyMapper(model.PrimaryKey);
        }

        public void Initialize<TEntity>(IQueryable<TEntity> query) where TEntity : class
        {
            var model = MetaTable.GetTable(typeof(TEntity));
            var keyMapper = new PrimaryKeyMapper(model.PrimaryKey);
            foreach (var entity in query)
            {
                _cache.AddOrGetExisting(keyMapper.GenerateKey(entity), entity, DistributedCache.InfiniteAbsoluteExpiration);
            }
        }

        /// <summary>
        /// Delete all cache items, and save db changes.
        /// This only used 4 debug.
        /// </summary>
        [System.Diagnostics.Conditional(Runtime.DebugSymbal)]
        public void FlushAll()
        {
            _cache.FlushAll();
            if (!this.ReadOnly)
            {
                base.LazySize = 0;
                base.SaveChanges();
            }
        }
        #endregion

        #region Linq
        /// <summary>
        /// 从数据库加载
        /// </summary>
        /// <param name="model"></param>
        /// <param name="value"></param>
        /// <param name="sourceCmd"></param>
        /// <returns>DataRow</returns>
        /// <exception cref="System.Data.Linq.ChangeConflictException">数据库表中该行已被删除</exception>
        private object LoadItem(MetaTable model, Array value, DbCommand sourceCmd)
        {
            Func<PropertyInfo, object, LambdaExpression> func = (prop, constValue) =>
            {
                var param = Expression.Parameter(model.EntityType, "item");
                var propLeft = Expression.Property(param, prop);
                if (constValue.GetType() != prop.PropertyType)
                {
                    constValue = Convert.ChangeType(constValue, prop.PropertyType);
                }
                var valueRight = Expression.Constant(constValue, prop.PropertyType);
                var body = Expression.Equal(propLeft, valueRight);
                return Expression.Lambda(body, param);
            };

            IQueryable query = _context.GetTable(model.EntityType);
            for (int i = 0; i < value.Length; i++)
            {
                query = query.Provider.CreateQuery(Expression.Call(null, _LinqWhere.MakeGenericMethod(model.EntityType), new Expression[] { query.Expression, Expression.Quote(func(model.PrimaryKey[i].EntityProperty, value.GetValue(i))) }));
            }
            var cmd = PrepareCommand(query, false);
            cmd.Connection = sourceCmd.Connection;
            cmd.Transaction = sourceCmd.Transaction;

            Runtime.LogDebug(string.Format("LoadItem:{0}.", cmd.CommandText));

            var tor = _context.Translate(model.EntityType, cmd.ExecuteReader()).GetEnumerator();
            if (!tor.MoveNext())
            {
                throw new ChangeConflictException("数据库表中该行已被删除");
            }
            return tor.Current;
        }

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<TEntity> ExecuteQuery<TEntity>(IQueryable<TEntity> query) where TEntity : class
        {
            LinqResolver resolver;
            var resultSet = ExecuteQuery(query, out resolver);
            int modelCount = resolver.QueriedModels.Count;
            if (modelCount == 1)
            {
                if (resolver.QueriedModels[0].EntityType == query.ElementType)
                {
                    return resultSet.Cast<TEntity>();
                }
                else
                {
                    return resultSet.Select(item => (TEntity)resolver.SelectAssign(item));
                }
            }
            else
            {
                var list = new List<TEntity>((int)Math.Ceiling((double)resultSet.Length / modelCount));
                object[] param = new object[modelCount];
                for (int i = 0, j; i < resultSet.Length; i += modelCount)
                {
                    for (j = 0; j < modelCount; j++)
                    {
                        param[j] = resultSet[i + j];
                    }
                    list.Add((TEntity)resolver.SelectAssign(param));
                }
                return list;
            }
        }
        private object[] ExecuteQuery(IQueryable query, out LinqResolver resolver)
        {
            resolver = new LinqResolver(query);
            var cmd = PrepareCommand(query, this.QueryNoLock);
            string queryKey = GenerateQueryKey(cmd);
            var primaryKeys = (List<string>)_cache.Get(queryKey);
            if (primaryKeys == null)
            {
                try
                {
                    cmd.Connection.Open();
                    //发布事务锁定读取，但不锁定写入；通过数据库事务来避免并发下此处的多次重复写入DistributedCache
                    cmd.Transaction = cmd.Connection.BeginTransaction(IsolationLevel.ReadCommitted);

                    primaryKeys = (List<string>)_cache.Get(queryKey);
                    if (primaryKeys == null)
                    {
                        primaryKeys = new List<string>();
                        #region InsertPrimaryKey
                        var buffer = new StringBuilder(cmd.CommandText);
                        string tValue = "[t", fValue = "FROM ";
                        int offset = 7;
                        offset = cmd.CommandText.IndexOf(tValue, offset);
                        int maxTIndex = int.Parse(cmd.CommandText.Substring(offset + tValue.Length, 1));
                        do
                        {
                            offset = cmd.CommandText.IndexOf(fValue, offset);
                            buffer.Insert(offset, " ");
                            bool withOrdinal = cmd.CommandText.Substring(offset + fValue.Length, 1) == "[";
                            if (withOrdinal)
                            {
                                for (int i = 0; i < resolver.QueriedModels.Count; i++)
                                {
                                    foreach (var pk in resolver.QueriedModels[i].PrimaryKey)
                                    {
                                        buffer.Insert(offset, string.Format(",t{0}.[{1}] '{2}'", i, pk.MappedName, pk.FullName));
                                    }
                                }
                                cmd.CommandText = buffer.ToString();
                                break;
                            }

                            foreach (var pk in resolver.QueriedModels.SelectMany(t => t.PrimaryKey))
                            {
                                string sqlField = string.Format(",t{0}.[{1}]", maxTIndex, pk.FullName);
                                buffer.Insert(offset, sqlField);
                                offset += sqlField.Length;
                            }
                            offset += fValue.Length;
                            maxTIndex--;
                            cmd.CommandText = buffer.ToString();
                        }
                        while (offset != -1);
                        #endregion
                        Runtime.LogDebug("ExecuteQuery:{0}.", cmd.CommandText);

                        var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            foreach (var model in resolver.QueriedModels)
                            {
                                Array pkArray = model.PrimaryKey.Select(pk => dr[pk.FullName]).ToArray();
                                var keyMapper = new PrimaryKeyMapper(model.PrimaryKey);
                                string key = keyMapper.GenerateKey(pkArray);
                                var item = _cache.Get(key);
                                if (item == null)
                                {
                                    try
                                    {
                                        item = LoadItem(model, pkArray, cmd);
                                        _cache.AddOrGetExisting(key, item, DistributedCache.InfiniteAbsoluteExpiration);
                                    }
                                    catch (ChangeConflictException)
                                    {
                                        //数据库表中该行已被删除
                                        continue;
                                    }
                                }
                                primaryKeys.Add(key);
                            }
                        }
                    }
                }
                finally
                {
                    cmd.Transaction.Dispose();
                    cmd.Connection.Close();
                }
                if (primaryKeys != null)
                {
                    _cache.Set(queryKey, primaryKeys, DateTimeOffset.Now.AddMinutes(10D));
                }
            }
            // 因为无法得到DistributedCache server remove callback，所以在查询时检测结果集的有效性。
            var resultSet = _cache.GetValues(primaryKeys).Values.ToArray();
            try
            {
                for (int i = 0; i < resultSet.Length; i++)
                {
                    if (resultSet[i] == null)
                    {
                        if (cmd.Connection.State != ConnectionState.Open)
                        {
                            cmd.Connection.Open();
                        }

                        MetaTable model;
                        var value = PrimaryKeyMapper.ResolveKey(primaryKeys[i], out model);
                        try
                        {
                            resultSet[i] = LoadItem(model, value, cmd);
                            _cache.AddOrGetExisting(primaryKeys[i], resultSet[i], DistributedCache.InfiniteAbsoluteExpiration);
                        }
                        catch (ChangeConflictException)
                        {
                            //数据库表中该行已被删除
                            continue;
                        }
                    }
                }
            }
            finally
            {
                if (cmd.Connection.State != ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
            return resultSet;
        }
        #endregion
    }
}