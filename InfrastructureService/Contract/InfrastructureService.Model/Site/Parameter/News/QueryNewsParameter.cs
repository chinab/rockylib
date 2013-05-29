using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class QueryNewsParameter : PagingParameter
    {
        [DataMember]
        public string Keyword { get; set; }
        [DataMember]
        public int? RowID { get; set; }
        [DataMember]
        public int? CategoryID { get; set; }
        [DataMember]
        public NewsFlags? Flags { get; set; }
        [DataMember]
        public StatusKind? Status { get; set; }
        [DataMember]
        public bool HasImage { get; set; }
    }
}