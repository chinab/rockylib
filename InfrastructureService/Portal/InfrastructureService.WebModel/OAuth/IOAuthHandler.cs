using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using InfrastructureService.WebModel.SSOService;

namespace InfrastructureService.WebModel
{
    public interface IOAuthHandler
    {
        /// <summary>
        /// App服务端回调地址
        /// </summary>
        string ServerCallbackUrl { get; set; }
        /// <summary>
        /// ServerCallbackUrl处理完毕后跳转的Url
        /// </summary>
        string ReturnUrl { get; set; }
        /// <summary>
        /// 是否调用过VerifyAuth()
        /// </summary>
        bool IsVerified { get; }
        /// <summary>
        /// 跳转至第3方处理
        /// </summary>
        void RedirectToAuthorize();
        /// <summary>
        /// 验证第3方处理callback
        /// </summary>
        /// <returns>验证实体</returns>
        OAuthEntity VerifyAuth();
        /// <summary>
        /// 绑定到新帐户
        /// </summary>
        /// <returns></returns>
        SSOIdentity BindNewAccount();
        /// <summary>
        /// 绑定到现有帐户
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        SSOIdentity BindAccount(string userName, string password);
    }

    public class OAuthEntity
    {
        /// <summary>
        /// OpenID
        /// </summary>
        public string OpenID { get; set; }
        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 是否绑定过
        /// </summary>
        public bool IsBound { get; set; }
        public SSOIdentity Identity { get; set; }
    }
}