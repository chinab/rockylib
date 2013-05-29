using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Caching;
using Rocky;
using Rocky.Caching;
using ServiceStack.Redis;
using ServiceStack.Redis.Support;

namespace NoSQL
{
    /// <summary>
    /// IRedisTypedClient的JSON序列化不稳定
    /// </summary>
    internal sealed class RedisCache : DistributedCache
    {
        #region Enumerator
        public class Enumerator : IEnumerable<KeyValuePair<string, object>>, IDisposable
        {
            private IRedisClient _client;

            public Enumerator(IRedisClient client)
            {
                _client = client;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                var keys = _client.GetAllKeys();
                foreach (string key in keys)
                {
                    object value = _client.Get<object>(key);
                    if (value != null)
                    {
                        yield return new KeyValuePair<string, object>(key, value);
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public void Dispose()
            {
                _client.Dispose();
            }
        }
        #endregion

        #region Fields
        private PooledRedisClientManager _clientMgr;
        private OptimizedObjectSerializer _serializer;
        private System.Collections.Hashtable _region;
        #endregion

        #region Properties
        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get
            {
                return base.DefaultCacheCapabilities | System.Runtime.Caching.DefaultCacheCapabilities.CacheRegions;
            }
        }
        public override string KeyPrefix
        {
            get { return _clientMgr.NamespacePrefix; }
            set { _clientMgr.NamespacePrefix = value; }
        }
        public override int ConnectTimeout
        {
            get { return _clientMgr.ConnectTimeout.Value; }
            set { _clientMgr.ConnectTimeout = value; }
        }
        public override int SendReceiveTimeout
        {
            get
            {
                return _clientMgr.SocketReceiveTimeout.Value;
            }
            set
            {
                _clientMgr.SocketReceiveTimeout = value;
                _clientMgr.SocketSendTimeout = value;
            }
        }
        public override int RetryCount { get; set; }
        public override int RetryWaitTimeout { get; set; }
        #endregion

        #region Constructors
        public RedisCache(ServerNode[] nodes, int maxWritePoolSize = 10, int maxReadPoolSize = 24)
            : base(typeof(RedisCache).FullName, nodes)
        {
            _clientMgr = RedisHelper.ClientManager;
            int timeout = 2000;
            _clientMgr.ConnectTimeout = timeout;
            _clientMgr.SocketSendTimeout = timeout;
            _clientMgr.SocketReceiveTimeout = timeout;
            _serializer = new OptimizedObjectSerializer();
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _clientMgr.Dispose();
            }
            _clientMgr = null;
            _serializer = null;
        }
        #endregion

        #region Methods
        internal void SetRegion(string regionName, ServerNode node)
        {
            if (_region == null)
            {
                _region = new System.Collections.Hashtable();
            }
            _region[regionName] = node;
        }

        private IRedisClient GetClient(string regionName, bool readOnly)
        {
            IRedisClient client;
            if (regionName != null)
            {
                ServerNode node = null;
                if (_region == null || (node = (ServerNode)_region[regionName]) == null)
                {
                    ThrowException(string.Format("RegionName '{0}' isn't exists.", regionName));
                }
                client = RedisCacheClientFactory.Instance.CreateRedisClient(node.IPEndPoint.Address.ToString(), node.IPEndPoint.Port);
                if (node.Credentials != null)
                {
                    client.Password = node.Credentials.Password;
                }
            }
            else
            {
                client = readOnly ? _clientMgr.GetReadOnlyClient() : _clientMgr.GetClient();
                client.Prepare(base.ServerNodes);
            }
            return client;
        }
        #endregion

        #region Read
        public override bool Contains(string key, string regionName = null)
        {
            base.CheckParameters(key);

            using (var client = this.GetClient(regionName, true))
            {
                return client.ContainsKey(key);
            }
        }

        public override object Get(string key, string regionName = null)
        {
            base.CheckParameters(key);

            using (var client = this.GetClient(regionName, true))
            {
                byte[] data = client.Get<byte[]>(key);
                if (data.IsNullOrEmpty())
                {
                    return null;
                }
                return _serializer.Deserialize(data);
            }
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            if (!keys.Any())
            {
                return DistributedCache.EmptyDictionary;
            }

            using (var client = this.GetClient(regionName, true))
            {
                var dict = client.GetAll<byte[]>(keys);
                return dict.ToDictionary<KeyValuePair<string, byte[]>, string, object>(pair => pair.Key, pair => _serializer.Deserialize(pair.Value));
            }
        }

        public override long GetCount(string regionName = null)
        {
            using (var client = this.GetClient(regionName, true))
            {
                return client.GetAllKeys().Count;
            }
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return new Enumerator(this.GetClient(null, true)).GetEnumerator();
        }
        #endregion

        #region Write
        public override object Remove(string key, string regionName = null)
        {
            object serverValue = this.Get(key, regionName);
            if (serverValue != null)
            {
                using (var client = this.GetClient(regionName, false))
                {
                    client.Remove(key);
                }
            }
            return serverValue;
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            base.CheckParameters(key, value);
            if (policy == null)
            {
                policy = DistributedCache.CreatePolicy();
            }

            using (var client = this.GetClient(regionName, false))
            {
                byte[] data = _serializer.Serialize(value);
                if (policy.AbsoluteExpiration != DistributedCache.InfiniteAbsoluteExpiration)
                {
                    client.Set(key, data, policy.AbsoluteExpiration.DateTime);
                }
                else if (policy.SlidingExpiration != DistributedCache.NoSlidingExpiration)
                {
                    client.Set(key, data, policy.SlidingExpiration);
                }
                else
                {
                    client.Set(key, data);
                }
            }
        }

        public override void Replace(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            base.CheckParameters(key, value);
            if (policy == null)
            {
                policy = DistributedCache.CreatePolicy();
            }

            using (var client = this.GetClient(regionName, false))
            {
                byte[] data = _serializer.Serialize(value);
                if (policy.AbsoluteExpiration != DistributedCache.InfiniteAbsoluteExpiration)
                {
                    client.Replace(key, data, policy.AbsoluteExpiration.DateTime);
                }
                else if (policy.SlidingExpiration != DistributedCache.NoSlidingExpiration)
                {
                    client.Replace(key, data, policy.SlidingExpiration);
                }
                else
                {
                    client.Replace(key, data);
                }
            }
        }

        public override void FlushAll(string regionName = null)
        {
            using (var client = this.GetClient(regionName, false))
            {
                client.FlushAll();
            }
        }
        #endregion
    }
}