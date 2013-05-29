using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class ChangePasswordParameter : HeaderEntity
    {
        [DataMember]
        public string AuthCode { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public string OldPassword { get; set; }

        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(32)]
        public string NewPassword { get; set; }
    }
}