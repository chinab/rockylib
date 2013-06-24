using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using InfrastructureService.WebModel.SSOService;
using Rocky;
using Rocky.Web;

namespace InfrastructureService.WebModel
{
    public sealed partial class SSOAuthentication : IHttpModule, IHttpHandler, IRequiresSessionState
    {
        public static readonly Guid AppID;
        public static readonly string LoginUrl, PlantForm;

        static SSOAuthentication()
        {
            AppID = Guid.Parse(ConfigurationManager.AppSettings["AppID"]);
            string domain = HttpUtils.WebDomain;
            if (string.IsNullOrEmpty(domain))
            {
                throw new InvalidOperationException("Domain entry not found in appSettings section of Web.config");
            }

            LoginUrl = ConfigurationManager.AppSettings["SSOAuthentication.LoginUrl"];
            if (string.IsNullOrEmpty(LoginUrl))
            {
                throw new InvalidOperationException("LoginUrl entry not found in appSettings section of Web.config");
            }

            PlantForm = ConfigurationManager.AppSettings["SSOAuthentication.SitePlant"];
        }

        /// <summary>
        /// 设置当前HTTP上下文的用户信息
        /// </summary>
        /// <param name="identity"></param>
        public static void SetContextPrincipal(SSOIdentity identity)
        {
            var context = SSOHelper.CheckHttp();
            var principal = new SSOPrincipal(identity);
            context.User = principal;

            HttpUtils.CookieSafety["token"] = identity.Token;
            DateTime? expires = null;
            var expiration = CurrentService.GetServerConfig().Timeout;
            if (expiration.HasValue)
            {
                expires = DateTime.Now.AddMinutes(expiration.Value);
            }
            HttpUtils.SetCookieSafety(context, expires);
        }

        #region IHttpModule
        private static readonly object ServiceKey = new object();

        public static UserServiceClient CurrentService
        {
            get
            {
                var context = HttpContext.Current;
                var client = (UserServiceClient)context.Items[ServiceKey];
                if (client == null)
                {
                    context.Items.Add(ServiceKey, client = new UserServiceClient());
                }
                return client;
            }
        }
        public static SSOPrincipal CurrentPrincipal
        {
            get
            {
                var context = HttpContext.Current;
                return context.User as SSOPrincipal;
            }
        }

        void IHttpModule.Init(HttpApplication context)
        {
            context.AuthenticateRequest += new EventHandler(context_AuthenticateRequest);
            context.EndRequest += new EventHandler(context_EndRequest);
        }

        void context_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpApplication context = (HttpApplication)sender;
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            string token = (string)HttpUtils.CookieSafety["token"];
            if (!string.IsNullOrEmpty(token))
            {
                var expires = (DateTime?)HttpUtils.CookieSafety["expires"];
                SSOIdentity identity = null;
                try
                {
                    //WCF
                    identity = CurrentService.GetIdentity(new GetIdentityParameter()
                    {
                        Token = token,
                        ExpiresDate = expires
                    });
                }
                catch (Exception ex)
                {
                    Runtime.LogError(ex, "AuthenticateRequest");
#if DEBUG
                    throw;
#endif
                }
                if (identity != null)
                {
                    SetContextPrincipal(identity);
                }
            }
        }

        void context_EndRequest(object sender, EventArgs e)
        {
            HttpApplication context = (HttpApplication)sender;

            var client = (UserServiceClient)context.Context.Items[ServiceKey];
            if (client != null)
            {
                client.Close();
            }
        }

        void IHttpModule.Dispose()
        {

        }
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
            //CP="NOI ADM DEV PSAi COM NAV OUR OTR STP IND DEM"
            Response.AppendHeader("P3P", "CP=IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT");

            string sid = Request.Form[SSOHelper.SessionIDName], action, token;
            //服务器回调
            if (!string.IsNullOrEmpty(sid))
            {
                action = Request.Form[SSOHelper.ParamsName.Action];
                token = Request.Form[SSOHelper.ParamsName.Token];
                var dict = new Dictionary<string, object>(2);
                dict.Add(SSOHelper.ParamsName.Action, action);
                dict.Add(SSOHelper.ParamsName.Token, token);
                SSOHelper.SetSessionValue(sid, dict);
                Response.Write("1");
                Response.End();
            }
            //客户端推送
            action = Request.QueryString[SSOHelper.ParamsName.Action] ?? (string)context.Session[SSOHelper.ParamsName.Action];
            token = Request.QueryString[SSOHelper.ParamsName.Token] ?? (string)context.Session[SSOHelper.ParamsName.Token];
            switch (action)
            {
                case "0":
                    HttpUtils.CookieSafety["token"] = token;
                    HttpUtils.SetCookieSafety(context);
                    break;
                case "1":
                    DateTime expires = DateTime.Now.AddDays(-2D);
                    HttpUtils.SetCookieSafety(context, expires);
                    break;
            }
        }
        #endregion
    }
}