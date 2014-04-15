using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Web.SessionState;
using System.IO;

namespace System.Web
{
    public static class HttpUtils
    {
        #region Fields
        public static readonly string WebDomain;
        internal const string CookieIDName = "_CookieID";
        internal const string CookieValueName = "_CookieValue";
        private static readonly string CryptoKey;
        #endregion

        #region Properties
        public static string Referer
        {
            get
            {
                var context = HttpContext.Current;
                return context.Request.ServerVariables["HTTP_REFERER"];
            }
        }
        /// <summary>
        /// 通过代理服务器获取远程用户IP
        /// </summary>
        public static string RemoteIP
        {
            get
            {
                var context = HttpContext.Current;
                string ip;
                if (context.Request.ServerVariables["HTTP_VIA"] != null)
                {
                    ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    if (!string.IsNullOrEmpty(ip))
                    {
                        ip = ip.Split(',')[0];
                    }
                }
                else
                {
                    ip = context.Request.UserHostAddress;
                }
                return ip;
            }
        }
        public static SessionStateItemCollection CookieSafety
        {
            get
            {
                var context = HttpContext.Current;
                var col = context.Items[CookieValueName] as SessionStateItemCollection;
                if (col == null)
                {
                    var cookieValue = context.Request.Cookies[CookieValueName];
                    if (cookieValue == null)
                    {
                        col = new SessionStateItemCollection();
                    }
                    else
                    {
                        string id = CookieID.Value;
                        var crypto = new CryptoManaged(CryptoKey, id);
                        try
                        {
                            using (var stream = crypto.Decrypt(new MemoryStream(Convert.FromBase64String(cookieValue.Value))))
                            using (var bw = new BinaryReader(stream, Encoding.UTF8))
                            {
                                col = SessionStateItemCollection.Deserialize(bw);
                            }
                            if ((string)col[CookieIDName] != id)
                            {
                                col.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            Hub.LogError(ex, "CookieSafety");
                            col = new SessionStateItemCollection();
                        }
                    }
                    context.Items[CookieValueName] = col;
                }
                return col;
            }
        }
        internal static HttpCookie CookieID
        {
            get
            {
                var context = HttpContext.Current;
                var cookie = context.Request.Cookies[CookieIDName];
                if (cookie == null)
                {
                    cookie = new HttpCookie(CookieIDName);
                    cookie.HttpOnly = true;
                    cookie.Value = Guid.NewGuid().ToString("N");
                }
                else
                {
                    Guid value;
                    if (!Guid.TryParse(cookie.Value, out value))
                    {
                        cookie.Value = Guid.NewGuid().ToString("N");
                    }
                }
                return cookie;
            }
        }
        #endregion

        #region Methods
        static HttpUtils()
        {
            WebDomain = ConfigurationManager.AppSettings["WebDomain"];
            CryptoKey = ConfigurationManager.AppSettings["CryptoKey"];
            if (string.IsNullOrEmpty(CryptoKey))
            {
                CryptoKey = CryptoManaged.NewSalt;
            }
        }

        public static void AppendLog(string message)
        {
            var context = HttpContext.Current;
            string url = context.Request.Url.PathAndQuery, referer = context.Request.ServerVariables["HTTP_REFERER"];
            if (!string.IsNullOrEmpty(referer))
            {
                url += "[referer=" + referer + "]";
            }
            Hub.LogDebug("{0},{1}\t{2}\t{3}\t{4}", HttpUtils.RemoteIP, context.Request.UserAgent,
                context.Request.HttpMethod, url, message);
        }
        public static void AppendLog(Exception ex)
        {
            var context = HttpContext.Current;
            var msg = new StringBuilder(256);
            msg.Append(HttpUtils.RemoteIP).Append(',').Append(context.Request.UserAgent);
            msg.Append('\t').Append(context.Request.HttpMethod).Append('\t').Append(context.Request.Url.PathAndQuery);
            string referer = context.Request.ServerVariables["HTTP_REFERER"];
            if (!string.IsNullOrEmpty(referer))
            {
                msg.AppendFormat("[referer={0}]", referer);
            }
            Hub.LogError(ex, msg.ToString());
        }

        public static void ResponseNoCache()
        {
            var context = HttpContext.Current;
            context.Response.Buffer = true;
            context.Response.ExpiresAbsolute = DateTime.Now.AddDays(-1D);
            context.Response.Expires = 0;
            context.Response.CacheControl = "no-cache";
        }

        public static void ResponseStatusCode(int statusCode)
        {
            var context = HttpContext.Current;
            context.Response.StatusCode = statusCode;
            context.Response.SuppressContent = true;
            context.Response.End();
        }

        public static void ResponseFile(string filePath)
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                throw new FileNotFoundException(filePath);
            }
            var context = HttpContext.Current;
            context.Response.Clear();
            context.Response.Buffer = false;
            context.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(Encoding.UTF8.GetBytes(file.Name)));
            context.Response.AddHeader("Content-Length", file.Length.ToString());
            context.Response.ContentType = GetContentType(file.Extension);
            context.Response.WriteFile(filePath);
            context.Response.End();
        }
        public static void ResponseFile(string fileName, string content)
        {
            var context = HttpContext.Current;
            context.Response.Clear();
            context.Response.Buffer = false;
            context.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(Encoding.UTF8.GetBytes(fileName)));
            context.Response.AddHeader("Content-Length", Encoding.UTF8.GetByteCount(content).ToString());
            context.Response.ContentType = GetContentType(Path.GetExtension(fileName));
            context.Response.Write(content);
            context.Response.Flush();
            context.Response.End();
        }

        private static string GetContentType(string fileExt)
        {
            switch (fileExt)
            {
                case ".doc":
                    return "application/ms-word";  //"application/vnd.ms-word"
                case ".xls":
                    return "application/ms-excel";
                default:
                    return System.Net.Mime.MediaTypeNames.Application.Octet;
            }
        }
        #endregion

        #region Cookie
        public static void SetCookieSafety(HttpContext context, DateTime? expires = null)
        {
            var col = context.Items[CookieValueName] as SessionStateItemCollection;
            if (col == null || !col.Dirty)
            {
                return;
            }

            var cookieID = CookieID;
            cookieID.Value = Guid.NewGuid().ToString("N");
            SetCookie(context, cookieID);

            col[CookieIDName] = cookieID.Value;
            var crypto = new CryptoManaged(CryptoKey, cookieID.Value);
            using (var stream = new MemoryStream())
            using (var br = new BinaryWriter(stream, Encoding.UTF8))
            {
                col.Serialize(br);
                SetCookie(context, new HttpCookie(CookieValueName)
                {
                    HttpOnly = true,
                    Value = Convert.ToBase64String(crypto.Encrypt(stream).ToArray())
                }, expires: expires);
            }
        }

        public static void SetCookie(HttpContext context, HttpCookie cookie, bool doExpire = false, DateTime? expires = null)
        {
            if (string.IsNullOrEmpty(WebDomain))
            {
                cookie.Path = "/";
            }
            else
            {
                cookie.Domain = WebDomain;
            }
            if (doExpire)
            {
                cookie.Expires = DateTime.Now.AddDays(-2D);
            }
            else if (expires.HasValue)
            {
                cookie.Expires = expires.Value;
            }
            context.Response.AppendCookie(cookie);
        }
        #endregion
    }
}