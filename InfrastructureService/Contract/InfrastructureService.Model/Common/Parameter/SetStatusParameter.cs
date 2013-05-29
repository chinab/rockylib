using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model
{
    [DataContract]
    public class SetStatusParameter : HeaderEntity
    {
        [DataMember]
        public Guid[] RowIDSet { get; set; }

        [DataMember]
        public StatusKind Status { get; set; }
    }
}