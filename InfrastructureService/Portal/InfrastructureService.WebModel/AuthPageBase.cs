//#define Session
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using InfrastructureService.Client;
using InfrastructureService.WebModel.SSOService;
using Rocky.Web;

namespace InfrastructureService.WebModel
{
    /// <summary>
    /// SSO客户站点Page基类，实现SSO验证
    /// </summary>
    public class AuthPageBase : System.Web.UI.Page
    {
        #region Properties
        /// <summary>
        /// 页面不需要登录也可以访问
        /// </summary>
        public virtual bool CanAnonymousAccess { get; set; }
        /// <summary>
        /// 返回地址，默认为当前页面
        /// </summary>
        public string ReturnUrl
        {
            get { return (string)ViewState["ReturnUrl"]; }
            set { ViewState["ReturnUrl"] = value; }
        }
        /// <summary>
        /// 是否启用ReturnUrl跳转
        /// </summary>
        public bool EnableRedirect
        {
            get { return ViewState["EnableRedirect"] == null ? true : Convert.ToBoolean(ViewState["EnableRedirect"]); }
            set { ViewState["EnableRedirect"] = value; }
        }
        /// <summary>
        /// 返回用户是否已经登录
        /// </summary>
        public bool IsSignIn
        {
            get
            {
                var principal = Page.User as SSOPrincipal;
                if (principal == null)
                {
                    return false;
                }
                return principal.Identity.IsAuthenticated;
            }
        }
        /// <summary>
        /// 返回用户登录身份信息 包含用户名，用户主键，用户email等常用信息
        /// </summary>
        protected SSOIdentity UserIdentity
        {
            get
            {
                var principal = Page.User as SSOPrincipal;
                if (principal == null)
                {
                    Redirect2LoginPage();
                }
                return principal.Identity;
            }
            set { SSOAuthentication.SetContextPrincipal(value); }
        }
        #endregion

        #region Methods
        protected override void OnPreInit(EventArgs e)
        {
            this.CanAnonymousAccess = true;

            string returnUrl = Request.QueryString[SSOHelper.ParamsName.ReturnUrl];
            if (!string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = HttpUtility.UrlDecode(returnUrl);
            }
            else
            {
                returnUrl = Request.Url.AbsoluteUri;
            }
            this.ReturnUrl = returnUrl;

            base.OnPreInit(e);
        }

        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            if (!Page.IsPostBack)
            {
                Verify();
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Page.IsPostBack)
            {
                Verify();
            }
        }
        private void Verify()
        {
            //当前站点没有登录则使用sso验证
            if (!this.CanAnonymousAccess)
            {
                //如果当前会话中没有发现登录用户，或者当前登录用户与登录到SSO站点的用户不同
                var token = HttpUtils.CookieSafety["token"] as string;
                if (!this.IsSignIn || string.IsNullOrEmpty(token)
                    || UserIdentity.Token != token)
                {
                    Redirect2LoginPage();
                }
            }
        }

        /// <summary>
        /// 登录方法 请重载OnPreLoad方法
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password">源密码</param>
        protected void SignIn(string userName, string password)
        {
            var identity = SSOAuthentication.CurrentService.SignIn(new SignInParameter()
            {
                AppID = SSOAuthentication.AppID,
                ClientIP = Request.UserHostAddress,
                Platform = SSOAuthentication.PlantForm,
                UserName = userName,
                Password = password
            });
            if (!identity.IsAuthenticated)
            {
                throw new ClientCallException("账户或密码错误！");
            }
            this.UserIdentity = identity;
            NotifyDomains(false, identity, this.ReturnUrl);

            HttpUtils.CookieSafety["token"] = identity.Token;
            DateTime expires = DateTime.Now.AddMinutes(30D);
            HttpUtils.CookieSafety["expires"] = expires;
            HttpUtils.SetCookieSafety(base.Context, expires);
        }

        /// <summary>
        /// 注销登录
        /// </summary>
        protected void SignOut()
        {
            SSOIdentity user = this.UserIdentity;
            if (user == null)
            {
                return;
            }
            SSOAuthentication.CurrentService.SignOut(user.Token);
            NotifyDomains(true, user, this.ReturnUrl);

            HttpUtils.CookieSafety.Remove("token");
        }

