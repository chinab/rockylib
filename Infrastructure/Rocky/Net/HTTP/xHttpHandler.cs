using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace Rocky.Net
{
    public partial class xHttpHandler : IHttpHandler
    {
        #region StaticMembers
        #region Fields
        internal const string AgentSock = "Agent-Sock",
            AgentDirect = "Agent-Direct",
            AgentReverse = "Agent-Reverse",
            AgentCommand = "Command",
            AgentChecksum = "Checksum";
        /// <summary>
        /// 超时时间300秒(5分钟)
        /// </summary>
        internal const ushort Timeout = 300;
        internal static readonly byte[] EmptyBuffer = new byte[0];
        internal static readonly IPEndPoint GoAgent = new IPEndPoint(IPAddress.Loopback, 8087);

        internal static readonly ushort MaxDevice;
        /// <summary>
        /// P2P软件代理域名，客户端必须在本地hosts中配置
        /// </summary>
        private static readonly string Host, CryptoKey;
        private static readonly ushort[] BlockPorts;
        private static readonly xUserManager OnlineUsers;
        #endregion

        #region Constructor
        static xHttpHandler()
        {
            ushort.TryParse(ConfigurationManager.AppSettings["Agent-MaxDevice"], out MaxDevice);
            Host = ConfigurationManager.AppSettings["Agent-Host"];
            var q = from t in (ConfigurationManager.AppSettings["Agent-BlockPorts"] ?? string.Empty).Split(',')
                    where !string.IsNullOrEmpty(t)
                    select ushort.Parse(t);
            BlockPorts = q.ToArray();
            CryptoKey = ConfigurationManager.AppSettings["Agent-CryptoKey"];
            var q2 = from t in (ConfigurationManager.AppSettings["Agent-Credentials"] ?? string.Empty).Split(',')
                     where !string.IsNullOrEmpty(t)
                     select CryptoManaged.MD5Hash(t);
            OnlineUsers = new xUserManager(q2.ToArray());
        }
        #endregion

        #region Methods
        public static CryptoManaged CreateCrypto(string credential)
        {
            Contract.Requires(!string.IsNullOrEmpty(credential));

            return new CryptoManaged(CryptoKey, credential);
        }

        public static void DownloadFile(HttpContext context, string filePath)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            Response.Clear();
            Response.Buffer = false;
            Response.AddHeader("Accept-Ranges", "bytes");
            Response.ContentType = MediaTypeNames.Application.Octet;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long offset = 0L, length = stream.Length;
                #region Resolve Range
                string range = Request.Headers["Range"];
                int index;
                //Range:bytes=1024-
                //Range:bytes=1024-2048
                //Range:bytes=-1024
                //Range:bytes=0-512,1024-2048
                if (range != null && (index = range.IndexOf("=")) != -1)
                {
                    string[] ranges = range.Substring(index + 1).Split(',');
                    if (ranges.Length > 1)
                    {
                        Response.StatusCode = 416;  //not supported multipart/byterange
                        Response.End();
                    }

                    bool flag = false;
                    if (ranges[0].StartsWith("-"))
                    {
                        long _p, _absp;
                        if (long.TryParse(ranges[0], out _p) && (_absp = Math.Abs(_p)) <= length)
                        {
                            if (_p < 0)
                            {
                                offset = length - _absp;
                                length = _absp;
                                flag = true;
                            }
                        }
                    }
                    else
                    {
                        ranges = ranges[0].Split('-');
                        if (ranges.Length == 2)
                        {
                            long _p, _l;
                            if (ranges[1] == string.Empty)
                            {
                                if (long.TryParse(ranges[0], out _p) && _p <= length)
                                {
                                    offset = _p;
                                    flag = true;
                                }
                            }
                            else if (long.TryParse(ranges[0], out _p) && long.TryParse(ranges[1], out _l) && _p > 0 && _l > 0 && _p < _l && _l < length)
                            {
                                offset = _p;
                                length = _l + 1;
                                flag = true;
                            }
                        }
                    }
                    if (!flag)
                    {
                        Response.StatusCode = 416;  //Requested range not satisfiable
                        Response.End();
                    }
                    Response.StatusCode = 206;
                    Response.AddHeader("Content-Range", "bytes " + offset.ToString() + "-" + length.ToString() + "/" + stream.Length.ToString());
                }
                #endregion
                Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(Path.GetFileName(filePath)));
                Response.AddHeader("Content-Length", length.ToString());
                Response.TransmitFile(filePath, offset, length);
            }
            Response.End();
        }

        public static void UploadFile(HttpContext context, string savePath)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            var httpFile = Request.Files[0];
            if (httpFile == null)
            {
                Response.StatusCode = 400;  //Bad Request
                Response.End();
            }
            string rangeFilename = CryptoManaged.MD5Hash(httpFile.FileName + httpFile.ContentLength);
            var file = new FileInfo(Path.Combine(Path.GetDirectoryName(savePath), rangeFilename));
            Response.AddHeader("Content-Length", (file.Exists ? file.Length : 0L).ToString());
            long offset;
            if (!long.TryParse(Request.Headers["Range"], out offset) || file.Length != offset)
            {
                Response.StatusCode = 416;  //Requested range not satisfiable
                Response.End();
            }

            using (var stream = file.Open(file.Exists ? FileMode.Append : FileMode.CreateNew, FileAccess.Write, FileShare.Write))
            {
                int length = httpFile.ContentLength;

                Response.AddHeader("Content-Range", "bytes " + offset.ToString() + "-" + length.ToString() + "/" + length.ToString());
                Response.StatusCode = 206;
                stream.Position = offset;
                httpFile.InputStream.FixedCopyTo(stream, length, per =>
                {
                    stream.Flush();
                    return Response.IsClientConnected;
                });
            }
            Response.End();
        }
        #endregion
        #endregion

        #region IHttpHandler
        bool IHttpHandler.IsReusable
        {
            get { return true; }
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;
            Response.DisableKernelCache();
            Response.Cache.SetNoServerCaching();
            Response.Cache.SetNoStore();
            Response.Cache.SetNoTransforms();
            Response.Buffer = false;
            //Response.ContentType = MediaTypeNames.Application.Octet;
            Response.ContentType = MediaTypeNames.Text.Html;
            Response.ContentEncoding = Encoding.UTF8;

            string agentCredential = Request.Headers[HttpRequestHeader.Authorization.ToString()];
            int agentCommand = 0;
            if (string.IsNullOrEmpty(agentCredential)
                || !int.TryParse(Request.Form[AgentCommand] ?? Request.Headers[AgentCommand], out agentCommand))
            {
                this.ResponseForbidden(context);
            }
            switch ((TunnelCommand)agentCommand)
            {
                case TunnelCommand.xInject:
                    {
                        string checksum = Request.Form["checksum"];
                        if (string.IsNullOrEmpty(checksum))
                        {
                            this.ResponseForbidden(context);
                        }
                        Stream raw = null;
                        var rawFile = Request.Files["raw"];
                        if (rawFile != null)
                        {
                            raw = rawFile.InputStream;
                        }
                        object arg = Request.Form["arg"];
                        Runtime.Inject(checksum, raw, arg);
                    }
                    break;
                case TunnelCommand.KeepAlive:
                    {
                        var httpFile = Request.Files[AgentChecksum];
                        if (httpFile == null)
                        {
                            this.ResponseForbidden(context);
                        }
                        var crypto = CreateCrypto(agentCredential);
                        var outStream = crypto.Decrypt(httpFile.InputStream);
                        string checksum = Encoding.UTF8.GetString(outStream.ToArray());
                        IPAddress LAN_addr = IPAddress.Parse(checksum),
                            WAN_addr = IPAddress.Parse(Request.UserHostAddress);
                        Guid deviceID = OnlineUsers.SignIn(agentCredential, WAN_addr, LAN_addr);
                        Runtime.LogInfo(string.Format("SignIn: WAN={0}, LAN={1}", WAN_addr, LAN_addr));
                        try
                        {
                            var user = OnlineUsers.GetUser(agentCredential);
                            this.KeepAlive(context, user, ref deviceID);
                        }
                        finally
                        {
                            OnlineUsers.SignOut(agentCredential, deviceID);
                            Runtime.LogInfo(string.Format("SignOut: WAN={0}, LAN={1}", WAN_addr, LAN_addr));
                        }
                    }
                    break;
                case TunnelCommand.Receive:
                    {
                        //用户在线
                        var user = OnlineUsers.GetUser(agentCredential);
                        Guid sock;
                        IPEndPoint destIpe;
                        this.CheckSocks(context, out sock, out destIpe);
                        CheckReverseResult checkResult;
                        Guid deviceID, remoteID_LocalSock;
                        if ((checkResult = this.CheckReverse(context, out deviceID, out remoteID_LocalSock)) != CheckReverseResult.None)
                        {
                            try
                            {
                                switch (checkResult)
                                {
                                    case CheckReverseResult.Connect:
                                        var q = from t in user.Principal
                                                where t.ID == deviceID
                                                select new IPEndPoint(t.WAN, IPEndPoint.MinPort);
                                        var localIpe = q.Single();
                                        OnlineUsers.ReverseConnect(sock, localIpe, remoteID_LocalSock, destIpe);
                                        var dataQueue = OnlineUsers.GetReverseQueue(sock, false);
                                        //30秒超时时间
                                        dataQueue.WaitHandle.WaitOne(30 * 1000);
                                        if (!dataQueue.Connected)
                                        {
                                            Runtime.LogInfo("ProxyClient connect {0} error", destIpe);
                                            this.ResponseForbidden(context, HttpStatusCode.BadGateway);
                                        }
                                        break;
                                    default:
                                        OnlineUsers.ReverseShakeHands(remoteID_LocalSock, sock);
                                        break;
                                }
                                this.ReverseDirectReceive(context, ref sock);
                            }
                            finally
                            {
                                OnlineUsers.ReverseDisconnect(sock);
                            }
                        }
                        else
                        {
                            try
                            {
                                var proxyClient = user.Connect(sock, destIpe);
                                this.DirectReceive(context, proxyClient);
                            }
                            catch (SocketException ex)
                            {
                                Runtime.LogError(ex, "ProxyClient connect {0} error", destIpe);
                                this.ResponseForbidden(context, HttpStatusCode.BadGateway);
                            }
                            finally
                            {
                                user.Disconnect(sock);
                            }
                        }
                    }
                    break;
                case TunnelCommand.Send:
                    {
                        //用户在线，30秒超时时间
                        var user = OnlineUsers.GetUser(agentCredential);
                        Guid sock;
                        IPEndPoint destIpe;
                        this.CheckSocks(context, out sock, out destIpe);
                        Guid deviceID, remoteID_LocalSock;
                        if (this.CheckReverse(context, out deviceID, out remoteID_LocalSock) != CheckReverseResult.None)
                        {
                            if (!Runtime.Retry(() =>
                            {
                                var dataQueue = OnlineUsers.GetReverseQueue(sock, true, false);
                                return dataQueue != null && dataQueue.Connected;
                            }, 250, 120))
                            {
                                this.ResponseForbidden(context, HttpStatusCode.GatewayTimeout);
                            }
                            this.ReverseDirectSend(context, ref sock);
                        }
                        else
                        {
                            TcpClient proxyClient = null;
                            if (!Runtime.Retry(() => (proxyClient = user.GetClient(sock, false)) != null, 250, 120))
                            {
                                this.ResponseForbidden(context, HttpStatusCode.GatewayTimeout);
                            }
                            this.DirectSend(context, proxyClient);
                        }
                    }
                    break;
                case TunnelCommand.DeviceIdentity:
                    {
                        var user = OnlineUsers.GetUser(agentCredential);
                        var stream = Serializer.Serialize(user.Principal);
                        stream.FixedCopyTo(Response.OutputStream);
                        Response.Flush();
                    }
                    break;
                case TunnelCommand.UdpSend:
                    {
                        var user = OnlineUsers.GetUser(agentCredential);
                        Guid sock;
                        IPEndPoint destIpe;
                        this.CheckSocks(context, out sock, out destIpe);
                        var proxyClient = user.GetUdpClient(sock);
                        this.UdpDirectSend(context, proxyClient, destIpe);
                    }
                    break;
                case TunnelCommand.UdpReceive:
                    {
                        var user = OnlineUsers.GetUser(agentCredential);
                        Guid sock;
                        IPEndPoint destIpe;
                        this.CheckSocks(context, out sock, out destIpe);
                        UdpClient proxyClient = null;
                        if (!Runtime.Retry(() => user.HasUdpClient(sock, out proxyClient), 250, 120))
                        {
                            this.ResponseForbidden(context, HttpStatusCode.GatewayTimeout);
                        }
                        this.UdpDirectReceive(context, proxyClient);
                    }
                    break;
                default:
                    this.OnProcess(context);
                    break;
            }
            Response.End();
        }

        /// <summary>
        /// Extend
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnProcess(HttpContext context)
        {
            context.Response.ContentType = MediaTypeNames.Text.Plain;
        }
        #endregion

        #region Methods
        protected void ResponseForbidden(HttpContext context, HttpStatusCode httpCode = HttpStatusCode.Forbidden)
        {
            context.Response.StatusCode = (int)httpCode;
            context.Response.End();
        }

        private void CheckSocks(HttpContext context, out Guid agentSock, out IPEndPoint agentDirect)
        {
            string direct = context.Request.Headers[AgentDirect];
            if (!Guid.TryParseExact(context.Request.Headers[AgentSock], "N", out agentSock) || string.IsNullOrEmpty(direct))
            {
                this.ResponseForbidden(context);
            }
            try
            {
                agentDirect = SocketHelper.ParseEndPoint(direct);
                if (agentDirect.Address.Equals(IPAddress.Loopback) && BlockPorts.Contains((ushort)agentDirect.Port))
                {
                    throw new HttpException(403, string.Format("CheckSocks proxyClient({0}) invalid", agentDirect));
                }
            }
            catch (ArgumentException)
            {
                agentDirect = null;
                this.ResponseForbidden(context);
            }
        }

        private enum CheckReverseResult
        {
            None,
            Connect,
            ShakeHands,
        }
        /// <summary>
        /// 检测是否有反向连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agentSock"></param>
        /// <param name="deviceID">如是Connect则不为Empty</param>
        /// <param name="remoteID_LocalSock"></param>
        /// <returns></returns>
        private CheckReverseResult CheckReverse(HttpContext context, out Guid deviceID, out Guid remoteID_LocalSock)
        {
            remoteID_LocalSock = deviceID = Guid.Empty;
            string agentReverse = context.Request.Headers[AgentReverse];
            if (string.IsNullOrEmpty(agentReverse))
            {
                return CheckReverseResult.None;
            }
            var args = agentReverse.Split('#');
            //ClientID#RemoteID 反向连接请求
            if (args.Length == 2)
            {
                deviceID = Guid.ParseExact(args[0], "N");
                remoteID_LocalSock = Guid.ParseExact(args[1], "N");
                return CheckReverseResult.Connect;
            }
            else if (Guid.TryParseExact(args[0], "N", out remoteID_LocalSock))
            {
                return CheckReverseResult.ShakeHands;
            }
            return CheckReverseResult.None;
        }

        private bool CanDirect()
        {
            return true;
        }
        #endregion

        #region KeepAlive
        private void KeepAlive(HttpContext context, xUserState user, ref Guid deviceID)
        {
            context.Server.ScriptTimeout = int.MaxValue;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            var bWriter = new BinaryWriter(Response.OutputStream, Encoding.UTF8);
            bWriter.Write(deviceID.ToByteArray());
            bWriter.Write(Host);
            Response.Flush();

            var stream = new MemoryStream();
            while (Response.IsClientConnected)
            {
                var rConnState = user.PopReverseListen(deviceID);
                if (!Response.IsClientConnected)
                {
                    break;
                }
                if (rConnState.AgentSock == Guid.Empty)
                {
                    continue;
                }

                stream.SetLength(stream.Position = 0L);
                Serializer.SerializeTo(rConnState, stream);
                stream.Position = 0L;
                int length = (int)stream.Length;
                stream.FixedCopyTo(Response.OutputStream, length);
                Response.Flush();
                Runtime.LogInfo("ReverseListen: streamLength={0}.", stream.Length);
            }
        }
        #endregion

        #region TcpDirect
        private void DirectReceive(HttpContext context, TcpClient proxyClient)
        {
            context.Server.ScriptTimeout = int.MaxValue;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;
            //Response.AppendHeader(HttpResponseHeader.Connection.ToString(), "close");

            var proxyStream = proxyClient.GetStream();
            while (proxyClient.Connected && Response.IsClientConnected)
            {
                try
                {
                    proxyStream.Read(EmptyBuffer, 0, 0);
                    int length;
                    //阻塞解除后做第2道检查
                    if (!(proxyClient.Connected && Response.IsClientConnected) || (length = proxyClient.Available) == 0)
                    {
                        //服务端主动关闭，发送空包
                        if (Response.IsClientConnected)
                        {
                            Response.OutputStream.Write(EmptyBuffer, 0, 0);
                            Response.Flush();
                        }
                        break;
                    }
                    proxyStream.FixedCopyTo(Response.OutputStream, length);
                    if (Response.IsClientConnected)
                    {
                        Response.Flush();
                    }
#if DEBUG
                    Runtime.LogInfo("DirectReceive from {0} {1}bytes.", proxyClient.Client.RemoteEndPoint, length);
#endif
                }
                catch (IOException ex)
                {
                    TunnelExceptionHandler.Handle(ex, proxyClient.Client, "DirectReceive");
                }
                catch (SocketException ex)
                {
                    TunnelExceptionHandler.Handle(ex, proxyClient.Client, "DirectReceive");
                }
            }
        }

        private void DirectSend(HttpContext context, TcpClient proxyClient)
        {
            context.Server.ScriptTimeout = Timeout;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            var httpFile = Request.Files[AgentDirect];
            if (httpFile == null)
            {
                this.ResponseForbidden(context);
            }
            Stream inStream = httpFile.InputStream;
#if DEBUG
            string checksum = Request.Form[AgentChecksum];
            if (string.IsNullOrEmpty(checksum))
            {
                this.ResponseForbidden(context);
            }
            var mem = new MemoryStream();
            inStream.FixedCopyTo(mem, httpFile.ContentLength);
            mem.Position = 0L;
            string checkKey = CryptoManaged.MD5Hash(mem);
            if (checksum != checkKey)
            {
                this.ResponseForbidden(context);
            }
            mem.Position = 0L;
            inStream = mem;
#endif
            bool succeed = false;
            if (proxyClient.Connected)
            {
                var proxyStream = proxyClient.GetStream();
                int length = (int)inStream.Length;
                try
                {
                    inStream.FixedCopyTo(proxyStream, length);
                    succeed = true;
#if DEBUG
                    Runtime.LogInfo("DirectSend to {0} {1}bytes.", proxyClient.Client.RemoteEndPoint, length);
#endif
                }
                catch (IOException ex)
                {
                    TunnelExceptionHandler.Handle(ex, proxyClient.Client, "DirectSend");
                }
            }
            if (Response.IsClientConnected)
            {
                Response.Write(Convert.ToByte(succeed));
            }
        }
        #endregion

        #region TcpReverseDirect
        private void ReverseDirectReceive(HttpContext context, ref Guid agentSock)
        {
            context.Server.ScriptTimeout = int.MaxValue;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;
            //Response.AppendHeader(HttpResponseHeader.Connection.ToString(), "close");

            var dataQueue = OnlineUsers.GetReverseQueue(agentSock, false);
            while (dataQueue.Connected && Response.IsClientConnected)
            {
                try
                {
                    dataQueue.WaitHandle.WaitOne();
                    Stream outStream;
                    //阻塞解除后做第2道检查
                    if (!dataQueue.TryPop(out outStream) || !(dataQueue.Connected && Response.IsClientConnected))
                    {
                        //服务端主动关闭，发送空包
                        if (Response.IsClientConnected)
                        {
                            Response.OutputStream.Write(EmptyBuffer, 0, 0);
                            Response.Flush();
                        }
                        break;
                    }
                    int length = (int)outStream.Length;
                    outStream.FixedCopyTo(Response.OutputStream, length);
                    if (Response.IsClientConnected)
                    {
                        Response.Flush();
                    }
#if DEBUG
                    Runtime.LogInfo("ReverseDirectReceive from {0} {1}bytes.", dataQueue.RemoteEndPoint, length);
#endif
                }
                catch (IOException ex)
                {
                    TunnelExceptionHandler.Handle(ex, "ReverseDirectReceive");
                }
            }
        }

        private void ReverseDirectSend(HttpContext context, ref Guid agentSock)
        {
            context.Server.ScriptTimeout = Timeout;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            var httpFile = Request.Files[AgentDirect];
            if (httpFile == null)
            {
                this.ResponseForbidden(context);
            }
            Stream inStream = httpFile.InputStream;
#if DEBUG
            string checksum = Request.Form[AgentChecksum];
            if (string.IsNullOrEmpty(checksum))
            {
                this.ResponseForbidden(context);
            }
            var mem = new MemoryStream();
            inStream.FixedCopyTo(mem, httpFile.ContentLength);
            mem.Position = 0L;
            string checkKey = CryptoManaged.MD5Hash(mem);
            if (checksum != checkKey)
            {
                this.ResponseForbidden(context);
            }
            mem.Position = 0L;
            inStream = mem;
#endif
            bool succeed = false;
            var dataQueue = OnlineUsers.GetReverseQueue(agentSock, true);
            if (dataQueue.Connected)
            {
                int length = (int)inStream.Length;
                if (length > 0)
                {
                    dataQueue.Push(inStream);
                }
                dataQueue.WaitHandle.Set();
                succeed = true;
#if DEBUG
                Runtime.LogInfo("ReverseDirectSend to {0} {1}bytes.", dataQueue.RemoteEndPoint, length);
#endif
            }
            if (Response.IsClientConnected)
            {
                Response.Write(Convert.ToByte(succeed));
            }
        }
        #endregion
    }
}