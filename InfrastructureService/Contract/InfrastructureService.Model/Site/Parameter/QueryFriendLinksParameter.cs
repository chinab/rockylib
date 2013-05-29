using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public class QueryFriendLinksParameter : PagingParameter
    {
        [DataMember]
        public Guid? RowID { get; set; }
    }
}