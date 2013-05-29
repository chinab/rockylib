using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class QueryNewsDetailResult
    {
        [DataContract]
        public class NewsSimpleResult
        {
            [DataMember]
            public int RowID { get; set; }
            [DataMember]
            public string Title { set; get; }
        }

        [DataMember]
        public QueryNewsResult.TResult News { get; set; }
        [DataMember]
        public NewsSimpleResult PreviousNews { get; set; }
        [DataMember]
        public NewsSimpleResult NextNews { get; set; }
    }
}