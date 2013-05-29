using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public enum FeedbackKind
    {
        [EnumMember(Value = "建议意见")]
        Suggestion = 0,
        [EnumMember(Value = "服务投诉")]
        Complaint = 1,
        [EnumMember(Value = "操作帮助")]
        Operation = 2,
        [EnumMember(Value = "合作意向")]
        Cooperation = 3,
        [EnumMember(Value = "其他信息")]
        Other = 4
    }
}