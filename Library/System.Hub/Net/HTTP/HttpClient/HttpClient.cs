using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using E = System.Environment;

namespace System.Net
{
    /// <summary>
    /// http://hc.apache.org
    /// </summary>
    public partial class HttpClient : IHttpClient
    {
        #region StaticMembers
        /// <summary>
        /// http://
        /// </summary>
        public static readonly string HttpScheme = Uri.UriSchemeHttp + Uri.SchemeDelimiter;
        internal static readonly Uri Default = new Uri("http://www.google.com/#Timothy.net");
        internal const string DefaultUserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html#Timothy.net)";
        internal const string ValidateResponseFailure = "ValidateResponseFailure";

        static HttpClient()
        {
#if !Mono
            //返回域名多IP地址
            ServicePointManager.EnableDnsRoundRobin = true;
#endif
            ServicePointManager.DefaultConnectionLimit = ushort.MaxValue;
            ServicePointManager.MaxServicePoints = 0;
            ServicePointManager.MaxServicePointIdleTime = 100000;
            checked
            {
                int keep = (int)xHttpHandler.KeepAliveInterval;
                ServicePointManager.SetTcpKeepAlive(true, keep, keep);
            }
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
        }

        /// <summary>
        /// https 忽略证书错误
        /// RequestUri.Scheme == Uri.UriSchemeHttps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool CheckValidationResult(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            // Always accept
            return true;
        }
        #endregion

        #region Fields
        private bool _initRequest;
        private string _referer;
        private CookieContainer _cookieContainer;
        private HttpRequestContent _content;
        private HttpWebRequest _request;
        private WebProxy _proxyAddr;
        private Func<HttpWebResponse, bool> _validateResponse;
        #endregion

        #region Properties
        /// <summary>
        /// 强制移除PersistentConnection为2的限制
        /// </summary>
        public bool KeepAlive
        {
            get { return _request.KeepAlive; }
            set
            {
                if (_request.KeepAlive = value)
                {
#if !Mono
                    //var sp = _request.ServicePoint;
                    //var prop = sp.GetType().GetProperty("HttpBehaviour", BindingFlags.NonPublic | BindingFlags.Instance);
                    //prop.SetValue(sp, (byte)0, null);
#endif
                }
            }
        }
        /// <summary>
        /// "ConnectTimeout" is the time for the server to respond to a request, not the amount of time to wait for the server to respond and send down all of the data.
        /// </summary>
        public int ConnectTimeout
        {
            get { return _request.Timeout; }
            set { _request.Timeout = value; }
        }
        /// <summary>
        /// "SendReceiveTimeout" applies to Read or Write operations to streams that transmit over the connection. 
        /// </summary>
        public int SendReceiveTimeout
        {
            get { return _request.ReadWriteTimeout; }
            set { _request.ReadWriteTimeout = value; }
        }
        public bool UseCookies
        {
            get { return _cookieContainer != null; }
            set
            {
                if (value)
                {
                    if (_cookieContainer == null)
                    {
                        _cookieContainer = new CookieContainer();
                    }
                }
                else
                {
                    _cookieContainer = null;
                }
            }
        }

        public WebHeaderCollection Headers
        {
            get { return _content.Headers; }
        }
        public CookieCollection Cookies
        {
            get
            {
                Contract.Requires(this.UseCookies);

                return _cookieContainer.GetCookies(_request.RequestUri);
            }
        }
        public NameValueCollection Form
        {
            get { return _content.Form; }
        }
        public List<HttpFileContent> Files
        {
            get { return _content.Files; }
        }
        #endregion

        #region Constructors
        public HttpClient(Uri url = null, Func<HttpWebResponse, bool> validateResponse = null)
        {
            _referer = Default.OriginalString;
            _content = new HttpRequestContent();
            _validateResponse = validateResponse;
            this.SetRequest(url ?? Default);
        }
        #endregion

        #region Request
        public Uri BuildUri(string url, NameValueCollection queryString)
        {
            if (queryString.Count > 0)
            {
                url += _content.GetFormString(queryString, url.Contains(HttpRequestContent.Symbol_AndFirst));
            }
            return new Uri(url);
        }

        public virtual void SetRequest(Uri url, NetworkCredential credential = null, bool newRequest = true, string checksum = null, Stream rawStream = null)
        {
            _request = (HttpWebRequest)WebRequest.Create(url);
            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _request.Accept = "*/*";
            _request.UserAgent = DefaultUserAgent;
            _request.Referer = _referer;
            _request.CookieContainer = _cookieContainer;
            if (_proxyAddr != null)
            {
                _request.Proxy = _proxyAddr;
            }
            if (credential != null)
            {
                _request.SendChunked = false;
                _request.PreAuthenticate = true;
                _request.UseDefaultCredentials = false;
                _request.Credentials = credential;
            }
            if (newRequest)
            {
                _content.Clear();
                _initRequest = true;
            }

            if (checksum != null)
            {
                _content.Form["checksum"] = checksum;
            }
            if (rawStream != null)
            {
                _content.Files.Add(new HttpFileContent("raw", "rawFile", rawStream));
            }
        }

