using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Net
{
    public static class ProxyUtility
    {
        /// <summary>
        /// 准备代理数据包
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="remoteEndPoint"></param>
        /// <returns></returns>
        public static byte[] HttpRequest(SocksProxy proxy, EndPoint remoteEndPoint)
        {
            Contract.Requires(proxy != null && remoteEndPoint != null);

            byte[] result = null;
            string host;
            var destDe = remoteEndPoint as DnsEndPoint;
            if (destDe != null)
            {
                host = destDe.ToString();
            }
            else
            {
                var destIpe = (IPEndPoint)remoteEndPoint;
                host = destIpe.ToString();
            }
            if (proxy.Credential == null)
            {
                result = Encoding.ASCII.GetBytes(string.Format("CONNECT {0} HTTP/1.1\r\nHost: {0}\r\n\r\n", host));
            }
            else
            {
                var credential = proxy.Credential;
                string base64Encoding = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", credential.UserName, credential.Password)));
                result = Encoding.ASCII.GetBytes(string.Format("CONNECT {0} HTTP/1.1\r\nHost: {0}\r\nProxy-Authorization: Basic {1}\r\n\r\n", host, base64Encoding));
            }
            return result;
        }

        public static void HttpResponse(SocksProxy proxy, byte[] bPack)
        {
            var match = Regex.Match(Encoding.ASCII.GetString(bPack), @"[HTTP/]\d[.]\d\s(?<code>\d+)\s(?<reason>.+)");
            if (!match.Success)
            {
                throw new ProxyAuthException(0, "Invalid proxy message response.");
            }
            int code = Convert.ToInt32(match.Groups["code"].Value);
            if (code != 200)
            {
                throw new ProxyAuthException(code, match.Groups["reason"].Value);
            }
        }
    }
}