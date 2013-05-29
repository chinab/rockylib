using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryHotKeywordsResult
    {
        [DataMember]
        public string[] Keywords { get; set; }
    }
}