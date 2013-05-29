using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace InfrastructureService.Common
{
    [DataContract]
    public class AppFaultDetail
    {
        [DataMember]
        public int ErrorCode { get; set; }
        [DataMember]
        public bool ThrowFault { get; set; }
        [DataMember]
        public ExceptionDetail Exception { get; set; }
    }
}