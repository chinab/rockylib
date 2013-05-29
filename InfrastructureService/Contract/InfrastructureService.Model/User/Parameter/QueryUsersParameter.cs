using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class QueryUsersParameter : PagingParameter
    {
        [DataMember]
        public string Keyword { get; set; }
        [DataMember]
        public Guid? UserID { get; set; }
        [DataMember]
        public QueryUsersOrderBy OrderBy { get; set; }
    }

    public enum QueryUsersOrderBy
    {
        CreateDateDesc,
        CreateDateAsc
    }
}