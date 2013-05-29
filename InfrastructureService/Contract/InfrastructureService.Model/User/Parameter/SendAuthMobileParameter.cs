using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class SendAuthMobileParameter : HeaderEntity
    {
        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string Mobile { get; set; }

        [DataMember]
        public AuthMobileKind Kind { get; set; }
    }
}