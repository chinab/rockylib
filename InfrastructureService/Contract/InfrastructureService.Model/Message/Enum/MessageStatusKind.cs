using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Message
{
    [DataContract]
    public enum MessageStatusKind
    {
        [EnumMember]
        Unsent = 0,
        [EnumMember]
        OK = 1,
        [EnumMember]
        Cancelled = 2,
        [EnumMember]
        Error = 3
    }
}