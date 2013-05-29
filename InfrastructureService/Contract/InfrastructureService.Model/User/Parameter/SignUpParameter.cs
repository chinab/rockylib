using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class SignUpParameter : HeaderEntity
    {
        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string UserName { get; set; }

        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(32)]
        public string Password { get; set; }

        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Mobile { get; set; }
        [DataMember]
        public int SmsCode { get; set; }
    }
}