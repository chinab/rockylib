using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class SignInParameter : HeaderEntity
    {
        /// <summary>
        /// 用户名，用于绑定现有帐号
        /// </summary>
        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string UserName { get; set; }

        /// <summary>
        /// 用户密码
        /// </summary>
        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(32)]
        public string Password { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        [DataMember]
        public string ClientIP { get; set; }

        /// <summary>
        /// 请求App名
        /// </summary>
        [DataMember]
        public string Platform { get; set; }

        [DataMember]
        public bool LogSignIn { get; set; }
    }
}