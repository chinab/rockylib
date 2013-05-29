using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class SendFindPwdCodeParameter : HeaderEntity
    {
        [DataMember]
        public string EmailOrMobile { get; set; }
    }
}