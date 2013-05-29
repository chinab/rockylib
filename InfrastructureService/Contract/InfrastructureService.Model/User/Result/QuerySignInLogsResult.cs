using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class QuerySignInLogsResult : PagedResult<QuerySignInLogsResult.TResult>
    {
        [DataContract]
        public class TResult
        {
            [DataMember]
            public string UserName { get; set; }
            [DataMember]
            public string ClientIP { get; set; }
            [DataMember]
            public string Platform { get; set; }
            [DataMember]
            public DateTime SignInDate { get; set; }
            [DataMember]
            public bool IsSuccess { get; set; }
        }

        [DataMember]
        public int SignInCount { get; set; }
    }
}