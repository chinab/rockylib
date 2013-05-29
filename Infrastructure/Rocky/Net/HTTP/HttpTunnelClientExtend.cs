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

namespace Rocky.Net
{
    /// <summary>
    /// socks5扩展
    /// </summary>
    public partial class HttpTunnelClient
    {
        #region NestedTypes
        private class UdpClientState : Disposable
        {
            #region Static
            /// <summary>
            /// Key: RemoteEndPoint
            /// </summary>
            private static ConcurrentDictionary<long, UdpClientState> _mapper = new ConcurrentDictionary<long, UdpClientState>();

            public static bool TryGet(IPEndPoint remoteIpe, out UdpClientState state)
            {
                long key = GenerateKey(remoteIpe);
                return _mapper.TryGetValue(key, out state);
            }

            private static long GenerateKey(IPEndPoint ipe)
            {
                return BitConverter.ToUInt32(ipe.Address.GetAddressBytes(), 0) + ipe.Port;
            }
            #endregion

            #region Fields
            private ConcurrentBag<long> _remoteEndPoints;
            private AutoResetEvent _waitHandle;
            #endregion

            #region Properties
            public UdpClient Client { get; set; }
            public IPEndPoint LocalEndPoint { get; set; }
            #endregion

            #region Constructors
            public UdpClientState()
            {
                _remoteEndPoints = new ConcurrentBag<long>();
                _waitHandle = new AutoResetEvent(false);
            }

            protected override void DisposeInternal(bool disposing)
            {
                if (disposing)
                {
                    this.Client.Close();
                    foreach (var key in _remoteEndPoints)
                    {
                        UdpClientState dummy;
                        _mapper.TryRemove(key, out dummy);
                    }
                }
                _waitHandle = null;
            }
            #endregion

