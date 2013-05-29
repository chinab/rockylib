using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.User
{
    /// <summary>
    /// 第3方登录类别
    /// </summary>
    [DataContract]
    public enum OAuthKind
    {
        [EnumMember]
        QQ = 0,
        [EnumMember]
        Weibo = 1
    }
}