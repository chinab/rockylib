using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class SetNewsStatusParameter : HeaderEntity
    {
        [DataMember]
        public int[] RowIDSet { get; set; }
        [DataMember]
        public StatusKind Status { get; set; }
    }
}