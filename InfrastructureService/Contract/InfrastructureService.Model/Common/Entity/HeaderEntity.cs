using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model
{
    [DataContract]
    public abstract class HeaderEntity
    {
        [DataMember]
        public Guid AppID { get; set; }
    }
}