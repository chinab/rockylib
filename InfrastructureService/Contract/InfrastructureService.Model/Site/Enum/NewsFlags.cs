using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    [Flags]
    public enum NewsFlags
    {
        [EnumMember]
        None = 0,
        [EnumMember(Value = "推荐新闻")]
        Recommend = 1 << 0,
        [EnumMember(Value = "热门新闻")]
        Hot = 1 << 1
    }
}