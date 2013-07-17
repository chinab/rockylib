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
    /// http://blog.csdn.net/laotse/article/details/6296573
    /// http://www.lob.cn/jq/csjq/3874.shtml
    /// </summary>
    public partial class HttpTunnelClient : Disposable
    {
        #region NestedTypes
        private class ClientState
        {
            public Guid UniqueID { get; private set; }
            private EndPoint RemoteEndPoint { get; set; }

            public ClientState(EndPoint remoteEndPoint)
            {
                this.UniqueID = Guid.NewGuid();
                this.RemoteEndPoint = remoteEndPoint;
            }

            public override string ToString()
            {
                var de = this.RemoteEndPoint as DnsEndPoint;
                return de != null ? string.Format("{0}:{1}", de.Host, de.Port) : this.RemoteEndPoint.ToString();
            }
        }
        #endregion

        #region Fields
        private static HttpClient _keepAlive;
        private static Guid _clientID;
        private static string _agentHost;
        private TcpListener _listener;
        private ConcurrentDictionary<TcpClient, ClientState> _clients;
        private EndPoint _remoteEndPoint;
        private SocksProxyType? _runType;
        private TextWriter _output;
        #endregion

        #region Properties
        public Guid ClientID
        {
            get
            {
                if (_clientID == Guid.Empty)
                {
                    throw new ArgumentException("ClientID");
                }

                return _clientID;
            }
        }
        public string AgentHost
        {
            get
            {
                if (_agentHost == null)
                {
                    throw new ArgumentNullException("AgentHost");
                }

                return _agentHost;
            }
        }
        /// <summary>
        /// 服务端负载均衡
        /// </summary>
        public Uri[] ServerBalance { get; private set; }
        public NetworkCredential Credential { get; private set; }
        /// <summary>
        /// 反向连接客户端ID
        /// </summary>
        public Guid? ReverseRemoteID { get; set; }
        public IPAddress LAN_IP
        {
            get { return SocketHelper.GetHostAddresses().First(); }
        }
        public TextWriter Output
        {
            get { return _output; }
            set { Interlocked.Exchange(ref _output, value ?? Console.Out); }
        }
        #endregion

        #region Constructors
        public HttpTunnelClient(ushort listenPort, Uri[] serverBalance, NetworkCredential credential, IPEndPoint remoteEndPoint)
            : this(listenPort, serverBalance, credential, remoteEndPoint, null)
        {

        }
        public HttpTunnelClient(ushort listenPort, Uri[] serverBalance, NetworkCredential credential, SocksProxyType runType)
            : this(listenPort, serverBalance, credential, null, runType)
        {

        }
        private HttpTunnelClient(ushort listenPort, Uri[] serverBalance, NetworkCredential credential, IPEndPoint remoteEndPoint = null, SocksProxyType? runType = null)
        {
            Contract.Requires(serverBalance != null);
            Contract.Requires(credential != null);
            Contract.Requires(!(remoteEndPoint == null && runType == null));

            this.ServerBalance = serverBalance;
            this.Credential = credential;
            _remoteEndPoint = remoteEndPoint;
            _runType = runType;
            _output = Console.Out;
            _clients = new ConcurrentDictionary<TcpClient, ClientState>();

            this.OnCreate();
            _listener = new TcpListener(IPAddress.Any, listenPort);
            _listener.Start();
            _listener.BeginAcceptTcpClient(this.AcceptCallback, null);

            if (Interlocked.CompareExchange(ref _keepAlive, this.CreateTunnel(TunnelCommand.KeepAlive, null), null) == null)
            {
                var waitHandle = new AutoResetEvent(false);
                TaskHelper.Factory.StartNew(this.KeepAlive, waitHandle);
                waitHandle.WaitOne();
            }
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _listener.Stop();
                _clients.Clear();
            }
            _listener = null;
            _clients = null;
        }
        #endregion

        #region KeepAlive
        /// <summary>
        /// 控制连接 & 监听反向连接
        /// </summary>
        private void KeepAlive(object state)
        {
            var waitHandle = (AutoResetEvent)state;
            _keepAlive.SendReceiveTimeout = Timeout.Infinite;
            var crypto = xHttpHandler.CreateCrypto(_keepAlive.Headers[HttpRequestHeader.Authorization]);
            string checksum = this.LAN_IP.ToString();
            var inStream = crypto.Encrypt(new MemoryStream(Encoding.UTF8.GetBytes(checksum)));
            _keepAlive.Files.Add(new HttpFile(xHttpHandler.AgentChecksum, xHttpHandler.AgentChecksum, inStream));
            try
            {
                var response = _keepAlive.GetResponse(webResponse =>
                {
                    var pushStream = webResponse.GetResponseStream();
                    var bReader = new BinaryReader(pushStream, Encoding.UTF8);
                    _clientID = new Guid(bReader.ReadBytes(16));
                    _agentHost = bReader.ReadString();
                    //写入hosts文件
                    string hostsPath = Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");
                    try
                    {
                        bool exist = false;
                        using (var reader = new StreamReader(hostsPath, true))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.StartsWith("#"))
                                {
                                    continue;
                                }
                                if (line.LastIndexOf(_agentHost) != -1)
                                {
                                    exist = true;
                                    break;
                                }
                            }
                        }
                        if (!exist)
                        {
                            using (var writer = new StreamWriter(hostsPath, true))
                            {
                                writer.WriteLine();
                                writer.WriteLine("127.0.0.1       {0}", _agentHost);
                            }
                        }
                    }
                    catch (SystemException ex)
                    {
                        _agentHost = IPAddress.Loopback.ToString();
                        this.OutWrite("写入hosts失败，若需P2P功能则请以管理员身份运行。", ex);
                    }

                    waitHandle.Set();
                    var stream = new MemoryStream();
                    byte[] buffer = new byte[1024];
                    while (!base.IsDisposed && pushStream.CanRead)
                    {
                        int length = pushStream.Read(buffer, 0, buffer.Length);
                        if (length == 0 || !pushStream.CanRead)
                        {
                            break;
                        }

                        stream.Position = 0L;
                        stream.SetLength(length);
                        stream.Write(buffer, 0, length);
                        stream.Position = 0L;
                        var rConnState = (xUserState.ReverseListenState)Serializer.Deserialize(stream);
                        this.ReverseListen(rConnState);
                    }
                });
                response.Close();
            }
            catch (Exception ex)
            {
                this.OutWrite("凭证验证失败: {0}。", ex.Message);
            }
        }
        private void ReverseListen(xUserState.ReverseListenState rConnState)
        {
            TaskHelper.Factory.StartNew(() =>
            {
                var proxyClient = new TcpClient();
                try
                {
                    proxyClient.Connect(rConnState.AgentDirect);
                    this.PushTcpClient(proxyClient, rConnState.AgentDirect);
                    TaskHelper.Factory.StartNew(this.DirectReceive, new object[] { proxyClient, rConnState.AgentSock });
                    TaskHelper.Factory.StartNew(this.DirectSend, new object[] { proxyClient, rConnState.AgentSock });
                    this.OutWrite("ReverseConnectTo={0}", rConnState.AgentDirect);
                }
                catch (SocketException ex)
                {
                    Hub.LogError(ex, "ReverseListen");
                }
            });
        }

        public xUserState.DeviceIdentity[] GetDeviceIdentity()
        {
            var tunnel = this.CreateTunnel(TunnelCommand.DeviceIdentity, null);
            var res = tunnel.GetResponse();
            var stream = res.GetResponseStream();
            return (xUserState.DeviceIdentity[])Serializer.Deserialize(stream);
        }
        #endregion

        #region Methods
        private void OutWrite(string msg, Exception ex = null)
        {
            _output.WriteLine("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
            if (ex != null)
            {
                Hub.LogError(ex, "TunnelClient");
            }
        }
        private void OutWrite(string format, params object[] args)
        {
            string prefix = DateTime.Now.ToString("HH:mm:ss ");
            _output.WriteLine(prefix + format, args);
        }

        partial void OnCreate();

        /// <summary>
        /// 创建HttpTunnel
        /// </summary>
        /// <param name="proxyClient"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        /// <exception cref="System.Net.TunnelStateMissingException"></exception>
        private HttpClient CreateTunnel(TunnelCommand cmd, TcpClient proxyClient)
        {
            var tunnel = new HttpClient((Uri)xHttpServer.GetRandom(this.ServerBalance));
            tunnel.SendReceiveTimeout = xHttpHandler.Timeout * 1000;
            var cred = this.Credential;
            tunnel.Headers[HttpRequestHeader.Authorization] = CryptoManaged.MD5Hash(string.Format("{0}:{1}", cred.UserName, cred.Password));
            if (proxyClient != null)
            {
                var state = this.GetClientState(proxyClient);
                tunnel.Headers[xHttpHandler.AgentSock] = state.UniqueID.ToString("N");
                tunnel.Headers[xHttpHandler.AgentDirect] = state.ToString();
            }
            var rRemoteID = this.ReverseRemoteID;
            if (rRemoteID != null)
            {
                tunnel.Headers[xHttpHandler.AgentReverse] = string.Format("{0}#{1}", _clientID.ToString("N"), rRemoteID.Value.ToString("N"));
            }
            tunnel.Form[xHttpHandler.AgentCommand] = ((int)cmd).ToString();
            return tunnel;
        }

        /// <summary>
        /// 获取客户端状态实体
        /// </summary>
        /// <param name="proxyClient"></param>
        /// <returns></returns>
        /// <exception cref="System.Net.TunnelStateMissingException"></exception>
        private ClientState GetClientState(TcpClient proxyClient)
        {
            base.CheckDisposed();

            ClientState state;
            if (!_clients.TryGetValue(proxyClient, out state))
            {
                throw new TunnelStateMissingException("ProxySock handle invalid")
                {
                    Client = proxyClient.Client
                };
            }
            return state;
        }
        #endregion

        #region ControlMethods
        private void AcceptCallback(IAsyncResult ar)
        {
            Hub.LogInfo("app accept...");
            if (_listener == null)
            {
                return;
            }
            _listener.BeginAcceptTcpClient(this.AcceptCallback, null);

            var proxyClient = _listener.EndAcceptTcpClient(ar);
            if (_runType.HasValue)
            {
                var proxySock = proxyClient.Client;
                byte[] buffer = new byte[256];
                try
                {
                    switch (_runType.Value)
                    {
                        case SocksProxyType.Socks4:
                        case SocksProxyType.Socks4a:
                            {
                                int recv = proxySock.Receive(buffer);
                                var request = new Socks4Request();
                                request.ParsePack(buffer);
                                if (request.Command == Socks4Command.Bind)
                                {
                                    Hub.LogInfo("{0} {1}协议错误: Bind命令暂不支持", proxySock.LocalEndPoint, _runType);
                                    goto default;
                                }
                                var resIpe = new IPEndPoint(request.RemoteIP, request.RemotePort);
                                var response = new Socks4Response(resIpe);
                                response.Status = Socks4Response.ResponseStatus.RequestGranted;
                                proxySock.Send(response.ToPackets());
                                var destIpe = request.ProxyType == SocksProxyType.Socks4a ? (EndPoint)new DnsEndPoint(request.RemoteHost, request.RemotePort) : resIpe;
                                this.PushTcpClient(proxyClient, destIpe);
                            }
                            break;
                        case SocksProxyType.Socks5:
                            {
                                var request = new Socks5Request();
                                var response = new Socks5Response();
                                proxySock.Receive(buffer);
                                request.ParsePack(buffer);
                                if (request.Credential == null)
                                {
                                    //请求匿名
                                    response.Anonymous = true;
                                    proxySock.Send(response.ToPackets());
                                }
                                else
                                {
                                    response.Anonymous = false;
                                    proxySock.Send(response.ToPackets());
                                    proxySock.Receive(buffer);
                                    request.ParsePack(buffer);
                                    var cred = request.Credential;
                                    //验证凭证
                                    //if (!true)
                                    //{
                                    //    goto default;
                                    //}
                                    response.Status = Socks5Response.ResponseStatus.Success;
                                    proxySock.Send(response.ToPackets());
                                }
                                proxySock.Receive(buffer);
                                request.ParsePack(buffer);
                                bool isDns = request.EndPoint is DnsEndPoint;
                                var resIpe = isDns ? new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort) : (IPEndPoint)request.EndPoint;
                                switch (request.Command)
                                {
                                    case Socks5Command.TcpBind:
                                        Hub.LogInfo("{0} {1}协议错误: Bind命令暂不支持", proxySock.LocalEndPoint, _runType);
                                        goto default;
                                    case Socks5Command.UdpAssociate:
                                        //客户端预备开放的Udp端口
                                        int clientPort = isDns ? ((DnsEndPoint)request.EndPoint).Port : ((IPEndPoint)request.EndPoint).Port;
                                        //服务端开放的Udp端点
                                        IPEndPoint localIpe;
                                        this.PushUdpClient(proxyClient, clientPort, out localIpe);
                                        response.RemoteEndPoint = localIpe;
                                        proxySock.Send(response.ToPackets());
                                        return;
                                    default:
                                        response.RemoteEndPoint = resIpe;
                                        proxySock.Send(response.ToPackets());
                                        this.PushTcpClient(proxyClient, request.EndPoint);
                                        break;
                                }
                            }
                            break;
                        case SocksProxyType.Http:
                            {
                                //GoAgent集成不做任何协议处理
                                this.PushTcpClient(proxyClient, xHttpHandler.GoAgent);
                            }
                            break;
                        default:
                            proxyClient.Close();
                            return;
                    }
                }
                catch (Exception ex)
                {
                    this.OutWrite("{0} {1}协议错误: {2}", proxySock.LocalEndPoint, _runType, ex.Message);
                    Hub.LogError(ex, "代理协议错误");
                    proxyClient.Close();
                    return;
                }
            }
            else
            {
                this.PushTcpClient(proxyClient, _remoteEndPoint);
            }
            TaskHelper.Factory.StartNew(this.DirectReceive, new object[] { proxyClient });
            TaskHelper.Factory.StartNew(this.DirectSend, new object[] { proxyClient });
        }

        /// <summary>
        /// Push socks Tcp代理
        /// </summary>
        /// <param name="proxyClient"></param>
        /// <param name="destIpe"></param>
        /// <exception cref="System.Net.TunnelStateMissingException"></exception>
        private void PushTcpClient(TcpClient proxyClient, EndPoint destIpe)
        {
            Contract.Requires(proxyClient.Connected && destIpe != null);

            if (!_clients.TryAdd(proxyClient, new ClientState(destIpe)))
            {
                throw new TunnelStateMissingException("ProxySock handle invalid")
                {
                    Client = proxyClient.Client
                };
            }
        }

        /// <summary>
        /// Pop socks Tcp代理
        /// </summary>
        /// <param name="proxyClient"></param>
        /// <exception cref="System.Net.TunnelStateMissingException"></exception>
        private void PopTcpClient(TcpClient proxyClient)
        {
            ClientState dummy;
            if (!_clients.TryRemove(proxyClient, out dummy))
            {
                throw new TunnelStateMissingException("ProxySock handle invalid")
                {
                    Client = proxyClient.Client
                };
            }
        }
        #endregion

        #region TcpDirect
        private void DirectReceive(object state)
        {
            var args = (object[])state;
            var proxyClient = (TcpClient)args[0];
            Guid? localAgentSock = null;
            if (args.Length == 2)
            {
                localAgentSock = (Guid)args[1];
            }
            //客户端立即关闭则跳过
            if (!proxyClient.Connected)
            {
                return;
            }
            string destIpe = null;
            ArraySegment<byte> bArray;
            BufferSegment.Instance.Take(out bArray);
            try
            {
                var localIpe = (IPEndPoint)proxyClient.Client.LocalEndPoint;
                destIpe = this.GetClientState(proxyClient).ToString();
                var proxyStream = proxyClient.GetStream();
                var tunnel = this.CreateTunnel(TunnelCommand.Receive, proxyClient);
                tunnel.SendReceiveTimeout = Timeout.Infinite;
                if (localAgentSock != null)
                {
                    tunnel.Headers[xHttpHandler.AgentReverse] = localAgentSock.Value.ToString("N");
                }
                var response = tunnel.GetResponse(webResponse =>
                {
                    var pushStream = webResponse.GetResponseStream();
                    while (proxyClient.Connected && pushStream.CanRead)
                    {
                        try
                        {
                            //第一个数据包有延迟，故放弃此方式
                            //pushStream.Read(xHttpHandler.EmptyBuffer, 0, 0);
                            //int length = pushSock.Available;
                            //if (length == 0)
                            //{
                            //    break;
                            //}
                            //pushStream.FixedCopyTo(proxyStream, length);

                            int read = pushStream.Read(bArray);
                            //阻塞解除后做第2道检查
                            if (read == 0 || !(proxyClient.Connected && pushStream.CanRead))
                            {
                                //服务端主动关闭
                                this.OutWrite("{0} Initiative disconnect {1}.", destIpe, localIpe);
                                break;
                            }
                            proxyStream.Write(bArray, 0, read);
                            this.OutWrite("{0} Receive {1}\t{2}bytes.", localIpe, destIpe, read);
                        }
                        catch (IOException ex)
                        {
                            TunnelExceptionHandler.Handle(ex, proxyClient.Client, string.Format("DirectReceive={0}", destIpe));
                        }
                        catch (SocketException ex)
                        {
                            TunnelExceptionHandler.Handle(ex, proxyClient.Client, string.Format("DirectReceive={0}", destIpe));
                        }
                    }
                });
                response.Close();
            }
            catch (WebException ex)
            {
                if (TunnelExceptionHandler.Handle(ex, string.Format("DirectReceive={0}", destIpe), _output))
                {
                    proxyClient.Client.Close(1);
                }
            }
            catch (Exception ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("DirectReceive={0}", destIpe));
            }
            finally
            {
                BufferSegment.Instance.Return(ref bArray);
                proxyClient.Client.Close(1);
                this.PopTcpClient(proxyClient);
            }
        }

        private void DirectSend(object state)
        {
            var args = (object[])state;
            var proxyClient = (TcpClient)args[0];
            Guid? localAgentSock = null;
            if (args.Length == 2)
            {
                localAgentSock = (Guid)args[1];
            }
            if (!proxyClient.Connected)
            {
                return;
            }
            string destIpe = null;
            try
            {
                var localIpe = (IPEndPoint)proxyClient.Client.LocalEndPoint;
                destIpe = this.GetClientState(proxyClient).ToString();
                var proxyStream = proxyClient.GetStream();
                var outStream = new MemoryStream();
                while (proxyClient.Connected)
                {
                    try
                    {
                        proxyStream.Read(xHttpHandler.EmptyBuffer, 0, 0);
                        int length;
                        //阻塞解除后做第2道检查，先判断连接状态否则proxyClient.Available会引发ObjectDisposedException
                        if (!proxyClient.Connected || (length = proxyClient.Available) == 0)
                        {
                            //客户端主动关闭
                            this.OutWrite("{0} Passive disconnect {1}.", localIpe, destIpe);
                            proxyClient.Close();
                            break;
                        }
                        outStream.Position = 0L;
                        outStream.SetLength(length);
                        proxyStream.FixedCopyTo(outStream, length);
                        outStream.Position = 0L;
                        var tunnel = this.CreateTunnel(TunnelCommand.Send, proxyClient);
                        if (localAgentSock != null)
                        {
                            tunnel.Headers[xHttpHandler.AgentReverse] = localAgentSock.Value.ToString("N");
                        }
#if DEBUG
                        tunnel.Form[xHttpHandler.AgentChecksum] = CryptoManaged.MD5Hash(outStream);
                        outStream.Position = 0L;
#endif
                        tunnel.Files.Add(new HttpFile(xHttpHandler.AgentDirect, xHttpHandler.AgentDirect, outStream));
                        var response = tunnel.GetResponse();
                        if (response.GetResponseText() != "1")
                        {
                            this.OutWrite("{0} Send {1}\t{2}bytes failure.", localIpe, destIpe, length);
                            continue;
                        }
                        this.OutWrite("{0} Send {1}\t{2}bytes.", localIpe, destIpe, length);
                    }
                    catch (WebException ex)
                    {
                        if (TunnelExceptionHandler.Handle(ex, string.Format("DirectSend={0}", destIpe), _output))
                        {
                            proxyClient.Client.Close();
                        }
                    }
                    catch (IOException ex)
                    {
                        var sockEx = ex.InnerException as SocketException;
                        if (sockEx != null)
                        {
                            //由于serverPush关闭后，无法将服务端主动关闭信号转发过来，因此proxyStream.Read()会引发Interrupted的异常；
                            //也就是说错误的原因在于调用Close()后，线程恰好继续向网络缓冲区中读取数据，所以引发SocketException。
                            if (sockEx.SocketErrorCode == SocketError.Interrupted)
                            {
#if DEBUG
                                Hub.LogInfo(string.Format("Predictable interrupted exception: {0}", sockEx.Message));
#endif
                                return;
                            }
                        }
                        TunnelExceptionHandler.Handle(ex, proxyClient.Client, string.Format("DirectSend={0}", destIpe));
                    }
                    catch (SocketException ex)
                    {
                        TunnelExceptionHandler.Handle(ex, proxyClient.Client, string.Format("DirectSend={0}", destIpe));
                    }
                }
            }
            catch (Exception ex)
            {
                TunnelExceptionHandler.Handle(ex, string.Format("DirectSend={0}", destIpe));
            }
        }
        #endregion
    }
}