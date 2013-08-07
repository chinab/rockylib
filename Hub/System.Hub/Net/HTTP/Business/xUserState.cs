using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text;
using System.Threading;

namespace System.Net
{
    public sealed class xUserState : Disposable
    {
        #region NestedTypes
        [Serializable]
        public sealed class DeviceIdentity
        {
            [NonSerialized]
            internal AutoResetEvent WaitHandle;
            [NonSerialized]
            internal ReverseListenState ListenState;

            public Guid ID { get; private set; }
            public IPAddress WAN { get; set; }
            public IPAddress LAN { get; set; }

            public DeviceIdentity()
            {
                this.ID = Guid.NewGuid();
                WaitHandle = new AutoResetEvent(false);
            }
        }

        [Serializable]
        internal struct ReverseListenState
        {
            public Guid AgentSock { get; set; }
            public IPEndPoint AgentDirect { get; set; }
        }
        #endregion

        #region Fields
        private static readonly MemoryCache _udpClients = new MemoryCache(typeof(xUserState).FullName);
        private SynchronizedCollection<DeviceIdentity> _principal;
        private ConcurrentDictionary<Guid, TcpClient> _clients;
        private SynchronizedCollection<string> _udpClientKeys;
        #endregion

        #region Properties
        /// <summary>
        /// 可用流量bytes
        /// 1073741824
        /// </summary>
        public uint MaximumFlowRate { get; internal set; }
        /// <summary>
        /// 客户端设备,ToArray()SynchronizedCollection未标记可序列化
        /// </summary>
        public ICollection<DeviceIdentity> Principal
        {
            get { return _principal.ToArray(); }
        }
        public ICollection<TcpClient> Clients
        {
            get { return _clients.Values; }
        }
        public ICollection<UdpClient> UdpClients
        {
            get
            {
                var dict = _udpClients.GetValues(_udpClientKeys);
                return dict.Values.Cast<UdpClient>().ToArray();
            }
        }
        #endregion

        #region Constructors
        public xUserState()
        {
            _principal = new SynchronizedCollection<DeviceIdentity>();
            _clients = new ConcurrentDictionary<Guid, TcpClient>();
            _udpClientKeys = new SynchronizedCollection<string>();
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                this.Kill();
            }
            _clients = null;
            _udpClientKeys = null;
        }
        #endregion

        #region Methods
        public Guid SignIn(IPAddress WAN_addr, IPAddress LAN_addr)
        {
            Contract.Requires(WAN_addr != null);
            base.CheckDisposed();

            lock (_principal.SyncRoot)
            {
                if (_principal.Count >= xHttpHandler.MaxDevice)
                {
                    _principal.RemoveAt(0);
                    this.Kill();
                }
                var q = from t in _principal
                        where t.WAN.Equals(WAN_addr) && t.LAN.Equals(LAN_addr)
                        select t;
                var identity = q.SingleOrDefault();
                if (identity == null)
                {
                    identity = new DeviceIdentity()
                    {
                        WAN = WAN_addr,
                        LAN = LAN_addr
                    };
                    _principal.Add(identity);
                }
                else
                {
                    identity.WaitHandle.Set();
                    Hub.LogInfo("SignIn: 该设备已登录");
                    Thread.Sleep(2000);
                }
                return identity.ID;
            }
        }

        public void SignOut(Guid deviceID)
        {
            Contract.Requires(deviceID != Guid.Empty);
            base.CheckDisposed();

            lock (_principal.SyncRoot)
            {
                var q = from t in _principal
                        where t.ID == deviceID
                        select t;
                var identity = q.SingleOrDefault();
                if (identity == null)
                {
                    return;
                }
                identity.WaitHandle.Close();
                _principal.Remove(identity);
            }
        }

        public void Kill()
        {
            base.CheckDisposed();

            lock (this)
            {
                foreach (Guid sock in _clients.Keys)
                {
                    this.Disconnect(sock);
                }
                foreach (string clientKey in _udpClientKeys)
                {
                    var client = (UdpClient)_udpClients.Remove(clientKey);
                    if (client != null)
                    {
                        client.Client.Close();
                    }
                }
            }
        }
        #endregion

        #region Tcp
        public TcpClient Connect(Guid sock, IPEndPoint destIpe)
        {
            Contract.Requires(destIpe != null);
            base.CheckDisposed();

            var proxyClient = new TcpClient();
            proxyClient.Connect(destIpe);
            if (!_clients.TryAdd(sock, proxyClient))
            {
                throw new InvalidOperationException(string.Format("Tcp connect {0} invalid", destIpe));
            }
            //5分钟
            const ulong keepalive_interval = 300000;
            proxyClient.Client.SetKeepAlive(keepalive_interval, keepalive_interval);
            return proxyClient;
        }

        public TcpClient GetClient(Guid sock, bool throwError = true)
        {
            base.CheckDisposed();

            TcpClient proxyClient;
            if (!_clients.TryGetValue(sock, out proxyClient) && throwError)
            {
                throw new InvalidOperationException(string.Format("Tcp get client({0}) invalid", sock));
            }
            return proxyClient;
        }

        public void Disconnect(Guid sock)
        {
            base.CheckDisposed();

            TcpClient proxyClient;
            if (_clients.TryRemove(sock, out proxyClient))
            {
                proxyClient.Close();
            }
        }
        #endregion

        #region Reverse
        internal void PushReverseListen(Guid deviceID, Guid agentSock, IPEndPoint agentDirect)
        {
            base.CheckDisposed();

            var device = _principal.SingleOrDefault(t => t.ID == deviceID);
            if (device == null || device.ListenState.AgentSock != Guid.Empty)
            {
                throw new InvalidOperationException("PushReverseListen");
            }
            device.ListenState.AgentSock = agentSock;
            device.ListenState.AgentDirect = agentDirect;
            device.WaitHandle.Set();
            Hub.LogInfo("PushReverseListen {0} {1}", agentSock, agentDirect);
        }

        internal ReverseListenState PopReverseListen(Guid deviceID)
        {
            base.CheckDisposed();

            var device = _principal.SingleOrDefault(t => t.ID == deviceID);
            if (device == null)
            {
                throw new InvalidOperationException("PopReverseListen");
            }
            bool isSet = false;
            try
            {
                //每8秒一检测连接
                isSet = device.WaitHandle.WaitOne(1000 * 8);
                return device.ListenState;
            }
            finally
            {
                if (isSet)
                {
                    Hub.LogInfo("PopReverseListen: {0} {1}", device.ListenState.AgentSock, device.ListenState.AgentDirect);
                    device.ListenState.AgentSock = Guid.Empty;
                }
            }
        }
        #endregion

        #region Udp
        public bool HasUdpClient(Guid sock, out UdpClient client)
        {
            string key = sock.ToString("N");
            client = (UdpClient)_udpClients.Get(key);
            return client != null;
        }

        public UdpClient GetUdpClient(Guid sock)
        {
            string key = sock.ToString("N");
            var client = (UdpClient)_udpClients.Get(key);
            if (client == null)
            {
                if (_udpClients.Add(key, client = xHttpHandler.CreateUdpClient(), new CacheItemPolicy()
                {
                    Priority = CacheItemPriority.NotRemovable,
                    SlidingExpiration = TimeSpan.FromMinutes(10D),
                    RemovedCallback = arguments =>
                    {
                        var dummy = (UdpClient)arguments.CacheItem.Value;
                        dummy.Client.Close();
                        _udpClientKeys.Remove(arguments.CacheItem.Key);
                    }
                }))
                {
                    _udpClientKeys.Add(key);
                }
            }
            return client;
        }
        #endregion
    }
}