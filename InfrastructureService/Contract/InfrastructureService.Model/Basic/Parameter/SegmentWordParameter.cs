using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class SegmentWordParameter : HeaderEntity
    {
        [DataMember]
        [StringLengthValidator(1, 50)]
        public string Keyword { get; set; }
    }
}