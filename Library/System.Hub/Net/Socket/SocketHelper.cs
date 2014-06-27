using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Caching;

namespace System.Net
{
    /// <summary>
    /// 全局类
    /// </summary>
    public static class SocketHelper
    {
        #region Fields
        private const string DnsPrefix = "DNS_";
        #endregion

        #region Methods
        /// <summary>
        /// 返回指定主机的 InternetV4 地址，有缓存。
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <returns></returns>
        public static IPAddress[] GetHostAddresses(string hostNameOrAddress = null)
        {
            if (hostNameOrAddress == null)
            {
                hostNameOrAddress = Dns.GetHostName();
            }

            string key = DnsPrefix + hostNameOrAddress;
            ObjectCache Cache = MemoryCache.Default;
            var addrs = Cache[key] as IPAddress[];
            if (addrs == null)
            {
                //var hostEntry = Dns.GetHostEntry(hostNameOrAddress);
                try
                {
                    Cache[key] = addrs = Dns.GetHostAddresses(hostNameOrAddress).Where(t => t.AddressFamily == AddressFamily.InterNetwork).ToArray();
                }
                catch (SocketException ex)
                {
                    App.LogError(ex, "解析{0}失败", hostNameOrAddress);
                    throw;
                }
            }
            return addrs;
        }

        /// <summary>
        /// 返回网络端点，DNS有缓存。
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static IPEndPoint ParseEndPoint(string endPoint)
        {
            Contract.Requires(endPoint != null);

            var arr = endPoint.Split(':');
            if (arr.Length != 2)
            {
                throw new ArgumentException(string.Format("'{0}' is invalid", endPoint));
            }
            //App.LogInfo("ParseEndPoint={0}", endPoint);
            var addr = GetHostAddresses(arr[0]).First();
            return new IPEndPoint(addr, int.Parse(arr[1]));
        }

        /// <summary>
        /// 创建sock连接池
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static ISocketPool CreatePool(DistributedEndPoint endpoint)
        {
            return new SocketPool(endpoint);
        }

        public static void CreateListener(out Socket server, EndPoint boundEP, ushort maxClient = 128)
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.ReuseAddress(boundEP);
            server.Listen(maxClient);
        }

        public static void CloseListener(ref Socket server)
        {
            //关闭连接，否则客户端会认为是强制关闭 
            if (server.Poll(-1, SelectMode.SelectRead))
            {
                server.Shutdown(SocketShutdown.Both);
            }
            server.Close();
            server = null;
        }
        #endregion
    }
}