        public virtual void SetProxy(EndPoint address, NetworkCredential credential = null)
        {
            _proxyAddr = new WebProxy(string.Format("http://{0}", address));
            if (credential != null)
            {
                _proxyAddr.UseDefaultCredentials = false;
                _proxyAddr.Credentials = credential;
            }
            _request.Proxy = _proxyAddr;
        }
        #endregion

        #region Response
        public string GetResponseLocation()
        {
            using (var response = this.GetResponse(null, false))
            {
                return response.Headers[HttpResponseHeader.Location];
            }
        }
        public HttpWebResponse GetResponseHead()
        {
            var response = GetResponse(HttpMethod.Head, true);
            Uri url = _request.RequestUri;
            NetworkCredential credential = (NetworkCredential)_request.Credentials;
            this.SetRequest(url, credential, false);
            return response;
        }

        public HttpWebResponse GetResponse(Action<HttpWebResponse> serverPush = null)
        {
            if (!_initRequest)
            {
                throw new WebException("Requires new request");
            }

            var response = this.GetResponse(null, true);
            if (serverPush != null)
            {
                serverPush(response);
            }
            return response;
        }

        #region Core
        protected virtual HttpWebResponse GetResponse(HttpMethod method, bool autoRedirect)
        {
            _initRequest = false;
            if (method == null)
            {
                method = _content.HasValue ? HttpMethod.Post : HttpMethod.Get;
            }
            _request.Method = method.Method;
            _request.AllowAutoRedirect = autoRedirect;
            var headers = _content.Headers;
            string referer = headers[HttpRequestHeader.Referer] ?? _referer;
            if (!string.IsNullOrEmpty(referer))
            {
                _request.Referer = referer;
                headers.Remove(HttpRequestHeader.Referer);
            }
            _content.AppendHeadersTo(_request.Headers);
            if (method == HttpMethod.Post)
            {
                if (_content.Files.Count > 0)
                {
                    var sb = new StringBuilder();
                    // http://www.w3.org/TR/html401/interact/forms.html#h-17.13.4
                    string boundary = DateTime.Now.Ticks.ToString("x");
                    byte[] beginBoundary = Encoding.ASCII.GetBytes(string.Format("--{0}{1}", boundary, E.NewLine)),
                        endBoundary = Encoding.ASCII.GetBytes(string.Format("--{0}--{1}", boundary, E.NewLine));
                    _request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

                    long contentLength = 0L;
                    var methodQueue = new Queue<Action<Stream>>();

                    for (int i = 0; i < _content.Form.Count; i++)
                    {
                        sb.Length = 0;
                        sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"{1}{1}", _content.Form.GetKey(i), E.NewLine);
                        sb.AppendFormat("{0}{1}", _content.Form.Get(i), E.NewLine);

                        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
                        contentLength += beginBoundary.LongLength + body.LongLength;
                        methodQueue.Enqueue(new Action<Stream>(stream =>
                        {
                            stream.Write(beginBoundary, 0, beginBoundary.Length);
                            stream.Write(body, 0, body.Length);
                        }));
                    }

                    byte[] endOfFile = Encoding.ASCII.GetBytes(E.NewLine);
                    foreach (var file in _content.Files)
                    {
                        sb.Length = 0;
                        sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", file.InputName, System.Web.HttpUtility.UrlEncode(file.FileName), E.NewLine);
                        sb.AppendFormat("Content-Type: {0}{1}{1}", file.ContentType, E.NewLine);

                        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
                        contentLength += beginBoundary.LongLength + body.LongLength + file.ContentLength + endOfFile.LongLength;
                        methodQueue.Enqueue(new Action<Stream>(stream =>
                        {
                            stream.Write(beginBoundary, 0, beginBoundary.Length);
                            stream.Write(body, 0, body.Length);
                            file.InputStream.FixedCopyTo(stream);
                            stream.Write(endOfFile, 0, endOfFile.Length);
                        }));
                    }

                    _request.ContentLength = contentLength + endBoundary.LongLength;
                    Stream requestStream = _request.GetRequestStream();
                    while (methodQueue.Count > 0)
                    {
                        methodQueue.Dequeue()(requestStream);
                    }
                    requestStream.Write(endBoundary, 0, endBoundary.Length);
                    requestStream.Close();
                }
                else
                {
                    _request.ContentType = "application/x-www-form-urlencoded";
                    if (_content.Form.Count > 0)
                    {
                        byte[] body = Encoding.UTF8.GetBytes(_content.GetFormString());
                        _request.ContentLength = body.LongLength;
                        Stream requestStream = _request.GetRequestStream();
                        requestStream.Write(body, 0, body.Length);
                        requestStream.Close();
                    }
                }
            }

