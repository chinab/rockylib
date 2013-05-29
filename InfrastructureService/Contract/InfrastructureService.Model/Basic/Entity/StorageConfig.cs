using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Net;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class StorageConfig
    {
        [DataMember]
        public string StorageUrl { get; set; }

        [DataMember]
        public IPEndPoint ListenedAddress { get; set; }
    }
}