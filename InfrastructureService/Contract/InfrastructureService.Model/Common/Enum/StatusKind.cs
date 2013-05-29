using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model
{
    [DataContract]
    public enum StatusKind
    {
        [EnumMember(Value = "已锁定")]
        Locked = -2,
        [EnumMember(Value = "已屏蔽")]
        Blocked = -1,
        [EnumMember(Value = "未审核")]
        Default = 0,
        [EnumMember(Value = "已审核")]
        Audited = 1
    }
}