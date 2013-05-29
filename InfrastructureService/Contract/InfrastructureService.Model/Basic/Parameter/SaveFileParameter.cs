using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class SaveFileParameter : HeaderEntity
    {
        [DataMember]
        [NotNullValidator]
        [StringLengthValidator(50)]
        public string FileName { get; set; }

        [DataMember]
        [NotNullValidator]
        public byte[] FileData { get; set; }
    }
}