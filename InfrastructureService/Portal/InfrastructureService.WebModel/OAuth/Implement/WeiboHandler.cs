using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using NetDimension.Weibo;
using InfrastructureService.WebModel.SSOService;

namespace InfrastructureService.WebModel
{
    internal class WeiboHandler : OAuthBaseHandler
    {
        private OAuth _oauth;

        public override OAuthKind OAuthKind
        {
            get { return OAuthKind.Weibo; }
        }

        protected override string MapOpenID(string openID)
        {
            return string.Format("Sina_{0}", base.MapOpenID(openID));
        }

        public override void RedirectToAuthorize()
        {
            var context = base.CheckHttp();
            _oauth = new OAuth(ConfigurationManager.AppSettings["WeiboAppKey"], ConfigurationManager.AppSettings["WeiboSecret"], ConfigurationManager.AppSettings["WeiboCallbackUrl"]);
            context.Response.Redirect(_oauth.GetAuthorizeURL());
        }

        public override OAuthEntity VerifyAuth()
        {
            if (_oauth == null)
            {
                throw new OAuthException("错误回调");
            }

            var context = base.CheckHttp();
            string code = context.Request.Params["code"];
            var accessToken = _oauth.GetAccessTokenByAuthorizationCode(code);
            if (string.IsNullOrEmpty(accessToken.Token))
            {
                throw new OAuthException("Weibo用户不存在");
            }

            var Sina = new NetDimension.Weibo.Client(_oauth);
            var uid = Sina.API.Entity.Account.GetUID(); //调用API中获取UID的方法
            var currentUser = Sina.API.Entity.Users.Show(uid);

            OAuth(accessToken.UID, currentUser.ScreenName);
            return base.VerifiedEntity;
        }
    }
}