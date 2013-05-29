using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using InfrastructureService.WebModel.SSOService;
using QConnectSDK;
using QConnectSDK.Context;

namespace InfrastructureService.WebModel
{
    internal class QQHandler : OAuthBaseHandler
    {
        private string _state;

        public override OAuthKind OAuthKind
        {
            get { return OAuthKind.QQ; }
        }

        public override void RedirectToAuthorize()
        {
            var context = base.CheckHttp();
            var qContext = new QzoneContext();
            _state = Guid.NewGuid().ToString("N");
            string scope = "get_user_info,add_share,list_album,upload_pic,check_page_fans,add_t,add_pic_t,del_t,get_repost_list,get_info,get_other_info,get_fanslist,get_idolist,add_idol,del_idol,add_one_blog,add_topic,get_tenpay_addr";
            var authenticationUrl = qContext.GetAuthorizationUrl(_state, scope);
            //request token, request token secret 需要保存起来 
            context.Response.Redirect(authenticationUrl);
        }

        public override OAuthEntity VerifyAuth()
        {
            if (_state == null)
            {
                throw new OAuthException("错误回调");
            }

            var context = base.CheckHttp();
            string code = context.Request.Params["code"];
            var qzone = new QOpenClient(code, _state);
            QConnectSDK.Models.User currentUser = qzone.GetCurrentUser();
            if (currentUser == null)
            {
                throw new OAuthException("QQ用户不存在");
            }

            base.OAuth(qzone.OAuthToken.OpenId, currentUser.Nickname);
            return base.VerifiedEntity;
        }
    }
}