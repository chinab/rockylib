using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class SignInParameter : HeaderEntity
    {
        [DataMember]
        [NotNullValidator]
        public string UserName { get; set; }

        [DataMember]
        [NotNullValidator]
        public string Password { get; set; }

        [DataMember]
        [NotNullValidator]
        public string LastLoginIP { get; set; }
    }
}