            #region Methods
            public void AddRemoteEndPoint(IPEndPoint remoteIpe)
            {
                long key = GenerateKey(remoteIpe);
                if (_mapper.TryAdd(key, this))
                {
                    _remoteEndPoints.Add(key);
                }
            }
            public bool HasRemoteEndPoint(IPEndPoint remoteIpe)
            {
                long key = GenerateKey(remoteIpe);
                return _mapper.ContainsKey(key);
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
                _waitHandle.Dispose();
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
        /// <exception cref="Rocky.Net.TunnelStateMissingException"></exception>
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
                throw new TunnelStateMissingException("UDP ProxySock handle invalid")
                {
                    Client = controlClient.Client
                };
            }
            this.PushTcpClient(controlClient, new IPEndPoint(IPAddress.None, IPEndPoint.MinPort));
            //监听连接状态
            state.Client.BeginReceive(this.ReceiveCallbak, controlClient);
            controlClient.Client.BeginReceive(xHttpHandler.EmptyBuffer, 0, 0, SocketFlags.None, this.DisconnectCallbak, controlClient);
            //Udp无连接先发送后接收
            state.WaitOnce();
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
                    this.OutWrite("UDP {0} disconnect.", state.LocalEndPoint);
                }
            }
            catch (SocketException ex)
            {
                TunnelExceptionHandler.Handle(ex, controlClient.Client, "UDP disconnect");
            }
            finally
            {
                controlClient.Client.Close();
                state.Dispose();
                this.PopTcpClient(controlClient);
                if (!_udpClients.TryRemove(controlClient, out state))
                {
                    throw new TunnelStateMissingException("UDP ProxySock handle invalid")
                    {
                        Client = controlClient.Client
                    };
                }
            }
        }

        /// <summary>
        /// GetUdpClientState
        /// </summary>
        /// <param name="controlClient"></param>
        /// <returns></returns>
        /// <exception cref="Rocky.Net.TunnelStateMissingException"></exception>
        private UdpClientState GetUdpClientState(TcpClient controlClient)
        {
            UdpClientState state;
            if (!_udpClients.TryGetValue(controlClient, out state))
            {
                throw new TunnelStateMissingException("UDP ProxySock handle invalid")
                {
                    Client = controlClient.Client
                };
            }
            return state;
        }
        #endregion

        #region UdpDirect
        private void ReceiveCallbak(IAsyncResult ar)
        {
            var controlClient = (TcpClient)ar.AsyncState;
            if (!controlClient.Connected)
            {
                return;
            }
            var localIpe = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            try
            {
                var state = this.GetUdpClientState(controlClient);
                state.Client.BeginReceive(this.ReceiveCallbak, controlClient);

                byte[] data = state.Client.EndReceive(ar, ref localIpe);
                if (data.IsNullOrEmpty() || !(data[0] == 0 && data[1] == 0 && data[2] == 0) || !state.LocalEndPoint.Equals(localIpe))
                {
                    //1.非法数据包
                    //2.本地端点非法
                    this.OutWrite("UDP {0} Discard {1}bytes.", localIpe, data == null ? -1 : data.Length);
                    return;
                }
                this.UdpDirectSend(controlClient, data);
                //开始接收数据
                state.SetOnce();
            }
            catch (ObjectDisposedException ex)
            {
#if DEBUG
                Runtime.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
            }
            catch (SocketException ex)
            {
                TunnelExceptionHandler.Handle(ex, controlClient.Client, string.Format("UDPAcceptReceive={0}", localIpe));
            }
            catch (Exception ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("UDPDirectSend={0}", localIpe));
            }
        }

        private void UdpDirectSend(TcpClient controlClient, byte[] data)
        {
            if (!controlClient.Connected)
            {
                return;
            }
            var currentState = this.GetUdpClientState(controlClient);
            IPEndPoint destIpe;
            int offset = 4;
            if (data[3] == 3)
            {
                DnsEndPoint de;
                Socks4Request.PackOut(data, ref offset, out de);
                destIpe = new IPEndPoint(SocketHelper.GetHostAddresses(de.Host).First(), de.Port);
            }
            else
            {
                Socks4Request.PackOut(data, ref offset, out destIpe);
            }
            currentState.AddRemoteEndPoint(destIpe);
#if DEBUG
            this.OutWrite("UDP {0} Record {1}.", currentState.LocalEndPoint, destIpe);
#endif

            try
            {
                var tunnel = this.CreateTunnel(TunnelCommand.UdpSend, controlClient);
                tunnel.Headers[xHttpHandler.AgentDirect] = destIpe.ToString();
                var outStream = new MemoryStream(data, offset, data.Length - offset);
#if DEBUG
                tunnel.Form[xHttpHandler.AgentChecksum] = CryptoManaged.MD5Hash(outStream);
                outStream.Position = 0L;
#endif
                tunnel.Files.Add(new HttpFile(xHttpHandler.AgentDirect, xHttpHandler.AgentDirect, outStream));
                var response = tunnel.GetResponse();
                if (response.GetResponseText() != "1")
                {
                    this.OutWrite("UDP {0} Send {1}\t{2}bytes failure.", currentState.LocalEndPoint, destIpe, data.Length);
                    return;
                }
                this.OutWrite("UDP {0} Send {1}\t{2}bytes.", currentState.LocalEndPoint, destIpe, data.Length);
            }
            catch (WebException ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("UDPDirectSend={0}", destIpe), _output);
            }
        }

        private void UdpDirectReceive(object state)
        {
            var controlClient = (TcpClient)state;
            if (!controlClient.Connected)
            {
                return;
            }
            var currentState = this.GetUdpClientState(controlClient);
#if DEBUG
            this.OutWrite("UDP {0} StartReceive.", currentState.LocalEndPoint);
#endif
            IPEndPoint remoteIpe = null;
            try
            {
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
                            UdpClientState dummy;
                            if (!UdpClientState.TryGet(remoteIpe, out dummy) || !dummy.HasRemoteEndPoint(remoteIpe))
                            {
                                //1.远程端点非法
                                this.OutWrite("UDP {0} Discard {1}bytes.", remoteIpe, read - 6);
                                continue;
                            }

                            int length = 10 + read;
                            int sent = dummy.Client.Send(data, length, dummy.LocalEndPoint);
                            this.OutWrite("UDP {0} Receive {1}\t{2}bytes.", dummy.LocalEndPoint, remoteIpe, length);
                        }
                        catch (ObjectDisposedException ex)
                        {
#if DEBUG
                            Runtime.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
                            return;
                        }
                        catch (IOException ex)
                        {
                            TunnelExceptionHandler.Handle(ex, controlClient.Client, string.Format("UDPDirectReceive={0}", remoteIpe));
                        }
                        catch (SocketException ex)
                        {
                            TunnelExceptionHandler.Handle(ex, controlClient.Client, string.Format("UDPDirectReceive={0}", remoteIpe));
                        }
                    }
                });
                response.Close();
            }
            catch (WebException ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("UDPDirectReceive={0}", remoteIpe), _output);
            }
            catch (Exception ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("UDPDirectReceive={0}", remoteIpe));
            }
        }
        #endregion
    }
}