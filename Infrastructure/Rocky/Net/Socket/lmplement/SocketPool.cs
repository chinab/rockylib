using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Rocky.Net
{
    internal class SocketPool : Disposable, ISocketPool
    {
        #region NestedTypes
        private class PooledSocket : Socket
        {
            private ISocketPool _owner;

            public bool Free { get; set; }

            public PooledSocket(ISocketPool pool)
                : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                _owner = pool;
                this.Connect(_owner.ServerEndpoint);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !this.Free)
                {
                    _owner.Return(this);
                    return;
                }
                base.Dispose(disposing);
            }
        }
        #endregion

        #region Fields
        private ConcurrentStack<Socket> _pool;
        #endregion

        #region Properties
        public DistributedEndPoint ServerEndpoint { get; private set; }
        #endregion

        #region Constructors
        public SocketPool(DistributedEndPoint endpoint)
        {
            Contract.Requires(endpoint != null);

            this.ServerEndpoint = endpoint;
            _pool = new ConcurrentStack<Socket>();
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                foreach (PooledSocket sock in _pool)
                {
                    sock.Free = true;
                    sock.Dispose();
                }
                _pool.Clear();
            }
            _pool = null;
        }
        #endregion

        #region Methods
        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_pool.All(t => t is PooledSocket));
        }

        public Socket Take()
        {
            Socket sock;
            if (!_pool.TryPop(out sock))
            {
                sock = new PooledSocket(this);
            }
            return sock;
        }

        public void Return(Socket item)
        {
            _pool.Push(item);
        }

        public Socket[] TakeRange(int count)
        {
            var socks = new Socket[count];
            int i = _pool.TryPopRange(socks);
            if (i < count)
            {
                Array.Resize(ref socks, count);
                for (; i < count; i++)
                {
                    socks[i] = new PooledSocket(this);
                }
            }
            return socks;
        }

        public void Return(Socket[] items)
        {
            _pool.PushRange(items);
        }
        #endregion
    }
}