using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class IsUserNameExistsParameter : HeaderEntity
    {
        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string UserName { get; set; }
    }
}