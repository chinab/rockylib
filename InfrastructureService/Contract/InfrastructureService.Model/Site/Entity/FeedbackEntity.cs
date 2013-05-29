using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class FeedbackEntity : HeaderEntity
    {
        [DataMember]
        public Guid RowID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Phone { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public FeedbackKind Kind { get; set; }
        [DataMember]
        public string Content { get; set; }
        [DataMember]
        public DateTime CreateDate { get; set; }
        [DataMember]
        public StatusKind Status { get; set; }
    }
}