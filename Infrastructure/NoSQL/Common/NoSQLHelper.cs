using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using System.Collections.Concurrent;
using Rocky;
using Rocky.Data;
using Rocky.Caching;

namespace NoSQL
{
    public static class NoSQLHelper
    {
        internal static readonly EmitMapper.ObjectMapperManager MapperManager;
        private static ConcurrentBag<SqlRowChangeMonitor> Monitors;

        static NoSQLHelper()
        {
            MapperManager = EmitMapper.ObjectMapperManager.DefaultInstance;
            Monitors = new ConcurrentBag<SqlRowChangeMonitor>();
        }

        public static int EnumToValue<T>(T value) where T : struct
        {
            return Convert.ToInt32(value);
        }
        public static int? EnumToValue<T>(T? value) where T : struct
        {
            return value.HasValue ? Convert.ToInt32(value.Value) : (int?)null;
        }

        public static EmitMapper.Mappers.ObjectsMapperBaseImpl GetEntityMapper(Type from, Type to)
        {
            return MapperManager.GetMapperImpl(from, to, EmitMapper.MappingConfiguration.DefaultMapConfig.Instance);
        }

        /// <summary>
        /// 获取指定KeyPrefix的DistributedCache，请勿修改KeyPrefix属性
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        /// <returns>DistributedCache</returns>
        public static DistributedCache CreateCache(string databaseName, CacheProviderName providerName = CacheProviderName.Redis)
        {
            Type provider;
            switch (providerName)
            {
                case CacheProviderName.Redis:
                    provider = typeof(RedisCache);
                    break;
                default:
                    throw new NotSupportedException(providerName.ToDescription());
            }
            var cache = DistributedCache.CreateCache(provider);
            cache.KeyPrefix = string.Format("{0}:", databaseName);
            cache.ConnectTimeout = 1000 * 3;
            cache.SendReceiveTimeout = 1000 * 60;
            cache.RetryCount = 2;
            return cache;
        }

        /// <summary>
        /// 注册SQLServer ChangeTracking监控
        /// </summary>
        /// <typeparam name="TEntity">PO</typeparam>
        /// <param name="connectionString">数据库连接串</param>
        /// <param name="period">PollingInterval</param>
        public static void RegisterChangeMonitor<TEntity>(string connectionString, TimeSpan? period = null)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            var monitor = new SqlRowChangeMonitor(connectionString, new Type[] { typeof(TEntity) }, -1L, period);
            monitor.Error += (sender, e) =>
            {
                var ex = e.GetException();
                Runtime.LogError(ex, string.Format("{0}ChangeMonitor", typeof(TEntity).Name));
            };
            monitor.Updated += (sender, e) =>
            {
                var owner = (SqlRowChangeMonitor)sender;
                var client = CreateCache(owner.DatabaseName);
                var entity = e.GetRow<TEntity>();
                var keyMapper = new PrimaryKeyMapper(owner.Model.Single().PrimaryKey);
                var primaryKey = keyMapper.GenerateKey(entity);
                // 替换缓存的值
                client.Replace(primaryKey, entity, null);
            };
            monitor.Deleted += (sender, e) =>
            {
                var owner = (SqlRowChangeMonitor)sender;
                var client = CreateCache(owner.DatabaseName);
                var pkValues = e.GetPrimaryKey().Values.ToArray();
                var keyMapper = new PrimaryKeyMapper(owner.Model.Single().PrimaryKey);
                var primaryKey = keyMapper.GenerateKey(pkValues);
                // 替换缓存的相对过期时间，相对过期分钟 > QueryKey缓存绝对过期分钟
                client.Replace(primaryKey, null, new System.Runtime.Caching.CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(10.1D)
                });
            };
            Monitors.Add(monitor);
        }
    }
}