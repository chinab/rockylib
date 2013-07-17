using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace System.Net
{
    internal sealed class ReceiveChunkModel : IChunkModel
    {
        #region Fields
        private long _offset, _count, _bytesReceived, _receiveLength;
        private volatile bool _isCompleted;
        private Socket _sock;
        private FileStream _writer;
        #endregion

        #region Properties
        /// <summary>
        /// 默认4M
        /// </summary>
        public int DiskWriteCache { get; set; }
        public Socket Client
        {
            get { return _sock; }
        }
        public bool IsCompleted
        {
            get { return _isCompleted; }
        }
        #endregion

        #region Constructors
        public ReceiveChunkModel(Socket client)
        {
            _sock = client;
            this.DiskWriteCache = 1024 ^ 2 * 4;
        }
        #endregion

        #region Methods
        public void Initialize(string filePath, long position, long length, long bytesTransferred = 0L, long transferLength = -1L)
        {
            if (transferLength == -1L)
            {
                transferLength = length;
            }
            if (!(bytesTransferred >= 0L && bytesTransferred < transferLength && transferLength <= length))
            {
                throw new ArgumentException("bytesTransferred");
            }

            _offset = position;
            _count = length;
            _bytesReceived = bytesTransferred;
            _receiveLength = transferLength;
            _writer = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.Write, this.DiskWriteCache);
            _writer.Position = _offset + _bytesReceived;
            _writer.Lock(_offset, _count);
        }

        public void Run()
        {
            var remoteIpe = _sock.RemoteEndPoint;
            var netStream = new NetworkStream(_sock, FileAccess.Read, false);
            try
            {
                netStream.FixedCopyTo(_writer, _receiveLength - _bytesReceived, recv =>
                {
                    _writer.Flush();
                    _bytesReceived += recv;
                    Hub.LogDebug("[ReceiveChunk{0}] {1} {2}/{3}.", Thread.CurrentThread.ManagedThreadId,
                        remoteIpe, _bytesReceived, _receiveLength);
                    return _sock.Connected;
                });
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    return;
                }
                throw;
            }
            finally
            {
                _writer.Unlock(_offset, _count);
                _writer.Dispose();
                _sock.Close();
                _isCompleted = true;
            }
        }

        public void ReportProgress(out long bytesTransferred, out long transferLength)
        {
            bytesTransferred = _bytesReceived;
            transferLength = _receiveLength;
        }
        #endregion
    }
}