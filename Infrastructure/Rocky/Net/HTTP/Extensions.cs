using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace Rocky.Net
{
    public static partial class Extensions
    {
        /// <summary>
        /// Copies all headers and content (except the URL) from an incoming to an outgoing
        /// request.
        /// </summary>
        /// <param name="source">The request to copy from</param>
        /// <param name="destination">The request to copy to</param>
        public static void CopyTo(this System.Web.HttpRequestBase source, HttpWebRequest destination)
        {
            Contract.Requires(source != null && destination != null);

            //注意：HttpWebRequire.Method默认为Get，
            //在写入请求前必须把HttpWebRequire.Method设置为Post,
            //否则在使用BeginGetRequireStream获取请求数据流的时候，系统就会发出“无法发送具有此谓词类型的内容正文”的异常。
            destination.Method = source.HttpMethod;

            // Copy unrestricted headers (including cookies, if any)
            foreach (var headerKey in source.Headers.AllKeys)
            {
                switch (headerKey)
                {
                    case "Connection":
                    case "Content-Length":
                    case "Date":
                    case "Expect":
                    case "Host":
                    case "If-Modified-Since":
                    case "Range":
                    case "Transfer-Encoding":
                    case "Proxy-Connection":
                        // Let IIS handle these
                        break;

                    case "Accept":
                    case "Content-Type":
                    case "Referer":
                    case "User-Agent":
                        // Restricted - copied below
                        break;

                    default:
                        destination.Headers[headerKey] = source.Headers[headerKey];
                        break;
                }
            }

            // Copy restricted headers
            if (!source.AcceptTypes.IsNullOrEmpty())
            {
                destination.Accept = string.Join(",", source.AcceptTypes);
            }
            destination.ContentType = source.ContentType;
            if (source.UrlReferrer != null)
            {
                destination.Referer = source.UrlReferrer.AbsoluteUri;
            }
            destination.UserAgent = source.UserAgent;
            destination.ContentLength = source.ContentLength;
            destination.ContentType = source.ContentType;
            destination.KeepAlive = source.Headers["Connection"] != "close";
            DateTime ifModifiedSince;
            if (DateTime.TryParse(source.Headers["If-Modified-Since"], out ifModifiedSince))
            {
                destination.IfModifiedSince = ifModifiedSince;
            }
            string transferEncoding = source.Headers["Transfer-Encoding"];
            if (transferEncoding != null)
            {
                destination.SendChunked = true;
                destination.TransferEncoding = transferEncoding;
            }

            // Copy content (if content body is allowed)
            if (source.HttpMethod != WebRequestMethods.Http.Get && source.HttpMethod != WebRequestMethods.Http.Head && source.ContentLength > 0)
            {
                var destinationStream = destination.GetRequestStream();
                source.InputStream.FixedCopyTo(destinationStream, source.ContentLength);
                destinationStream.Close();
            }
        }

        public static Encoding GetContentEncoding(this HttpWebResponse instance)
        {
            Contract.Requires(instance != null);

            return string.IsNullOrEmpty(instance.CharacterSet) ? Encoding.UTF8 : Encoding.GetEncoding(instance.CharacterSet);
        }

        public static string GetResponseText(this HttpWebResponse instance)
        {
            Contract.Requires(instance != null);

            var resStream = instance.GetResponseStream();
            if (instance.ContentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase))
            {
                var mem = new MemoryStream();
                using (var gzipStream = new GZipStream(resStream, CompressionMode.Decompress, true))
                {
                    gzipStream.FixedCopyTo(mem);
                }
                mem.Position = 0L;
                resStream = mem;
            }
            using (var reader = new StreamReader(resStream, instance.GetContentEncoding()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}