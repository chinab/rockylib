using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class QueryFileResult
    {
        [DataMember]
        public string FileKey { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public DateTime CreateDate { get; set; }
    }
}