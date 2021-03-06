﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryFileParameter : HeaderEntity
    {
        [DataMember]
        [StringLengthValidator(32, 32)]
        public string FileKey { get; set; }
    }
}