using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryAutoCompleteParameter : SegmentWordParameter
    {
        [DataMember]
        public AutoCompleteComponent ComponentKind { get; set; }
    }
}