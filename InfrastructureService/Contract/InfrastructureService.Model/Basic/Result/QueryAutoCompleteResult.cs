using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryAutoCompleteResult : ExecResult<QueryAutoCompleteResult.TResult>
    {
        [DataContract]
        public class TResult
        {
            [DataMember]
            public string Keyword { set; get; }

            [DataMember]
            public int RecordCount { set; get; }
        }

        [DataMember]
        public string[] SegmentWords { get; set; }
    }
}