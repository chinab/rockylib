using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Message
{
    [DataContract]
    public class SendSMSParameter : HeaderEntity
    {
        [DataMember]
        public Guid? ConfigID { get; set; }

        [DataMember]
        [StringLengthValidator(13, 13)]
        public string ReceiveMobile { get; set; }

        [DataMember]
        [NotNullValidator]
        public string SendMessage { get; set; }
    }
}