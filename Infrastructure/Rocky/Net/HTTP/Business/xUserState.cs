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

namespace Rocky.Net
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
        private List<DeviceIdentity> _principal;
        private ConcurrentDictionary<Guid, TcpClient> _clients;
        private List<string> _udpClientKeys;
        #endregion

        #region Properties
        /// <summary>
        /// 可用流量bytes
        /// 1073741824
        /// </summary>
        public uint MaximumFlowRate { get; internal set; }
        /// <summary>
        /// 客户端设备
        /// </summary>
        public DeviceIdentity[] Principal
        {
            get
            {
                lock (_principal)
                {
                    return _principal.ToArray();
                }
            }
        }
        public IEnumerable<TcpClient> Clients
        {
            get { return _clients.Values; }
        }
        public IEnumerable<UdpClient> UdpClients
        {
            get
            {
                lock (_udpClientKeys)
                {
                    var dict = _udpClients.GetValues(_udpClientKeys);
                    return dict.Values.Cast<UdpClient>();
                }
            }
        }
        #endregion

        #region Constructors
        public xUserState()
        {
            _principal = new List<DeviceIdentity>();
            _clients = new ConcurrentDictionary<Guid, TcpClient>();

            _udpClientKeys = new List<string>();
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

            lock (_principal)
            {
                var q = from t in _principal
                        where t.WAN.Equals(WAN_addr) && t.LAN.Equals(LAN_addr)
                        select t;
                var device = q.SingleOrDefault();
                if (device != null)
                {
                    device.WaitHandle.Close();
					Thread.Sleep(1000 * 10);
                }
                if (q.Any())
                {
                    throw new InvalidOperationException("该设备已登录");
                }
                if (_principal.Count >= xHttpHandler.MaxDevice)
                {
                    _principal.RemoveAt(0);
                    this.Kill();
                }
                var identity = new DeviceIdentity()
                {
                    WAN = WAN_addr,
                    LAN = LAN_addr
                };
                _principal.Add(identity);
                return identity.ID;
            }
        }

        public void SignOut(Guid deviceID)
        {
            Contract.Requires(deviceID != Guid.Empty);
            base.CheckDisposed();

            lock (_principal)
            {
                int i = _principal.FindIndex(t => t.ID == deviceID);
                if (i == -1)
                {
                    return;
                }
                _principal[i].WaitHandle.Close();
                _principal.RemoveAt(i);
            }
        }

        public void Kill()
        {
            base.CheckDisposed();

            lock (this)
            {
                Guid[] clientKeys = _clients.Keys.ToArray();
                foreach (Guid sock in clientKeys)
                {
                    this.Disconnect(sock);
                }
                lock (_udpClientKeys)
                {
                    foreach (string clientKey in _udpClientKeys)
                    {
                        var client = (UdpClient)_udpClients.Remove(clientKey);
                        if (client != null)
                        {
                            client.Close();
                        }
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

            DeviceIdentity device;
            lock (_principal)
            {
                device = _principal.Find(t => t.ID == deviceID);
                if (device == null || device.ListenState.AgentSock != Guid.Empty)
                {
                    throw new InvalidOperationException("PushReverseListen");
                }
            }
            device.ListenState.AgentSock = agentSock;
            device.ListenState.AgentDirect = agentDirect;
            device.WaitHandle.Set();
            Runtime.LogInfo("PushReverseListen {0} {1}", agentSock, agentDirect);
        }

        internal ReverseListenState PopReverseListen(Guid deviceID)
        {
            base.CheckDisposed();

            DeviceIdentity device;
            lock (_principal)
            {
                device = _principal.Find(t => t.ID == deviceID);
                if (device == null)
                {
                    throw new InvalidOperationException("PopReverseListen");
                }
            }
            bool isSet = false;
            try
            {
                //每10秒一检测连接
                isSet = device.WaitHandle.WaitOne(1000 * 10);
                return device.ListenState;
            }
            finally
            {
                if (isSet)
                {
                    Runtime.LogInfo("PopReverseListen {0} {1}", device.ListenState.AgentSock, device.ListenState.AgentDirect);
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
                        dummy.Close();
                        lock (_udpClientKeys)
                        {
                            _udpClientKeys.Remove(arguments.CacheItem.Key);
                        }
                    }
                }))
                {
                    lock (_udpClientKeys)
                    {
                        _udpClientKeys.Add(key);
                    }
                }
            }
            return client;
        }
        #endregion
    }
}