using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class SegmentWordResult
    {
        [DataMember]
        public string[] Words { get; set; }
    }
}