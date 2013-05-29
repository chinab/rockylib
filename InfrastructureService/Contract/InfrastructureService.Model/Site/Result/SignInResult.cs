using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class SignInResult
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public AdminEntity User { get; set; }
    }
}