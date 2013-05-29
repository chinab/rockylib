using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class GetServerConfigResult
    {
        [DataMember]
        public SSONotify[] Notify { get; set; }
        [DataMember]
        public int? Timeout { get; set; }
    }
}