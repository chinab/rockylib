using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Message
{
    [DataContract]
    public class SendEmailParameter : HeaderEntity
    {
        [DataMember]
        public Guid? ConfigID { get; set; }

        [DataMember]
        [NotNullValidator]
        public string[] Recipients { get; set; }

        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string Body { get; set; }
    }
}