        private void NotifyDomains(bool isOut, SSOIdentity id, string returnUrl)
        {
            string action = Convert.ToUInt32(isOut).ToString();
            foreach (var domain in SSOAuthentication.CurrentService.GetServerConfig().Notify)
            {
                ClientScript.RegisterClientScriptInclude(domain.Domain,
                    string.Format("{0}?{1}={2}&{3}={4}", domain.ClientHandlerUrl, SSOHelper.ParamsName.Action, action, SSOHelper.ParamsName.Token, id.Token));
            }
            if (this.EnableRedirect && !string.IsNullOrEmpty(returnUrl))
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "SSO", string.Format("location.href='{0}';", returnUrl), true);
            }
        }

        private const string NotifyScriptFormat = @"var UUNetSSO = {
            _Urls: [|0|],
            Action: { SignIn: 0, SignOut: 1 },
            Notify: function (Action, Token) {
                var func = function (url) {
                    var script = document.createElement('script');
                    script.type = 'text/javascript';
                    script.src = url;
                    document.body.appendChild(script);
                };
                for (var i = 0; i < UUNetSSO._Urls.length; i++) {
                    func(UUNetSSO._Urls[i].replace('|a|', Action).replace('|t|', Token));
                }
            }
        }";
        public void RegisterNotifyScript()
        {
            var entry = SSOAuthentication.CurrentService.GetServerConfig().Notify;
            if (entry.Length == 0)
            {
                return;
            }
            StringBuilder script = new StringBuilder();
            foreach (var domain in entry)
            {
                script.Append(string.Format("'{0}?{1}=|a|&{2}=|t|'", domain.ClientHandlerUrl, SSOHelper.ParamsName.Action, SSOHelper.ParamsName.Token)).Append(",");
            }
            script.Length--;
            ClientScript.RegisterClientScriptBlock(this.GetType(), "RegisterNotifyScript", NotifyScriptFormat.Replace("|0|", script.ToString()), true);
        }

        public void Redirect2LoginPage()
        {
            Redirect2LoginPage(null);
        }
        public void Redirect2LoginPage(string returnUrl)
        {
            string script = string.Empty, url = SSOAuthentication.LoginUrl;
            if (string.IsNullOrEmpty(returnUrl))
            {
                script = string.Format("<script>var url='{0}';url+=(url.indexOf('?')==-1?'?':'&')+'{1}='+encodeURIComponent(top.location.href);top.location.href=url;</script>", url, SSOHelper.ParamsName.ReturnUrl);
            }
            else
            {
                bool hasRet = url.Contains(SSOHelper.ParamsName.ReturnUrl);
                if (!hasRet)
                {
                    url += (url.Contains("?") ? "&" : "?") + SSOHelper.ParamsName.ReturnUrl + "=" + HttpUtility.UrlEncode(returnUrl);
                }
                script = string.Format("<script>top.location.href='{0}';</script>", url);
            }
            Response.ClearContent();
            Response.Write(script);
            Response.End();
        }
        #endregion

        #region Fields
        private StringBuilder _scriptBlock;
        #endregion

        #region Properties
        private StringBuilder ScriptBlock
        {
            get
            {
                if (_scriptBlock == null)
                {
                    _scriptBlock = new StringBuilder();
                }
                return _scriptBlock;
            }
        }
        #endregion

        #region Fake MVVM
        public virtual void SetModel<T>(T view, bool asState = false) where T : class
        {
            SetModel(view, asState ? vo =>
            {
#if Session
                var dict = (ListDictionary)Session["ModelState"];
                if (dict == null)
                {
                    Session["ModelState"] = dict = new ListDictionary();
                }
                dict[vo.GetType()] = vo;
#else
                var dict = (ListDictionary)ViewState["ModelState"];
                if (dict == null)
                {
                    ViewState["ModelState"] = dict = new ListDictionary();
                }
                dict[vo.GetType()] = vo;
#endif
            } : (Action<T>)null);
        }
        public virtual void SetModel<T>(T view, Action<T> stateMapper) where T : class
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            PageUtils.SetPost(view, this);
            if (stateMapper != null)
            {
                stateMapper(view);
            }
        }

        public virtual T GetModel<T>(bool asState = false) where T : class, new()
        {
            return GetModel<T>(asState ? () =>
            {
#if Session
                var dict = (ListDictionary)Session["ModelState"];
                return dict != null ? ((T)dict[typeof(T)] ?? new T()) : new T();
#else
                var dict = (ListDictionary)ViewState["ModelState"];
                return dict != null ? ((T)dict[typeof(T)] ?? new T()) : new T();
#endif
            } : (Func<T>)null);
        }
        public virtual T GetModel<T>(Func<T> stateMapper) where T : class, new()
        {
            T view;
            if (stateMapper != null)
            {
                view = stateMapper();
                if (view == null)
                {
                    throw new ArgumentException("stateMapper");
                }
            }
            else
            {
                view = new T();
            }
            PageUtils.GetPost(view, this);
            return view;
        }
        #endregion

        #region Methods
        public void RegisterHeader(string keywords, string description)
        {
            Page.MetaKeywords += keywords;
            Page.MetaDescription += description;
        }

        public void RegisterScript(string js)
        {
            this.ScriptBlock.Append(js);
        }

        /// <summary>
        /// 弹出信息
        /// </summary>
        /// <param name="msg"></param>
        public void Alert(string msg, string url = null)
        {
            if (url == null)
            {
                this.ScriptBlock.Append(string.Format("MsgBox.alert(\"{0}\");", msg));
            }
            else
            {
                this.ScriptBlock.Append(string.Format("MsgBox.alertto(\"{0}\",'{1}');", msg, url));
            }
        }
        /// <summary>
        /// 弹出层框架
        /// </summary>
        /// <param name="url">框架页面url</param>
        /// <param name="title">弹出层标题</param>
        ///   /// <param name="width">弹出层宽度</param>
        /// <param name="height">弹出层高度</param>
        public void Iframe(string url, string title, int width, int height)
        {
            this.ScriptBlock.Append(string.Format("MsgBox.iframe('{0}','{1}',{2},{3});", url, title, width, height));
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (_scriptBlock != null)
            {
                PageUtils.RegisterScript("window.onload=function(){" + _scriptBlock.ToString() + "};");
            }
            base.Render(writer);
        }
        #endregion
    }
}