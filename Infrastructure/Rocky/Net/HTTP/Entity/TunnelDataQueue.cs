using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Rocky.Net
{
    public class TunnelDataQueue : Disposable
    {
        private bool _connected;
        private Stream _queue;

        public bool Connected
        {
            get { return _connected; }
            internal set
            {
                base.CheckDisposed();
                _connected = value;
            }
        }
        public IPEndPoint RemoteEndPoint { get; private set; }
        public AutoResetEvent WaitHandle { get; private set; }

        public TunnelDataQueue(IPEndPoint remoteEndPoint)
        {
            Contract.Requires(remoteEndPoint != null);

            this.RemoteEndPoint = remoteEndPoint;
            this.WaitHandle = new AutoResetEvent(false);
        }
        protected override void DisposeInternal(bool disposing)
        {
            this.Connected = false;
            if (disposing)
            {
                this.WaitHandle.Dispose();
            }
            this.WaitHandle = null;
        }

        public void Push(Stream queue)
        {
            if (_queue != null)
            {
                throw new InvalidOperationException("queue");
            }

            _queue = queue;
        }

        public bool TryPop(out Stream queue)
        {
            try
            {
                return (queue = _queue) != null;
            }
            finally
            {
                _queue = null;
            }
        }
    }
}