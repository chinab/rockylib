using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
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
        internal static readonly string HttpScheme = Uri.UriSchemeHttp + Uri.SchemeDelimiter;
        internal const string DefaultReferer = "http://www.google.com/#Timothy.net";
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
        private bool _hasNewRequest;
        private string _referer;
        private CookieContainer _cookieContainer;
        private HttpWebRequest _request;
        private HttpRequestEntity _entity;
        private Action<HttpWebRequest> _requestChanged;
        private Func<HttpWebResponse, bool> _validateResponse;
        #endregion

        #region Properties
        public bool KeepCookie
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
        /// <summary>
        /// 移除PersistentConnection为2的限制
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

        public WebHeaderCollection Headers
        {
            get { return _request.Headers; }
        }
        public NameValueCollection Form
        {
            get { return _entity.Form; }
        }
        public List<HttpFile> Files
        {
            get { return _entity.Files; }
        }
        public CookieCollection Cookies
        {
            get
            {
                Contract.Requires(this.KeepCookie);

                return _cookieContainer.GetCookies(_request.RequestUri);
            }
        }
        #endregion

        #region Constructors
        public HttpClient(Uri url = null, Action<HttpWebRequest> requestChanged = null, Func<HttpWebResponse, bool> validateResponse = null)
        {
            if (url == null)
            {
                url = new Uri(DefaultReferer);
            }

            _referer = DefaultReferer;
            _entity = new HttpRequestEntity();
            _requestChanged = requestChanged;
            _validateResponse = validateResponse;
            this.SetRequest(url);
        }
        #endregion

        #region Request
        public Uri BuildUri(string url, NameValueCollection queryString)
        {
            if (queryString.Count > 0)
            {
                url += _entity.GetFormString(queryString, url.Contains(HttpRequestEntity.Symbol_AndFirst));
            }
            return new Uri(url);
        }

        public virtual void SetRequest(Uri url, NetworkCredential credential = null, bool newRequest = true, string checksum = null, Stream rawStream = null)
        {
            _request = (HttpWebRequest)WebRequest.Create(url);
            _request.CookieContainer = _cookieContainer;
            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _request.Accept = "*/*";
            _request.UserAgent = DefaultUserAgent;
            _request.Referer = _referer;
            if (credential != null)
            {
                _request.SendChunked = false;
                _request.PreAuthenticate = true;
                _request.UseDefaultCredentials = false;
                _request.Credentials = credential;
            }
            if (newRequest)
            {
                _entity.Clear();
                _hasNewRequest = true;
            }
            if (_requestChanged != null)
            {
                _requestChanged(_request);
            }

            if (checksum != null)
            {
                _entity.Form["checksum"] = checksum;
            }
            if (rawStream != null)
            {
                _entity.Files.Add(new HttpFile("raw", "rawFile", rawStream));
            }
        }

        public virtual void SetProxy(EndPoint address, NetworkCredential credential = null)
        {
            var proxy = new WebProxy(string.Format("http://{0}", address));
            if (credential == null)
            {
                proxy.UseDefaultCredentials = true;
            }
            else
            {
                proxy.UseDefaultCredentials = false;
                proxy.Credentials = credential;
            }
            _request.Proxy = proxy;
        }
        #endregion

        #region Response
        #region Core
        protected virtual HttpWebResponse GetResponse(string httpMethod, bool autoRedirect)
        {
            _request.AllowAutoRedirect = autoRedirect;
            if (httpMethod == null)
            {
                _request.Method = _entity.HasValue ? WebRequestMethods.Http.Post : WebRequestMethods.Http.Get;
            }
            if (_request.Method == WebRequestMethods.Http.Post)
            {
                if (_entity.Files.Count > 0)
                {
                    var sb = new StringBuilder();
                    // http://www.w3.org/TR/html401/interact/forms.html#h-17.13.4
                    string boundary = DateTime.Now.Ticks.ToString("x");
                    byte[] beginBoundary = Encoding.ASCII.GetBytes(string.Format("--{0}{1}", boundary, E.NewLine)),
                        endBoundary = Encoding.ASCII.GetBytes(string.Format("--{0}--{1}", boundary, E.NewLine));
                    _request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

                    long contentLength = 0L;
                    var methodQueue = new Queue<Action<Stream>>();

                    for (int i = 0; i < _entity.Form.Count; i++)
                    {
                        sb.Length = 0;
                        sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"{1}{1}", _entity.Form.GetKey(i), E.NewLine);
                        sb.AppendFormat("{0}{1}", _entity.Form.Get(i), E.NewLine);

                        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
                        contentLength += beginBoundary.LongLength + body.LongLength;
                        methodQueue.Enqueue(new Action<Stream>(stream =>
                        {
                            stream.Write(beginBoundary, 0, beginBoundary.Length);
                            stream.Write(body, 0, body.Length);
                        }));
                    }

                    byte[] endOfFile = Encoding.ASCII.GetBytes(E.NewLine);
                    foreach (var file in _entity.Files)
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
                    if (_entity.Form.Count > 0)
                    {
                        byte[] body = Encoding.UTF8.GetBytes(_entity.GetFormString());
                        _request.ContentLength = body.LongLength;
                        Stream requestStream = _request.GetRequestStream();
                        requestStream.Write(body, 0, body.Length);
                        requestStream.Close();
                    }
                }
            }

            var response = (HttpWebResponse)_request.GetResponse();
            _referer = response.ResponseUri.AbsoluteUri;
            if (this.KeepCookie)
            {
                _cookieContainer.Add(response.ResponseUri, response.Cookies);
            }
            _hasNewRequest = false;
            if (_validateResponse != null && !_validateResponse(response))
            {
                throw new WebException(ValidateResponseFailure, null, WebExceptionStatus.UnknownError, response);
            }
            return response;
        }
        #endregion

        public string GetResponseLocation()
        {
            using (var response = this.GetResponse(null, false))
            {
                return response.Headers[HttpResponseHeader.Location];
            }
        }

        /// <summary>
        /// 通过WebRequestMethods.Http.Head获取HttpWebResponse
        /// </summary>
        /// <returns></returns>
        public HttpWebResponse GetResponseHead()
        {
            var response = GetResponse(WebRequestMethods.Http.Head, true);
            Uri url = _request.RequestUri;
            NetworkCredential credential = (NetworkCredential)_request.Credentials;
            this.SetRequest(url, credential, false);
            return response;
        }

        public HttpWebResponse GetResponse(Action<HttpWebResponse> serverPush = null)
        {
            if (!_hasNewRequest)
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

            var response = this.GetResponse(_request.Method, true);
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
            _entity.Files.Add(new HttpFile(string.Empty, filePath, offset));
            var res = this.GetResponse();
            return res.GetResponseText();
        }
        #endregion

        #region IHttpClient
        ushort? IHttpClient.RetryCount { get; set; }
        TimeSpan? IHttpClient.RetryWaitDuration { get; set; }
        string IHttpClient.SaveFileDirectory { get; set; }

        string IHttpClient.GetHtml(Uri url, NameValueCollection form)
        {
            var client = (IHttpClient)this;
            string result = null;
            var waitDuration = client.RetryWaitDuration;
            Hub.Retry(() =>
            {
                this.SetRequest(url);
                if (!form.IsNullOrEmpty())
                {
                    for (int i = 0; i < form.Count; i++)
                    {
                        this.Form[form.GetKey(i)] = form.Get(i);
                    }
                }
                var res = this.GetResponse();
                result = res.GetResponseText();
            }, client.RetryCount.GetValueOrDefault(1), waitDuration.HasValue ? (int?)waitDuration.Value.TotalMilliseconds : null);
            return result;
        }

        Stream IHttpClient.GetStream(Uri url, NameValueCollection form)
        {
            var client = (IHttpClient)this;
            Stream result = null;
            var waitDuration = client.RetryWaitDuration;
            Hub.Retry(() =>
            {
                this.SetRequest(url);
                if (!form.IsNullOrEmpty())
                {
                    for (int i = 0; i < form.Count; i++)
                    {
                        this.Form[form.GetKey(i)] = form.Get(i);
                    }
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
                if (!Hub.Retry(() =>
                {
                    this.SetRequest(fileUrl);
                    this.DownloadFile(localPath);
                    var file = new FileInfo(localPath);
                    return file.Exists && file.Length > 0L;
                }, client.RetryCount.GetValueOrDefault(1), waitDuration.HasValue ? (int?)waitDuration.Value.TotalMilliseconds : null))
                {
                    throw new DownloadException(string.Empty)
                    {
                        RemoteUrl = fileUrl.OriginalString,
                        LocalPath = localPath
                    };
                };
            }
            catch (Exception ex)
            {
                throw new DownloadException(string.Empty, ex)
                {
                    RemoteUrl = fileUrl.OriginalString,
                    LocalPath = localPath
                };
            }
        }
        #endregion
    }
}