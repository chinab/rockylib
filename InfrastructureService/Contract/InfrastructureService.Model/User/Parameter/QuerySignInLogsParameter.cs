using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class QuerySignInLogsParameter : PagingParameter
    {
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public bool? IsSuccess { get; set; }
    }
}