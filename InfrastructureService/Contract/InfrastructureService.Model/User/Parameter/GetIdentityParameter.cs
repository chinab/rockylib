using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class GetIdentityParameter
    {
        [DataMember]
        public string Token { get; set; }
        [DataMember]
        public DateTime? ExpiresDate { get; set; }
    }
}