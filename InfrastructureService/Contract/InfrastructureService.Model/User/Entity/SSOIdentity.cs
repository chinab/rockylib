using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    /// <summary>
    /// 表示用户身份信息
    /// </summary>
    [DataContract]
    public sealed class SSOIdentity : IIdentity
    {
        #region 实现IIdentity接口
        /// <summary>
        /// 获取验证类型
        /// </summary>
        public string AuthenticationType
        {
            get { return "InfrastructureService.SSO"; }
        }

        /// <summary>
        /// 标明用户是否经过验证
        /// </summary>
        [DataMember]
        public bool IsAuthenticated { get; set; }

        string IIdentity.Name
        {
            get { return this.UserName; }
        }
        #endregion

        /// <summary>
        /// 用户ID
        /// </summary>
        [DataMember]
        public Guid UserID { get; set; }
        /// <summary>
        /// 登录名
        /// </summary>
        [DataMember]
        public string UserName { get; set; }
        /// <summary>
        /// 登录令牌
        /// </summary>
        [DataMember]
        public string Token { get; set; }
        /// <summary>
        /// 会话ID
        /// </summary>
        [DataMember]
        public string SessionID { get; set; }
        /// <summary>
        /// 令牌颁发时间
        /// </summary>
        [DataMember]
        public DateTime IssueDate { get; set; }
    }
}