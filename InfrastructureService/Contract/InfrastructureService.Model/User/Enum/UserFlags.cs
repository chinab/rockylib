using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.User
{
    [DataContract]
    [Flags]
    public enum UserFlags
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        AuthenticEmail = 1 << 0,
        [EnumMember]
        AuthenticMobile = 1 << 1,
        [EnumMember]
        Blocked = 1 << 2,
        [EnumMember]
        All = AuthenticEmail | AuthenticMobile | Blocked
    }
}