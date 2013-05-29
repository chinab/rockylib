using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class QueryCategoriesParameter : HeaderEntity
    {
        [DataMember]
        public bool DeepQuery { get; set; }
        [DataMember]
        public int? ParentID { get; set; }
        [DataMember]
        public string Name { get; set; }
    }
}