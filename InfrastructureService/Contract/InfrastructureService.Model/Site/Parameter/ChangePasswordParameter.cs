using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class ChangePasswordParameter : HeaderEntity
    {
        [DataMember]
        public string UserName { set; get; }
        [DataMember]
        public string OldPassword { get; set; }
        [DataMember]
        public string NewPassword { set; get; }
    }
}