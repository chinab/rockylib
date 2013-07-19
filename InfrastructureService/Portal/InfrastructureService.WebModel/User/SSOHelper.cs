using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Configuration;
using System.Web.SessionState;
using System.Threading;
using System.Reflection;
using System.Linq.Expressions;
using System.IO;
using Rocky;
using Newtonsoft.Json;

namespace InfrastructureService.WebModel
{
    /// <summary>
    /// Author: WangXiaoming
    ///   Date: 2012/12/12 10:10:10
    /// </summary>
    public static class SSOHelper
    {
        /// <summary>
        /// 常量参数名
        /// </summary>
        public class ParamsName
        {
            public const string Action = "Action";
            public const string Token = "Token";
            /// <summary>
            /// 回调URL参数名服务端回调地址
            /// </summary>
            public const string ServerCallbackUrl = "ServerCallbackUrl";
            /// <summary>
            /// 回调URL参数名 客户端跳转页面
            /// </summary>
            public const string ReturnUrl = "ReturnUrl";
        }

        #region Fields
        /// <summary>
        /// 校验参数名
        /// </summary>
        public const string AuthKeyName = "_AK", SessionIDName = "_SID";
        /// <summary>
        /// 回调URL参数名
        /// </summary>
        public const string ServerCallbackUrlName = "ServerCallbackUrl",
            ReturnUrlName = "ReturnUrl",
            TokenName = "Token";
        /// <summary>
        /// OAuth服务地址
        /// </summary>
        private static readonly string OAuthUrl;
        /// <summary>
        /// OAuth App回调URL
        /// </summary>
        private static readonly string ServerCallbackUrl;

        private static Func<string, object> CacheGetter;
        private static FieldInfo _sessionGetter;
        #endregion

        #region Properties
        /// <summary>
        /// 当前HttpContext中预留的string buffer，提供给GetOAuthUrl()方法生成URL
        /// </summary>
        private static StringBuilder CurrentBuffer
        {
            get
            {
                var context = CheckHttp();
                string key = "SSOHelper.CurrentBuffer";
                var buffer = context.Items[key] as StringBuilder;
                if (buffer == null)
                {
                    context.Items[key] = buffer = new StringBuilder();
                }
                return buffer;
            }
        }
        /// <summary>
        /// 当前HttpContext中的AuthKey，主要提供给GetOAuthUrl()方法生成URL
        /// </summary>
        private static string AuthKey
        {
            get
            {
                var context = CheckHttp();
                string key = "SSOHelper.AuthKey";
                string authKey = context.Items[key] as string;
                if (authKey == null)
                {
                    context.Items[key] = authKey = CreateAuthKey(context.Session.SessionID);
                }
                return authKey;
            }
        }
        #endregion

        #region Methods
        static SSOHelper()
        {
            OAuthUrl = ConfigurationManager.AppSettings["SSO_OAuthUrl"];
            ServerCallbackUrl = ConfigurationManager.AppSettings["SSO_OAuthServerCallbackUrl"];

            var rProp = typeof(HttpRuntime).GetProperty("CacheInternal", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
            var cacheInternal = rProp.GetValue(null, null);
            Type tString = typeof(string);
            var cacheGetter = cacheInternal.GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { tString }, null);
            var eParam = Expression.Parameter(tString, "Key");
            var eCall = Expression.Call(Expression.Constant(cacheInternal), cacheGetter, eParam);
            CacheGetter = Expression.Lambda<Func<string, object>>(eCall, eParam).Compile();
        }

