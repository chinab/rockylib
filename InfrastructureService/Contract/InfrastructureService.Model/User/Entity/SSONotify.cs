using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class SSONotify
    {
        [DataMember]
        public string Domain { get; set; }
        [DataMember]
        public string ClientHandlerUrl { get; set; }
    }
}