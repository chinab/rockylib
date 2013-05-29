using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class QueryNewsResult : PagedResult<QueryNewsResult.TResult>
    {
        [DataContract]
        public class TResult
        {
            [DataMember]
            public int CategoryID { set; get; }
            [DataMember]
            public string CategoryName { set; get; }
            [DataMember]
            public int RowID { set; get; }
            [DataMember]
            public string Title { set; get; }
            [DataMember]
            public string Content { set; get; }
            [DataMember]
            public string Origin { get; set; }
            [DataMember]
            public string Author { get; set; }
            [DataMember]
            public string ImageFileKey { set; get; }
            [DataMember]
            public string AttachmentFileKey { get; set; }
            [DataMember]
            public string Tag { get; set; }
            [DataMember]
            public int ViewCount { get; set; }
            [DataMember]
            public NewsFlags Flags { get; set; }
            [DataMember]
            public StatusKind Status { get; set; }
            [DataMember]
            public DateTime CreateDate { set; get; }
        }
    }
}