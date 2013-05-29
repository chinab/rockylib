using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class SaveNewsParameter : HeaderEntity
    {
        [DataMember]
        public int RowID { get; set; }
        [DataMember]
        public int CategoryID { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string TitleColor { get; set; }
        [DataMember]
        public string Content { get; set; }
        [DataMember]
        public string Origin { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public string ImageFileKey { get; set; }
        [DataMember]
        public string AttachmentFileKey { get; set; }
        [DataMember]
        public string Tag { get; set; }
        [DataMember]
        public int ViewCount { get; set; }
    }
}