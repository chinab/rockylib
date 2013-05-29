using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Rocky.Net
{
    public sealed class TransferProgress
    {
        #region Fields
        private Stopwatch _watcher;
        private long _contentLength, _totalBytesTransferred, _bytesTransferred, _speed;
        private Action<TransferProgress> _progressChanged;
        #endregion

        #region Properties
        public TimeSpan Elapsed
        {
            get { return _watcher.Elapsed; }
        }
        public long ContentLength
        {
            get
            {
                Contract.Ensures(Contract.Result<long>() > 0L);

                return Interlocked.Read(ref _contentLength);
            }
        }
        public long TotalBytesTransferred
        {
            get { return Interlocked.Read(ref _totalBytesTransferred); }
        }
        public int ProgressPercentage
        {
            get { return (int)(this.TotalBytesTransferred * 100 / this.ContentLength); }
        }
        public long BytesTransferred
        {
            get { return Interlocked.Read(ref _bytesTransferred); }
        }
        #endregion

        #region Constructors
        public TransferProgress(Action<TransferProgress> progressChanged = null)
        {
            _progressChanged = progressChanged;
            _watcher = new Stopwatch();
        }
        #endregion

        #region Methods
        internal void Start(long contentLength)
        {
            Interlocked.Exchange(ref _contentLength, contentLength);
            _watcher.Restart();
        }

        internal void OnProgressChanged(long transferred, long totalTransferred)
        {
            Interlocked.Add(ref _speed, transferred);
            Interlocked.Exchange(ref _bytesTransferred, transferred);
            Interlocked.Exchange(ref _totalBytesTransferred, totalTransferred);
            if (_progressChanged != null)
            {
                _progressChanged(this);
            }
        }

        internal void Stop()
        {
            _watcher.Stop();
        }

        /// <summary>
        /// 传输速度，单位KB/S
        /// 1KB=1024B
        /// 1Byte=8bit
        /// </summary>
        /// <returns></returns>
        public long GetSpeed()
        {
            try
            {
                return Interlocked.Read(ref _speed) / 1024L;
            }
            finally
            {
                Interlocked.Exchange(ref _speed, 0L);
            }
        }
        #endregion
    }
}