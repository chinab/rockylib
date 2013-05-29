using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public enum AuthMobileKind
    {
        [EnumMember]
        SignUp = 0,
        [EnumMember]
        FindPassword = 1
    }
}