using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class FriendLinkEntity : HeaderEntity
    {
        [DataMember]
        public Guid RowID { get; set; }
        [DataMember]
        public string SiteName { get; set; }
        [DataMember]
        public string SiteUrl { get; set; }
        [DataMember]
        public string LinkText { get; set; }
        [DataMember]
        public string LinkFileKey { get; set; }
        [DataMember]
        public int Sort { get; set; }
        [DataMember]
        public RenderKind RenderKind { get; set; }
        [DataMember]
        public DateTime CreateDate { get; set; }
        [DataMember]
        public StatusKind Status { get; set; }
    }
}