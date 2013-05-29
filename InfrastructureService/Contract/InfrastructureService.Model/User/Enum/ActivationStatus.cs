using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public enum ActivationStatus
    {
        [EnumMember(Value = "未激活")]
        NotActive = 0,
        [EnumMember(Value = "已激活")]
        Activated = 1,
    }
}