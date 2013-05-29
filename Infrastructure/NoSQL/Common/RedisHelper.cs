using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using Rocky.Caching;
using Rocky.Net;
using ServiceStack.Redis;

namespace NoSQL
{
    /// <summary>
    /// ServiceStack.Redis的JSON序列化会出现数据不完整的情况，如果类比较复杂推荐用Binary序列化
    /// </summary>
    internal static class RedisHelper
    {
        #region Fields
        /// <summary>
        /// 唯一读写管理池
        /// </summary>
        public static readonly PooledRedisClientManager ClientManager;
        /// <summary>
        /// 配置中的服务节点
        /// </summary>
        private static readonly ServerNode[] DefaultCacheServerNodes;
        /// <summary>
        /// 配置中的过期分钟
        /// </summary>
        private static readonly double DefaultPolicyExpiresMinutes;
        #endregion

        #region Constractor
        static RedisHelper()
        {
            var q = from t in ConfigurationManager.AppSettings["DefaultCacheServerNodes"].Split(',')
                    let val = t.Split('@')
                    let cred = val.Length == 2 ? val[0].Split(':') : null
                    select new ServerNode
                    {
                        Credentials = cred.Length == 2 ? new NetworkCredential(cred[0].Trim(), cred[1].Trim()) : null,
                        IPEndPoint = SocketHelper.ParseEndPoint(val[1].Trim()),
                        ReadOnly = true
                    };
            DefaultCacheServerNodes = q.ToArray();
            if (DefaultCacheServerNodes.Length < 2)
            {
                throw new InvalidOperationException("必须使用2+个Redis服务节点");
            }
            DefaultCacheServerNodes[0].ReadOnly = false;

            if (!double.TryParse(ConfigurationManager.AppSettings["DefaultPolicyExpiresMinutes"], out DefaultPolicyExpiresMinutes))
            {
                DefaultPolicyExpiresMinutes = 10D;
            }

            ClientManager = CreateClientManager();
        }
        #endregion

        #region Methods
        /// <summary>
        /// 创建读写管理池，默认读写比例7:3
        /// </summary>
        /// <param name="maxWritePoolSize"></param>
        /// <param name="maxReadPoolSize"></param>
        /// <returns></returns>
        private static PooledRedisClientManager CreateClientManager(int maxWritePoolSize = 10, int maxReadPoolSize = 24)
        {
            var nodes = DefaultCacheServerNodes;
            IEnumerable<string> readWriteHosts = nodes.Where(t => !t.ReadOnly).Select(t => t.IPEndPoint.ToString()),
                readOnlyHosts = nodes.Where(t => t.ReadOnly).Select(t => t.IPEndPoint.ToString());
            if (!readWriteHosts.Any())
            {
                throw new InvalidOperationException("至少单个Redis可写服务节点");
            }

            if (!readOnlyHosts.Any())
            {
                readOnlyHosts = new string[] { readWriteHosts.First() };
            }
            return new PooledRedisClientManager(readWriteHosts, readOnlyHosts, new RedisClientManagerConfig()
            {
                MaxWritePoolSize = maxWritePoolSize,
                MaxReadPoolSize = maxReadPoolSize,
                AutoStart = true
            });
        }
        /// <summary>
        /// 创建读写客户端
        /// </summary>
        /// <param name="ipe"></param>
        /// <returns></returns>
        public static IRedisClient CreateClient(string ipe)
        {
            var ipEndPoint = SocketHelper.ParseEndPoint(ipe);
            var client = RedisClientFactory.Instance.CreateRedisClient(ipEndPoint.Address.ToString(), ipEndPoint.Port);
            client.Prepare();
            return client;
        }

        /// <summary>
        /// 创建绝对过期时间
        /// </summary>
        /// <param name="minutesExpiration">过期分钟</param>
        /// <returns></returns>
        public static DateTime CreateExpiresAt(double? minutesExpiration = null)
        {
            return DateTime.UtcNow.AddMinutes(minutesExpiration.GetValueOrDefault(DefaultPolicyExpiresMinutes));
        }
        /// <summary>
        /// 创建相对过期时间
        /// </summary>
        /// <param name="minutesExpiration">过期分钟</param>
        /// <returns></returns>
        public static TimeSpan CreateExpiresIn(double? minutesExpiration = null)
        {
            return TimeSpan.FromMinutes(minutesExpiration.GetValueOrDefault(DefaultPolicyExpiresMinutes));
        }
        #endregion

        #region Extensions
        /// <summary>
        /// 准备客户端，主要从节点中获取验证信息；
        /// 服务节点没找到则会引发异常
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="serverNodes"></param>
        public static void Prepare(this IRedisClient instance, ServerNode[] serverNodes = null)
        {
            if (serverNodes == null)
            {
                serverNodes = DefaultCacheServerNodes;
            }
            var node = serverNodes.Where(t => t.IPEndPoint.Address.ToString() == instance.Host && t.IPEndPoint.Port == instance.Port).Single();
            if (node.Credentials != null)
            {
                instance.Password = node.Credentials.Password;
            }
        }

        /// <summary>
        /// 获取读客户端，已Prepare()
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IRedisClient GetReadClient(this IRedisClientsManager instance)
        {
            var client = instance.GetReadOnlyClient();
            client.Prepare();
            return client;
        }

        /// <summary>
        /// 获取写客户端，已Prepare()
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IRedisClient GetWriteClient(this IRedisClientsManager instance)
        {
            var client = instance.GetClient();
            client.Prepare();
            return client;
        }
        #endregion
    }
}