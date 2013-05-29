using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class SaveCategoryParameter : HeaderEntity
    {
        [DataMember]
        public int RowID { get; set; }
        [DataMember]
        public int ParentID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int Sort { get; set; }
        [DataMember]
        public string SEO_Keyword { get; set; }
        [DataMember]
        public string SEO_Description { get; set; }
    }
}