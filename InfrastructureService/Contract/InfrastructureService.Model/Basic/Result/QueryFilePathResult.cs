using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryFilePathResult
    {
        [DataMember]
        public string VirtualPath { get; set; }

        [DataMember]
        public string PhysicalPath { get; set; }

        [DataMember]
        public string ServerAuthority { get; set; }
    }
}