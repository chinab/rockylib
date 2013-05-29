using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Rocky.Net
{
    internal sealed class SendChunkModel : IChunkModel
    {
        #region Fields
        private long _offset, _count, _bytesSent, _sendLength;
        private volatile bool _isCompleted;
        private Socket _sock;
        private FileStream _reader;
        #endregion

        #region Properties
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
        public SendChunkModel(IPEndPoint ipe)
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sock.Connect(ipe);
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
            _bytesSent = bytesTransferred;
            _sendLength = transferLength;
            _reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _reader.Position = _offset + _bytesSent;
            //_reader.Lock(_offset, _count);
        }

        public void Run()
        {
            var remoteIpe = _sock.RemoteEndPoint;
            try
            {
                //var netStream = new NetworkStream(_sock, FileAccess.Write, false);
                //_reader.FixedCopyTo(netStream, _sendLength - _bytesSent, sent =>
                //{
                //    _bytesSent += (long)sent;
                //    return true;
                //});
                byte[] buffer = new byte[8192];
                int read, sent, sendLeft;
                while (_bytesSent < _sendLength && _sock.Connected)
                {
                    read = _reader.Read(buffer, 0, Math.Min(buffer.Length, (int)(_sendLength - _bytesSent)));
                    sent = 0;
                    sendLeft = read;
                    while ((sent += _sock.Send(buffer, sent, sendLeft, SocketFlags.None)) < read)
                    {
                        sendLeft -= sent;
                    }
                    _bytesSent += read;
                    Runtime.LogDebug("[SendChunk{0}] {1} {2}/{3}.", Thread.CurrentThread.ManagedThreadId,
                        remoteIpe, _bytesSent, _bytesSent);
                }
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
                //_reader.Unlock(_offset, _count);
                _reader.Dispose();
                _sock.Disconnect();
                _isCompleted = true;
            }
        }

        public void ReportProgress(out long bytesTransferred, out long transferLength)
        {
            bytesTransferred = _bytesSent;
            transferLength = _sendLength;
        }
        #endregion
    }
}