using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class SendAuthEmailParameter : HeaderEntity
    {
        [DataMember]
        public Guid UserID { get; set; }

        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string Email { get; set; }

        [DataMember]
        public AuthEmailKind Kind { get; set; }
    }
}