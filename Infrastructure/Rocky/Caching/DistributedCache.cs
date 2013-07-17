using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Caching;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace System.Caching
{
    /// <summary>
    /// 分布式缓存基类
    /// PS: 性能监控请用EntLib中的PolicyInjection实现
    /// </summary>
    public abstract class DistributedCache : ObjectCache, IDisposable
    {
        #region Static
        public static readonly ServerNode[] DefaultCacheServerNodes;
        protected static IDictionary<string, object> EmptyDictionary;
        private static double DefaultPolicyExpiresMinutes;

        static DistributedCache()
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
            if (DefaultCacheServerNodes.Length == 0)
            {
                throw new InvalidOperationException("DefaultCacheServerNodes");
            }
            DefaultCacheServerNodes[0].ReadOnly = false;

            if (!double.TryParse(ConfigurationManager.AppSettings["DefaultPolicyExpiresMinutes"], out DefaultPolicyExpiresMinutes))
            {
                DefaultPolicyExpiresMinutes = 10D;
            }
            EmptyDictionary = new Dictionary<string, object>(0);
        }

        /// <summary>
        /// 创建默认缓存策略
        /// </summary>
        /// <param name="minutesExpiration">过期分钟</param>
        /// <param name="isSliding">是否相对过期</param>
        /// <returns>CacheItemPolicy</returns>
        public static CacheItemPolicy CreatePolicy(double? minutesExpiration = null, bool isSliding = false)
        {
            var policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.NotRemovable
            };
            if (isSliding)
            {
                policy.SlidingExpiration = TimeSpan.FromMinutes(minutesExpiration.GetValueOrDefault(DefaultPolicyExpiresMinutes));
            }
            else
            {
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(minutesExpiration.GetValueOrDefault(DefaultPolicyExpiresMinutes));
            }
            return policy;
        }

        /// <summary>
        /// 创建缓存提供者
        /// </summary>
        /// <param name="provider">提供者名称</param>
        /// <param name="serverNodes">服务端节点</param>
        /// <returns>抽象类DistributedCache</returns>
        public static DistributedCache CreateCache(Type provider, ServerNode[] serverNodes = null)
        {
            if (serverNodes.IsNullOrEmpty())
            {
                serverNodes = DefaultCacheServerNodes;
            }

            var cache = (DistributedCache)Activator.CreateInstance(provider, serverNodes);
            cache._providerInvariantName = provider.FullName;
            return cache;
        }
        #endregion

        #region Events
        /// <summary>
        /// 预料错误事件
        /// </summary>
        public event ErrorEventHandler Error;

        protected virtual void OnError(ErrorEventArgs e)
        {
            if (this.Error != null)
            {
                this.Error(this, e);
            }
            else
            {
                var ex = e.GetException();
                ThrowException(innerEx: ex);
            }
        }
        #endregion

        #region Fields
        protected string _name;
        private string _providerInvariantName;
        private bool _disposed;
        private ServerNode[] _serverNodes;
        #endregion

        #region Properties
        /// <summary>
        /// OutOfProcessProvider | AbsoluteExpirations | SlidingExpirations
        /// </summary>
        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get
            {
                return System.Runtime.Caching.DefaultCacheCapabilities.OutOfProcessProvider
                    | System.Runtime.Caching.DefaultCacheCapabilities.AbsoluteExpirations
                    | System.Runtime.Caching.DefaultCacheCapabilities.SlidingExpirations;
            }
        }
        /// <summary>
        /// 实例名称
        /// </summary>
        public sealed override string Name
        {
            get { return _name; }
        }
        /// <summary>
        /// 服务群节点
        /// </summary>
        public ServerNode[] ServerNodes
        {
            get { return _serverNodes; }
        }
        /// <summary>
        /// Cache Key前缀
        /// </summary>
        public abstract string KeyPrefix { get; set; }
        /// <summary>
        /// 连接超时毫秒
        /// </summary>
        public abstract int ConnectTimeout { get; set; }
        /// <summary>
        /// 发送&接收超时毫秒
        /// </summary>
        public abstract int SendReceiveTimeout { get; set; }
        /// <summary>
        /// 失败重试次数
        /// </summary>
        public abstract int RetryCount { get; set; }
        /// <summary>
        /// 失败重试停留毫秒
        /// </summary>
        public abstract int RetryWaitTimeout { get; set; }
        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="key">Cache Key</param>
        /// <returns>Cache Value</returns>
        public sealed override object this[string key]
        {
            get
            {
                return this.Get(key);
            }
            set
            {
                this.Set(key, value, DistributedCache.CreatePolicy());
            }
        }
        #endregion

        #region Constructors
        public DistributedCache(string name, ServerNode[] serverNodes)
        {
            if (serverNodes.IsNullOrEmpty())
            {
                throw new ArgumentException("serverNodes");
            }

            _serverNodes = serverNodes;
        }
        ~DistributedCache()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                DisposeInternal(disposing);
                _disposed = true;
            }
        }
        /// <summary>
        /// Free native resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to Dispose managed resources</param>
        protected virtual void DisposeInternal(bool disposing)
        {
            //if (disposing)
            //{

            //}
            _serverNodes = null;
            this.Error = null;
        }

        /// <summary>
        /// 释放native resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region ExceptionMethods
        [DebuggerStepThrough]
        protected virtual void CheckParameters(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
        }
        [DebuggerStepThrough]
        protected virtual void CheckParameters(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
        }

        [DebuggerStepThrough]
        protected void CheckDisposed()
        {
            if (_disposed)
            {
                string typeName = this.GetType().FullName;
                throw new ObjectDisposedException(typeName, string.Format("Cannot access a disposed {0}.", typeName));
            }
        }

        [DebuggerStepThrough]
        protected void ThrowException(string msg = "DistributedCache", Exception innerEx = null)
        {
            throw new DistributedException(msg, innerEx)
            {
                ProviderInvariantName = _providerInvariantName
            };
        }
        #endregion

        #region Methods
        public sealed override bool Add(CacheItem item, CacheItemPolicy policy)
        {
            return this.Add(item.Key, item.Value, policy, item.RegionName);
        }
        public sealed override bool Add(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            var policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = absoluteExpiration;
            return this.Add(key, value, policy, regionName);
        }

        public sealed override CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
        {
            value.Value = this.AddOrGetExisting(value.Key, value.Value, policy, value.RegionName);
            return value;
        }
        /// <summary>
        /// 如果存在具有相同键的缓存项，则为指定的缓存项；否则向缓存中插入缓存项。
        /// </summary>
        /// <param name="key">该缓存项的唯一标识符。</param>
        /// <param name="value">要插入的对象。</param>
        /// <param name="policy">一个包含该缓存项的逐出详细信息的对象。此对象提供比简单绝对过期更多的逐出选项。</param>
        /// <param name="regionName">可选。缓存中的一个可用来添加缓存项的命名区域（如果实现了区域）。可选参数的默认值为 null。</param>
        /// <returns>如果存在具有相同键的缓存项，则为指定的缓存项；否则为 null。</returns>
        public sealed override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            object serverValue = this.Get(key, regionName);
            if (serverValue == null)
            {
                this.Set(key, value, policy, regionName);
            }
            return serverValue;
        }
        public sealed override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            var policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = absoluteExpiration;
            return this.AddOrGetExisting(key, value, policy, regionName);
        }

        public sealed override CacheItem GetCacheItem(string key, string regionName = null)
        {
            object value = this.Get(key, regionName);
            return new CacheItem(key, value, regionName);
        }

        public sealed override void Set(CacheItem item, CacheItemPolicy policy)
        {
            this.Set(item.Key, item.Value, policy, item.RegionName);
        }
        public sealed override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            var policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = absoluteExpiration;
            this.Set(key, value, policy, regionName);
        }

        public abstract void Replace(string key, object value, CacheItemPolicy policy, string regionName = null);
        public abstract void FlushAll(string regionName = null);

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            return new CacheChangeMonitor(keys, regionName);
        }
        #endregion
    }
}