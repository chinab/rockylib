using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public enum SignUpErrorCode
    {
        [EnumMember(Value = "服务器忙")]
        ServerBusy = 0,
        [EnumMember(Value = "邮箱格式不正确")]
        EmailFormatError = 1,
        [EnumMember(Value = "手机格式不正确")]
        MobileFormatError = 2,
        [EnumMember(Value = "账户已被注册")]
        AccountExist = 3
    }
}