            var response = (HttpWebResponse)_request.GetResponse();
            _referer = response.ResponseUri.AbsoluteUri;
            if (this.UseCookies)
            {
                _cookieContainer.Add(response.ResponseUri, response.Cookies);
            }
            _proxyAddr = null;
            if (_validateResponse != null && !_validateResponse(response))
            {
                throw new WebException(ValidateResponseFailure, null, WebExceptionStatus.UnknownError, response);
            }
            return response;
        }
        #endregion
        #endregion

        #region File
        public void DownloadFile(string savePath)
        {
            var file = new FileInfo(savePath);
            bool useRange = file.Exists && (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            if (useRange)
            {
                _request.AddRange(file.Length);
            }

            var response = this.GetResponse(null, true);
            long offset = useRange && response.StatusCode == (HttpStatusCode)206 ? file.Length : 0L,
                length = response.ContentLength;
            using (Stream responseStream = response.GetResponseStream())
            using (FileStream stream = useRange ? file.OpenWrite() : file.Create())
            {
                file.Attributes |= FileAttributes.Hidden;
                stream.Position = offset;
                responseStream.FixedCopyTo(stream, length, per =>
                {
                    stream.Flush();
                    return true;
                });
                file.Attributes = FileAttributes.Normal;
                file.Refresh();
            }
        }

        public string UploadFile(string filePath)
        {
            long offset = this.GetResponseHead().ContentLength;
            _request.AddRange(offset);
            _request.AllowWriteStreamBuffering = false;
            _content.Files.Add(new HttpFileContent(string.Empty, filePath, offset));
            var response = this.GetResponse();
            return response.GetResponseText();
        }
        #endregion

        #region IHttpClient
        ushort? IHttpClient.RetryCount { get; set; }
        TimeSpan? IHttpClient.RetryWaitDuration { get; set; }
        CookieContainer IHttpClient.CookieContainer
        {
            get { return _cookieContainer; }
        }
        string IHttpClient.SaveFileDirectory { get; set; }

        string IHttpClient.GetHtml(Uri requestUrl, HttpRequestContent content)
        {
            var client = (IHttpClient)this;
            string result = null;
            var waitDuration = client.RetryWaitDuration;
            App.Retry(() =>
            {
                this.SetRequest(requestUrl);
                if (content != null)
                {
                    this._content = content;
                }
                var res = this.GetResponse();
                result = res.GetResponseText();
            }, client.RetryCount.GetValueOrDefault(1), waitDuration.HasValue ? (int?)waitDuration.Value.TotalMilliseconds : null);
            return result;
        }

        Stream IHttpClient.GetStream(Uri requestUrl, HttpRequestContent content)
        {
            var client = (IHttpClient)this;
            Stream result = null;
            var waitDuration = client.RetryWaitDuration;
            App.Retry(() =>
            {
                this.SetRequest(requestUrl);
                if (content != null)
                {
                    this._content = content;
                }
                var res = this.GetResponse();
                result = res.GetResponseStream();
            }, client.RetryCount.GetValueOrDefault(1), waitDuration.HasValue ? (int?)waitDuration.Value.TotalMilliseconds : null);
            return result;
        }

        void IHttpClient.DownloadFile(Uri fileUrl, out string fileName)
        {
            var client = (IHttpClient)this;
            fileName = fileUrl.OriginalString;
            int i = fileName.LastIndexOf("?");
            if (i != -1)
            {
                fileName = fileName.Remove(i);
            }
            fileName = CryptoManaged.MD5Hex(fileUrl.OriginalString) + Path.GetExtension(fileName);
            string localPath = client.SaveFileDirectory + fileName;
            var waitDuration = client.RetryWaitDuration;
            try
            {
                if (!App.Retry(() =>
                {
                    this.SetRequest(fileUrl);
                    this.DownloadFile(localPath);
                    var file = new FileInfo(localPath);
                    return file.Exists && file.Length > 0L;
                }, client.RetryCount.GetValueOrDefault(1), waitDuration.HasValue ? (int?)waitDuration.Value.TotalMilliseconds : null))
                {
                    throw new DownloadException(string.Empty)
                    {
                        RemoteUrl = fileUrl,
                        LocalPath = localPath
                    };
                };
            }
            catch (Exception ex)
            {
                throw new DownloadException(string.Empty, ex)
                {
                    RemoteUrl = fileUrl,
                    LocalPath = localPath
                };
            }
        }
        #endregion
    }
}