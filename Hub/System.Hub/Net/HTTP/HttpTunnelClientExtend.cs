using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace System.Net
{
    /// <summary>
    /// socks5扩展
    /// </summary>
    public partial class HttpTunnelClient
    {
        #region NestedTypes
        private class UdpClientState : Disposable
        {
            #region Fields
            private static readonly HashSet<EndPoint> _remoteEndPoints = new HashSet<EndPoint>();
            private AutoResetEvent _waitHandle;
            #endregion

            #region Properties
            public UdpClient Client { get; set; }
            public IPEndPoint LocalEndPoint { get; set; }
            #endregion

            #region Constructors
            public UdpClientState()
            {
                _waitHandle = new AutoResetEvent(false);
            }

            protected override void DisposeInternal(bool disposing)
            {
                if (disposing)
                {
                    this.Client.Client.Close();
                    if (_waitHandle != null)
                    {
                        _waitHandle.Close();
                    }
                }
                _waitHandle = null;
            }
            #endregion

            #region Methods
            public void AddRemoteEndPoint(EndPoint remoteEndPoint)
            {
                var de = remoteEndPoint as DnsEndPoint;
                if (de != null)
                {
                    var q = from ip in SocketHelper.GetHostAddresses(de.Host)
                            select new IPEndPoint(ip, de.Port);
                    lock (_remoteEndPoints)
                    {
                        _remoteEndPoints.UnionWith(q);
                    }
                }
                else
                {
                    lock (_remoteEndPoints)
                    {
                        _remoteEndPoints.Add(remoteEndPoint);
                    }
                }
                Hub.LogInfo("Udp Record {0}.", remoteEndPoint);
            }
            public bool HasRemoteEndPoint(IPEndPoint remoteEndPoint)
            {
                bool result;
                lock (_remoteEndPoints)
                {
                    result = _remoteEndPoints.Contains(remoteEndPoint);
                }
                if (!result)
                {
                    var sb = new StringBuilder();
                    lock (_remoteEndPoints)
                    {
                        foreach (var ipe in _remoteEndPoints)
                        {
                            sb.Append(ipe).Append("\t");
                        }
                    }
                    sb.AppendLine().AppendFormat("\tForbidden: {0}.", remoteEndPoint);
                    Hub.LogInfo("Udp Check: {0}", sb);
                }
                return result;
            }

            public void SetOnce()
            {
                if (_waitHandle == null)
                {
                    return;
                }
                _waitHandle.Set();
            }
            public void WaitOnce()
            {
                _waitHandle.WaitOne();
                _waitHandle.Close();
                _waitHandle = null;
            }
            #endregion
        }
        #endregion

        #region Fields
        private ConcurrentDictionary<TcpClient, UdpClientState> _udpClients;
        #endregion

        #region Methods
        partial void OnCreate()
        {
            _udpClients = new ConcurrentDictionary<TcpClient, UdpClientState>();
        }

        /// <summary>
        /// Push socks5 Udp代理
        /// </summary>
        /// <param name="controlClient"></param>
        /// <param name="clientPort"></param>
        /// <param name="localIpe"></param>
        /// <exception cref="System.Net.TunnelStateMissingException"></exception>
        private void PushUdpClient(TcpClient controlClient, int clientPort, out IPEndPoint serverIpe)
        {
            Contract.Requires(_runType != null && controlClient.Connected);

            var localIpe = new IPEndPoint(((IPEndPoint)controlClient.Client.LocalEndPoint).Address, clientPort);
            var proxyClient = xHttpHandler.CreateUdpClient();
            serverIpe = new IPEndPoint(((IPEndPoint)controlClient.Client.RemoteEndPoint).Address, ((IPEndPoint)proxyClient.Client.LocalEndPoint).Port);
            var state = new UdpClientState()
            {
                LocalEndPoint = localIpe,
                Client = proxyClient
            };
            if (!_udpClients.TryAdd(controlClient, state))
            {
                throw new TunnelStateMissingException("Udp ProxySock handle invalid")
                {
                    Client = controlClient.Client
                };
            }
            this.PushTcpClient(controlClient, new IPEndPoint(IPAddress.None, IPEndPoint.MinPort));
            //监听连接状态
            controlClient.Client.BeginReceive(xHttpHandler.EmptyBuffer, 0, 0, SocketFlags.None, this.DisconnectCallbak, controlClient);
            TaskHelper.Factory.StartNew(this.UdpDirectSend, controlClient);
            TaskHelper.Factory.StartNew(this.UdpDirectReceive, controlClient);
        }
        private void DisconnectCallbak(IAsyncResult ar)
        {
            var controlClient = (TcpClient)ar.AsyncState;
            var state = this.GetUdpClientState(controlClient);
            try
            {
                int recv = controlClient.Client.EndReceive(ar);
                if (recv == 0 || !controlClient.Connected)
                {
                    this.OutWrite("Udp {0} disconnect.", state.LocalEndPoint);
                }
            }
            catch (ObjectDisposedException ex)
            {
#if DEBUG
                Hub.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
            }
            catch (SocketException ex)
            {
                TunnelExceptionHandler.Handle(ex, controlClient.Client, "Udp disconnect");
            }
            finally
            {
                controlClient.Client.Close();
                this.PopTcpClient(controlClient);
                state.Dispose();
                _udpClients.TryRemove(controlClient, out state);
            }
        }

        /// <summary>
        /// GetUdpClientState
        /// </summary>
        /// <param name="controlClient"></param>
        /// <returns></returns>
        /// <exception cref="System.Net.TunnelStateMissingException"></exception>
        private UdpClientState GetUdpClientState(TcpClient controlClient)
        {
            UdpClientState state;
            if (!_udpClients.TryGetValue(controlClient, out state))
            {
                throw new TunnelStateMissingException("Udp ProxySock handle invalid")
                {
                    Client = controlClient.Client
                };
            }
            return state;
        }
        #endregion

        #region UdpDirect
        private void UdpDirectSend(object state)
        {
            var controlClient = (TcpClient)state;
            if (!controlClient.Connected)
            {
                return;
            }
            string destIpe = null;
            try
            {
                var currentState = this.GetUdpClientState(controlClient);
                while (controlClient.Connected)
                {
                    try
                    {
                        var localIpe = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                        byte[] data = currentState.Client.Receive(ref localIpe);
                        if (!controlClient.Connected)
                        {
                            break;
                        }
                        if (data.IsNullOrEmpty() || !(data[0] == 0 && data[1] == 0 && data[2] == 0))
                        {
                            this.OutWrite("Udp Send Discard {0} {1}bytes.", localIpe, data == null ? -1 : data.Length);
                            Hub.LogInfo("Udp Send Discard 非法数据包.");
                            continue;
                        }
                        else if (!currentState.LocalEndPoint.Equals(localIpe))
                        {
                            this.OutWrite("Udp Send Discard {0} {1}bytes.", localIpe, data == null ? -1 : data.Length);
                            Hub.LogInfo("Udp Send Discard 非法本地端点.");
                            continue;
                        }

                        int offset = 4;
                        if (data[3] == 3)
                        {
                            DnsEndPoint de;
                            Socks4Request.PackOut(data, ref offset, out de);
                            currentState.AddRemoteEndPoint(de);
                            destIpe = string.Format("{0}:{1}", de.Host, de.Port);
                        }
                        else
                        {
                            IPEndPoint ipe;
                            Socks4Request.PackOut(data, ref offset, out ipe);
                            currentState.AddRemoteEndPoint(ipe);
                            destIpe = ipe.ToString();
                        }
                        var tunnel = this.CreateTunnel(TunnelCommand.UdpSend, controlClient);
                        tunnel.Headers[xHttpHandler.AgentDirect] = destIpe;
                        var outStream = new MemoryStream(data, offset, data.Length - offset);
#if DEBUG
                        tunnel.Form[xHttpHandler.AgentChecksum] = CryptoManaged.MD5Hash(outStream).ToString();
                        outStream.Position = 0L;
#endif
                        tunnel.Files.Add(new HttpFile(xHttpHandler.AgentDirect, xHttpHandler.AgentDirect, outStream));
                        var response = tunnel.GetResponse();
                        if (response.GetResponseText() != "1")
                        {
                            this.OutWrite("Udp {0} Send {1}\t{2}bytes failure.", currentState.LocalEndPoint, destIpe, outStream.Length);
                            return;
                        }
                        this.OutWrite("Udp {0} Send {1}\t{2}bytes.", currentState.LocalEndPoint, destIpe, outStream.Length);

                        //开始接收数据
                        currentState.SetOnce();
                    }
                    catch (ObjectDisposedException ex)
                    {
#if DEBUG
                        Hub.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
                    }
                    catch (WebException ex)
                    {
                        bool isRejected;
                        if (TunnelExceptionHandler.Handle(ex, string.Format("UdpDirectSend={0}", destIpe), _output, out isRejected))
                        {
                            controlClient.Client.Close();
                        }
                        if (isRejected)
                        {
                            this.OnServerRejected();
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.Interrupted)
                        {
#if DEBUG
                            Hub.LogInfo(string.Format("Predictable interrupted exception: {0}", ex.Message));
#endif
                            return;
                        }
                        TunnelExceptionHandler.Handle(ex, controlClient.Client, string.Format("UdpDirectSend={0}", destIpe));
                    }
                }
            }
            catch (Exception ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("UdpDirectSend={0}", destIpe));
            }
        }

        private void UdpDirectReceive(object state)
        {
            var controlClient = (TcpClient)state;
            if (!controlClient.Connected)
            {
                return;
            }
            IPEndPoint remoteIpe = null;
            try
            {
                var currentState = this.GetUdpClientState(controlClient);
                //Udp无连接先发送后接收
                currentState.WaitOnce();
                var tunnel = this.CreateTunnel(TunnelCommand.UdpReceive, controlClient);
                tunnel.SendReceiveTimeout = Timeout.Infinite;
                var response = tunnel.GetResponse(webResponse =>
                {
                    var pushStream = webResponse.GetResponseStream();
                    byte[] data = new byte[(int)BufferSizeof.MaxSocket];
                    //00 00 00 01 
                    data[3] = 1;
                    while (controlClient.Connected && pushStream.CanRead)
                    {
                        try
                        {
                            int offset = 4;
                            int read = pushStream.Read(data, offset, data.Length - offset);
                            if (read == 0 || !(controlClient.Connected && pushStream.CanRead))
                            {
                                break;
                            }
                            //4-10 中间6字节为remoteIpe
                            Socks4Request.PackOut(data, ref offset, out remoteIpe);
                            if (!currentState.HasRemoteEndPoint(remoteIpe))
                            {
                                this.OutWrite("Udp Receive Discard {0} {1}bytes.", remoteIpe, read);
                                Hub.LogInfo("Udp Receive Discard 非法远程端点.");
                                continue;
                            }

                            int length = 4 + read;
                            int sent = currentState.Client.Send(data, length, currentState.LocalEndPoint);
                            if (length != sent)
                            {
                                throw new InvalidOperationException("Udp数据包错误");
                            }
                            this.OutWrite("Udp {0} Receive {1}\t{2}bytes.", currentState.LocalEndPoint, remoteIpe, read);
                        }
                        catch (ObjectDisposedException ex)
                        {
#if DEBUG
                            Hub.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
                            return;
                        }
                        catch (IOException ex)
                        {
                            TunnelExceptionHandler.Handle(ex, controlClient.Client, string.Format("UdpDirectReceive={0}", remoteIpe));
                        }
                        catch (SocketException ex)
                        {
                            TunnelExceptionHandler.Handle(ex, controlClient.Client, string.Format("UdpDirectReceive={0}", remoteIpe));
                        }
                    }
                });
                response.Close();
            }
            catch (WebException ex)
            {
                bool isRejected;
                TunnelExceptionHandler.Handle(ex, string.Format("UdpDirectReceive={0}", remoteIpe), _output, out isRejected);
                if (isRejected)
                {
                    this.OnServerRejected();
                }
            }
            catch (Exception ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("UdpDirectReceive={0}", remoteIpe));
            }
        }
        #endregion
    }
}