        public static void SetSessionValue(string id, string key, object value)
        {
            SetSessionValue(id, new Dictionary<string, object>(1) { { key, value } });
        }
        /// <summary>
        /// 设置HttpSessionState
        /// </summary>
        /// <param name="id">SessionID</param>
        /// <param name="dict">键值集合</param>
        /// <returns>设置成功返回true，否则false</returns>
        public static void SetSessionValue(string id, IDictionary<string, object> dict)
        {
            var context = HttpContext.Current;
#if !DEBUG
            //设置的sid等于当前请求的sid
            if (context.Session != null && context.Session.SessionID == id)
            {
                foreach (var pair in dict)
                {
                    context.Session[pair.Key] = pair.Value;
                }
                return;
            }
#endif
            string cacheKey = "j" + id;
            var inProcSession = CacheGetter(cacheKey);
            //SessionStore未初始化
            if (inProcSession == null)
            {
                //这里InProcSessionStateStore方法中的HttpContext参数无意义，
                //所以如果HttpContext.Current为空随意传个空实例即可。
                if (context == null)
                {
                    string filePath = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.aspx").First();
                    context = new HttpContext(new HttpRequest(filePath, "http://www.guzhen.net", string.Empty), new HttpResponse(new StringWriter()));
                }
                var baseType = typeof(SessionStateStoreProviderBase);
                var type = Type.GetType(baseType.AssemblyQualifiedName.Replace(baseType.Name, "InProcSessionStateStore"), true);
                var stateStoreProvider = (SessionStateStoreProviderBase)Activator.CreateInstance(type);
                stateStoreProvider.Initialize(null, null);
                int timeout = context.Session == null ? 20 : context.Session.Timeout;
                stateStoreProvider.CreateUninitializedItem(context, id, timeout);
                inProcSession = CacheGetter(cacheKey);
            }

            if (_sessionGetter == null)
            {
                Interlocked.CompareExchange(ref _sessionGetter, inProcSession.GetType().GetField("_sessionItems", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField), null);
            }
            var session = (ISessionStateItemCollection)_sessionGetter.GetValue(inProcSession);
            //Session未初始化
            if (session == null)
            {
                _sessionGetter.SetValue(inProcSession, session = new SessionStateItemCollection());
            }
            foreach (var pair in dict)
            {
                session[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// 检查HttpContext并返回
        /// </summary>
        /// <returns></returns>
        internal static HttpContext CheckHttp()
        {
            var context = HttpContext.Current;
            if (context == null)
            {
                throw new HttpException("无HTTP请求");
            }
            return context;
        }

        /// <summary>
        /// 生成新AuthKey
        /// </summary>
        /// <param name="id">client的SessionID</param>
        /// <param name="host">一般情况为当前App的Host</param>
        /// <returns></returns>
        public static string CreateAuthKey(string id, string host = null)
        {
            if (host == null)
            {
                var context = CheckHttp();
                host = context.Request.ServerVariables["HTTP_HOST"];
            }
            return CryptoManaged.MD5Hex(string.Format("^{0}@{1}$", id, host));
        }

        /// <summary>
        /// 获取OAuthUrl
        /// </summary>
        /// <param name="kind">
        /// QQ = 0,
        /// Weibo = 1,
        /// Renren = 2,
        /// Alipay = 3,
        /// Ctrip = 4
        /// </param>
        /// <param name="returnUrl">
        /// 验证成功后返回的地址
        /// PS:returnUrl的Domain必须等于当前App的Domain，默认为当前请求URL
        /// </param>
        /// <returns></returns>
        public static string GetOAuthUrl(int kind, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(OAuthUrl))
            {
                throw new InvalidOperationException("请先配置SSO_OAuthUrl");
            }
            if (string.IsNullOrEmpty(ServerCallbackUrl))
            {
                throw new InvalidOperationException("请先配置SSO_OAuthServerCallbackUrl");
            }

            var context = CheckHttp();
            if (context.Session != null)
            {
                context.Session[string.Empty] = null;
            }
            if (returnUrl == null)
            {
                var currentUrl = context.Request.Url;
                returnUrl = currentUrl.Scheme + Uri.SchemeDelimiter + currentUrl.Authority + currentUrl.PathAndQuery;
            }
            var sb = CurrentBuffer;
            //append ServerCallbackUrl
            sb.Length = 0;
            string callbackUrl = ServerCallbackUrl, sAnd = callbackUrl.Contains("?") ? "&" : "?";
            sb.Append(callbackUrl).AppendFormat("{0}{1}={2}&{3}={4}", sAnd, SessionIDName, context.Session.SessionID, AuthKeyName, AuthKey);
            callbackUrl = sb.ToString();
            //append OAuthUrl
            sb.Length = 0;
            sAnd = OAuthUrl.Contains("?") ? "&" : "?";
            sb.Append(OAuthUrl).AppendFormat("{0}handler={1}&{2}={3}&{4}={5}", sAnd, kind,
                ServerCallbackUrlName, HttpUtility.UrlEncode(callbackUrl),
                ReturnUrlName, HttpUtility.UrlEncode(returnUrl));
            return sb.ToString();
        }

        /// <summary>
        /// 检验OAuth App回调是否合法
        /// </summary>
        /// <param name="id">client的SessionID</param>
        public static void CheckOAuthResponse(out string id)
        {
            var context = CheckHttp();
            id = context.Request.QueryString[SessionIDName];
            string key = context.Request.QueryString[AuthKeyName],
                svcKey = context.Request.Headers[AuthKeyName],
                currentKey = CreateAuthKey(id),
                currentSvcKey = CreateAuthKey(id, JsonConvert.SerializeObject(context.Request.Form));
            if (key != currentKey || svcKey != currentSvcKey)
            {
                throw new InvalidOperationException("OAuth AppCallbak非法");
            }
        }
        #endregion
    }
}