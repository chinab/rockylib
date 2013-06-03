using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace Rocky.Net
{
    public partial class xHttpHandler
    {
        #region Fields
        private static volatile ushort _udpPortOffset = 1090;
        #endregion

        #region Methods
        public static UdpClient CreateUdpClient()
        {
            //var udpSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //udpSock.ReuseAddress();
            //udpSock.Bind(new IPEndPoint(IPAddress.Any, _udpPortOffset++));

            var client = new UdpClient(new IPEndPoint(IPAddress.Any, _udpPortOffset++));
            if (_udpPortOffset == IPEndPoint.MaxPort)
            {
                _udpPortOffset = 1090;
            }
            //client.Client = udpSock;
            return client;
        }
        #endregion

        #region UdpDirect
        public void UdpDirectSend(HttpContext context, UdpClient proxyClient, IPEndPoint remoteIpe)
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
            int length = (int)inStream.Length;
            try
            {
                var reader = new BinaryReader(inStream);
                proxyClient.Send(reader.ReadBytes(length), length, remoteIpe);
                succeed = true;
                Runtime.LogInfo("UdpDirectSend to {0} {1}bytes.", remoteIpe, length);
            }
            catch (ObjectDisposedException ex)
            {
#if DEBUG
                Runtime.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
            }
            catch (SocketException ex)
            {
                TunnelExceptionHandler.Handle(ex, proxyClient.Client, "UdpDirectSend");
            }
            if (Response.IsClientConnected)
            {
                Response.Write(Convert.ToByte(succeed));
            }
        }

        public void UdpDirectReceive(HttpContext context, UdpClient proxyClient)
        {
            context.Server.ScriptTimeout = int.MaxValue;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            var bPack = new List<byte>();
            while (Response.IsClientConnected)
            {
                try
                {
                    var remoteIpe = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                    byte[] data = proxyClient.Receive(ref remoteIpe);
                    if (!Response.IsClientConnected)
                    {
                        break;
                    }
                    bPack.Clear();
                    Socks4Request.PackIn(bPack, remoteIpe);
                    bPack.AddRange(data);
                    byte[] bData = bPack.ToArray();
                    Response.OutputStream.Write(bData, 0, bData.Length);
                    Response.Flush();
                    Runtime.LogInfo("UdpDirectReceive from {0} {1}bytes.", remoteIpe, data.Length);
                }
                catch (ObjectDisposedException ex)
                {
#if DEBUG
                    Runtime.LogInfo(string.Format("Predictable objectDisposed exception: {0}", ex.StackTrace));
#endif
                }
                catch (IOException ex)
                {
                    TunnelExceptionHandler.Handle(ex, proxyClient.Client, "UdpDirectReceive");
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                    {
#if DEBUG
                        Runtime.LogInfo(string.Format("Predictable interrupted exception: {0}", ex.Message));
#endif
                        return;
                    }
                    TunnelExceptionHandler.Handle(ex, proxyClient.Client, "UdpDirectReceive");
                }
            }
        }
        #endregion
    }
}