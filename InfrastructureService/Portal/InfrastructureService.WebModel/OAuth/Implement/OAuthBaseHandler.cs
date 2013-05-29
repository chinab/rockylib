using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InfrastructureService.Client;
using InfrastructureService.WebModel.SSOService;

namespace InfrastructureService.WebModel
{
    internal abstract class OAuthBaseHandler : IOAuthHandler
    {
        protected OAuthEntity VerifiedEntity { get; set; }
        public virtual string ServerCallbackUrl { get; set; }
        public virtual string ReturnUrl { get; set; }
        public bool IsVerified { get; private set; }
        public abstract OAuthKind OAuthKind { get; }

        protected HttpContext CheckHttp()
        {
            var context = HttpContext.Current;
            if (context == null)
            {
                throw new OAuthException("无HTTP请求");
            }
            return context;
        }
        protected virtual string MapOpenID(string openID)
        {
            return openID ?? string.Empty;
        }
        protected SSOIdentity OAuth(string openID, string nickname, string userName = null, string password = null)
        {
            SSOIdentity id;
            try
            {
                var context = this.CheckHttp();
                id = SSOAuthentication.CurrentService.OAuth(new OAuthParameter()
                {
                    OAuthKind = this.OAuthKind,
                    OpenID = openID,
                    Nickname = nickname,
                    UserName = userName ?? MapOpenID(openID),
                    Password = password,
                    ClientIP = HttpContext.Current.Request.UserHostAddress,
                    Platform = SSOAuthentication.PlantForm
                });
            }
            catch (ClientCallException ex)
            {
                throw new OAuthException(ex.Message, ex);
            }
            if (this.VerifiedEntity == null || this.VerifiedEntity.OpenID != openID)
            {
                this.VerifiedEntity = new OAuthEntity()
                {
                    OpenID = openID,
                    Nickname = nickname,
                    IsBound = id != null,
                    Identity = id
                };
                this.IsVerified = true;
            }
            return id;
        }

        public abstract void RedirectToAuthorize();
        public abstract OAuthEntity VerifyAuth();

        public virtual SSOIdentity BindNewAccount()
        {
            return BindAccount(MapOpenID(this.VerifiedEntity.OpenID), Guid.NewGuid().ToString("N").Substring(0, 6));
        }
        public virtual SSOIdentity BindAccount(string userName, string password)
        {
            if (!this.IsVerified)
            {
                throw new OAuthException("回调未验证");
            }

            SSOIdentity id;
            try
            {
                id = OAuth(this.VerifiedEntity.OpenID, this.VerifiedEntity.Nickname, userName, password);
            }
            catch (Exception ex)
            {
                throw new OAuthException("绑定帐户失败", ex);
            }
            return id;
        }
    }
}