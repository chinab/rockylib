using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class OAuthParameter : SignInParameter
    {
        /// <summary>
        /// 第3方OpenID（唯一）
        /// </summary>
        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string OpenID { get; set; }

        /// <summary>
        /// 第3方昵称
        /// </summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>
        /// 第3方类型
        /// </summary>
        [DataMember]
        public OAuthKind OAuthKind { get; set; }
    }
}