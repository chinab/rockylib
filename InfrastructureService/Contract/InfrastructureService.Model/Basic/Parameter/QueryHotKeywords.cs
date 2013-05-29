using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryHotKeywordsParameter : HeaderEntity
    {
        [DataMember]
        public AutoCompleteComponent ComponentKind { get; set; }

        [DataMember]
        public int TakeCount { get; set; }
    }
}