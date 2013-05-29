using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class AdminEntity : HeaderEntity
    {
        [DataMember]
        public Guid RowID { get; set; }

        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string UserName { get; set; }

        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string Password { get; set; }

        [DataMember]
        public String Email { get; set; }

        [DataMember]
        public String Mobile { get; set; }

        [DataMember]
        public string Remark { get; set; }

        [DataMember]
        public DateTime LastSignInDate { get; set; }

        [DataMember]
        public string LastSignInIP { get; set; }

        [DataMember]
        public StatusKind Status { get; set; }

        [DataMember]
        public Guid[] RoleIDs { get; set; }
    }
}