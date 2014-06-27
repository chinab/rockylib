/*********************************************************************************
** File Name	:	FileTransmitor.cs
** Copyright (C) 2010 Snda Network Corporation. All Rights Reserved.
** Creator		:	RockyWong
** Create Date	:	2010-06-02 11:22:45
** Update Date	:	2013-01-11 11:35:26
** Description	:	多线程多管道可断点传输大文件
** Version No	:	
*********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed class FileTransfer : Disposable
    {
        #region Fields
        internal const int PerLongSize = sizeof(long);
        internal const string PointExtension = ".dat";
        internal const string TempExtension = ".temp";

        private string _savePath;
        private Socket _listener;
        #endregion

        #region Properties
        public event EventHandler<TransferEventArgs> Prepare;
        public event EventHandler<TransferEventArgs> ProgressChanged;
        public event EventHandler<TransferEventArgs> Completed;

        public string DirectoryPath
        {
            get { return _savePath; }
            set
            {
                _savePath = value + @"\" + DateTime.Now.ToString("yyyy-MM") + @"\";
            }
        }
        #endregion

        #region Constructors
        public FileTransfer()
        {

        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                if (_listener != null)
                {
                    SocketHelper.CloseListener(ref _listener);
                }
            }
            _listener = null;
            Prepare = null;
            ProgressChanged = null;
            Completed = null;
        }
        #endregion

        #region Methods
        private void OnPrepare(TransferEventArgs e)
        {
            if (this.Prepare != null)
            {
                this.Prepare(this, e);
            }
        }

        private void OnProgressChanged(TransferEventArgs e)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, e);
            }
        }

        private void OnCompleted(TransferEventArgs e)
        {
            if (this.Completed != null)
            {
                this.Completed(this, e);
            }
        }
        #endregion

        #region Receive
        /// <summary>
        /// Listen
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="port"></param>
        public void Listen(string savePath, ushort port)
        {
            Contract.Requires(!string.IsNullOrEmpty(savePath));
            if (_listener != null)
            {
                throw new ApplicationException("已启动监听");
            }

            App.CreateDirectory(_savePath = savePath);
            var localIpe = new IPEndPoint(IPAddress.Any, port);
            //最多支持16线程
            SocketHelper.CreateListener(out _listener, localIpe, 16);
            _listener.BeginAccept(this.AcceptCallback, null);
        }
        private void AcceptCallback(IAsyncResult ar)
        {
            if (_listener == null)
            {
                return;
            }
            _listener.BeginAccept(this.AcceptCallback, null);

            Socket controlClient = _listener.EndAccept(ar);
            App.LogInfo("TunnelTest 双工通讯: {0}.", controlClient.RemoteEndPoint);

            TransferConfig config;
            controlClient.Receive(out config);
            var e = new TransferEventArgs(config);
            this.OnPrepare(e);
            if (e.Cancel || !controlClient.Connected)
            {
                controlClient.Close();
                return;
            }
            byte[] buffer = new byte[1] { 1 };
            controlClient.Send(buffer);

            var chunkGroup = new ReceiveChunkModel[config.ChunkCount];
            chunkGroup[0] = new ReceiveChunkModel(controlClient);
            for (int i = 1; i < chunkGroup.Length; i++)
            {
                chunkGroup[i] = new ReceiveChunkModel(_listener.Accept());
            }
            TaskHelper.Factory.StartNew(Receive, new object[] { e, chunkGroup });
        }

        private void Receive(object state)
        {
            var args = (object[])state;
            var e = (TransferEventArgs)args[0];
            var chunkGroup = (ReceiveChunkModel[])args[1];
            var controlClient = chunkGroup[0].Client;
            var remoteIpe = controlClient.RemoteEndPoint;

            e.Progress = new TransferProgress();
            e.Progress.Start(e.Config.FileLength);
            #region Breakpoint
            int perPairCount = PerLongSize * 2, count = perPairCount * chunkGroup.Length;
            byte[] bufferInfo = new byte[count];
            string filePath = Path.Combine(_savePath, e.Config.Checksum + Path.GetExtension(e.Config.FileName)),
                pointFilePath = Path.ChangeExtension(filePath, PointExtension), tempFilePath = Path.ChangeExtension(filePath, TempExtension);
            FileStream pointStream;
            long oddSize, avgSize = Math.DivRem(e.Config.FileLength, (long)chunkGroup.Length, out oddSize);
            if (File.Exists(pointFilePath) && File.Exists(tempFilePath))
            {
                pointStream = new FileStream(pointFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                pointStream.Read(bufferInfo, 0, count);
                long fValue, tValue;
                for (int i = 0, j = chunkGroup.Length - 1; i < chunkGroup.Length; i++)
                {
                    fValue = BitConverter.ToInt64(bufferInfo, i * perPairCount);
                    tValue = BitConverter.ToInt64(bufferInfo, i * perPairCount + PerLongSize);
                    chunkGroup[i].Initialize(tempFilePath, i * avgSize, i == j ? avgSize + oddSize : avgSize, fValue, tValue);
                    App.LogDebug("[ReceiveBreakpoint] {0} Read{1}:{2}/{3}.", remoteIpe, i, fValue, tValue);
                }
                controlClient.Send(bufferInfo);
            }
            else
            {
                pointStream = new FileStream(pointFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                FileStream stream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Write);
                stream.SetLength(e.Config.FileLength);
                stream.Flush();
                stream.Dispose();
                for (int i = 0, j = chunkGroup.Length - 1; i < chunkGroup.Length; i++)
                {
                    chunkGroup[i].Initialize(tempFilePath, i * avgSize, i == j ? avgSize + oddSize : avgSize);
                }
                controlClient.Send(bufferInfo, 0, 4, SocketFlags.None);
            }
            var timer = new Timer(arg =>
            {
                long fValue, tValue;
                for (int i = 0; i < chunkGroup.Length; i++)
                {
                    chunkGroup[i].ReportProgress(out fValue, out tValue);
                    Buffer.BlockCopy(BitConverter.GetBytes(fValue), 0, bufferInfo, i * perPairCount, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(tValue), 0, bufferInfo, i * perPairCount + PerLongSize, 8);
                    App.LogDebug("[ReceiveBreakpoint] {0} Write{1}:{2}/{3}.", remoteIpe, i, fValue, tValue);
                }
                pointStream.Position = 0L;
                pointStream.Write(bufferInfo, 0, count);
                pointStream.Flush();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(4));
            #endregion
            TaskHelper.Factory.StartNew(() => Parallel.ForEach(chunkGroup, chunk => chunk.Run()));
            long bytesTransferred = 0L;
            do
            {
                chunkGroup.ReportSpeed(e.Progress, ref bytesTransferred);
                this.OnProgressChanged(e);
                Thread.Sleep(1000);
            }
            while (!chunkGroup.IsAllCompleted());
            chunkGroup.ReportSpeed(e.Progress, ref bytesTransferred);
            this.OnProgressChanged(e);
            timer.Dispose();
            pointStream.Dispose();
            File.Delete(pointFilePath);
            File.Move(tempFilePath, filePath);

            e.Progress.Stop();
            this.OnCompleted(e);
        }
        #endregion

        #region Send
        public void Send(TransferConfig config, IPEndPoint remoteIpe)
        {
            Contract.Requires(config != null && remoteIpe != null);

            var controlChunk = new SendChunkModel(remoteIpe);
            controlChunk.Client.Send(config);
            var e = new TransferEventArgs(config);
            this.OnPrepare(e);
            byte[] buffer = new byte[1];
            controlChunk.Client.Receive(buffer);
            if (e.Cancel || buffer[0] == 0 || !controlChunk.Client.Connected)
            {
                controlChunk.Client.Close();
                return;
            }

            var chunkGroup = new SendChunkModel[config.ChunkCount];
            chunkGroup[0] = controlChunk;
            for (int i = 1; i < chunkGroup.Length; i++)
            {
                chunkGroup[i] = new SendChunkModel(remoteIpe);
            }

            e.Progress = new TransferProgress();
            e.Progress.Start(config.FileLength);
            #region Breakpoint
            int perPairCount = PerLongSize * 2, count = perPairCount * chunkGroup.Length;
            byte[] bufferInfo = new byte[count];
            long oddSize, avgSize = Math.DivRem(config.FileLength, (long)chunkGroup.Length, out oddSize);
            if (controlChunk.Client.Receive(bufferInfo) == 4)
            {
                for (int i = 0, j = chunkGroup.Length - 1; i < chunkGroup.Length; i++)
                {
                    chunkGroup[i].Initialize(config.FilePath, i * avgSize, i == j ? avgSize + oddSize : avgSize);
                }
            }
            else
            {
                long fValue, tValue;
                for (int i = 0, j = chunkGroup.Length - 1; i < chunkGroup.Length; i++)
                {
                    fValue = BitConverter.ToInt64(bufferInfo, i * perPairCount);
                    tValue = BitConverter.ToInt64(bufferInfo, i * perPairCount + PerLongSize);
                    chunkGroup[i].Initialize(config.FilePath, i * avgSize, i == j ? avgSize + oddSize : avgSize, fValue, tValue);
                    App.LogDebug("[SendBreakpoint] {0} {1}:{2}/{3}.", remoteIpe, i, fValue, tValue);
                }
            }
            #endregion
            TaskHelper.Factory.StartNew(() => Parallel.ForEach(chunkGroup, chunk => chunk.Run()));
            long bytesTransferred = 0L;
            do
            {
                chunkGroup.ReportSpeed(e.Progress, ref bytesTransferred);
                this.OnProgressChanged(e);
                Thread.Sleep(1000);
            }
            while (!chunkGroup.IsAllCompleted());
            chunkGroup.ReportSpeed(e.Progress, ref bytesTransferred);
            this.OnProgressChanged(e);

            e.Progress.Stop();
            this.OnCompleted(e);
        }
        #endregion
    }
}