using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Text;

namespace Rocky.Net
{
    internal sealed class xUserManager
    {
        #region NestedTypes
        private class TunnelDataState
        {
            public Guid LocalAgentSock { get; private set; }
            public TunnelDataQueue LocalQueue { get; private set; }
            public Guid RemoteAgentSock { get; private set; }
            public TunnelDataQueue RemoteQueue { get; private set; }

            public TunnelDataState(Guid localAgentSock, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
            {
                this.LocalAgentSock = localAgentSock;
                this.LocalQueue = new TunnelDataQueue(remoteEndPoint);
                this.RemoteQueue = new TunnelDataQueue(localEndPoint);
            }

            public void ShakeHands(Guid remoteAgentSock)
            {
                this.RemoteAgentSock = remoteAgentSock;
                this.RemoteQueue.Connected = this.LocalQueue.Connected = true;
                //30秒超时时间
                this.LocalQueue.WaitHandle.Set();
            }
        }
        #endregion

        #region Fields
        private string[] _credentials;
        /// <summary>
        /// Key: 凭证MD5
        /// </summary>
        private readonly ConcurrentDictionary<string, xUserState> _users;
        /// <summary>
        /// Key: LocalAgentSock
        /// </summary>
        private readonly ConcurrentDictionary<Guid, TunnelDataState> _dataStates;
        #endregion

        #region Constructors
        public xUserManager(string[] credentials)
        {
            Contract.Requires(credentials != null);

            _credentials = credentials;
            _users = new ConcurrentDictionary<string, xUserState>();
            _dataStates = new ConcurrentDictionary<Guid, TunnelDataState>();
        }
        #endregion

        #region Methods
        public Guid SignIn(string credential, IPAddress WAN_addr, IPAddress LAN_addr)
        {
            Contract.Requires(!string.IsNullOrEmpty(credential));

            if (!_credentials.Any(t => t == credential))
            {
                throw new InvalidOperationException("凭证验证失败");
            }
            //1GB
            var user = _users.GetOrAdd(credential, k => new xUserState() { MaximumFlowRate = 1073741824u });
            return user.SignIn(WAN_addr, LAN_addr);
        }

        public xUserState GetUser(string credential)
        {
            Contract.Requires(!string.IsNullOrEmpty(credential));

            xUserState user;
            if (!_users.TryGetValue(credential, out user))
            {
                throw new InvalidOperationException("用户验证失败");
            }
            return user;
        }
        public xUserState GetUser(Guid deviceID)
        {
            var q = from u in _users.Values
                    where u.Principal.Any(t => t.ID == deviceID)
                    select u;
            var user = q.SingleOrDefault();
            if (user == null)
            {
                throw new InvalidOperationException("设备验证失败");
            }
            return user;
        }

        public void SignOut(string credential, Guid deviceID)
        {
            Contract.Requires(!string.IsNullOrEmpty(credential));

            var user = this.GetUser(credential);
            user.SignOut(deviceID);
            if (user.Principal.Count == 0 && _users.TryRemove(credential, out user))
            {
                user.Dispose();
            }
        }
        #endregion

        #region ReverseMethods
        public void ReverseConnect(Guid localAgentSock, IPEndPoint localEndPoint, Guid deviceID, IPEndPoint remoteEndPoint)
        {
            var state = new TunnelDataState(localAgentSock, localEndPoint, remoteEndPoint);
            if (!_dataStates.TryAdd(localAgentSock, state))
            {
                throw new InvalidOperationException("ReverseConnect");
            }
            var remoteUser = this.GetUser(deviceID);
            remoteUser.PushReverseListen(deviceID, localAgentSock, remoteEndPoint);
        }

        public void ReverseShakeHands(Guid localAgentSock, Guid remoteAgentSock)
        {
            TunnelDataState state;
            if (!_dataStates.TryGetValue(localAgentSock, out state))
            {
                throw new InvalidOperationException("ReverseShakeHands");
            }
            if (!_dataStates.TryAdd(remoteAgentSock, state))
            {
                throw new InvalidOperationException("ReverseShakeHands");
            }
            state.ShakeHands(remoteAgentSock);
        }

        public TunnelDataQueue GetReverseQueue(Guid agentSock, bool isSend2, bool throwError = true)
        {
            TunnelDataState state;
            if (!_dataStates.TryGetValue(agentSock, out state))
            {
                if (!throwError)
                {
                    return null;
                }
                throw new InvalidOperationException("GetReverseDataQueue");
            }
            if (state.LocalAgentSock == agentSock)
            {
                return isSend2 ? state.RemoteQueue : state.LocalQueue;
            }
            else if (state.RemoteAgentSock == agentSock)
            {
                return isSend2 ? state.LocalQueue : state.RemoteQueue;
            }
            throw new InvalidOperationException("GetReverseDataQueue");
        }

        public void ReverseDisconnect(Guid agentSock)
        {
            TunnelDataState dummy;
            if (!_dataStates.TryRemove(agentSock, out dummy))
            {
                return;
            }
            dummy.LocalQueue.Dispose();
            dummy.RemoteQueue.Dispose();
            _dataStates.TryRemove(dummy.RemoteAgentSock, out dummy);
        }
        #endregion
    }
}