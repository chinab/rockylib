using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class SiteADEntity : HeaderEntity
    {
        [DataMember]
        public Guid RowID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public string FileKey { get; set; }
        [DataMember]
        public int Sort { get; set; }
        [DataMember]
        public int RenderWidth { get; set; }
        [DataMember]
        public int RenderHeight { get; set; }
        [DataMember]
        public DateTime BeginDate { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }
        [DataMember]
        public StatusKind Status { get; set; }
    }
}