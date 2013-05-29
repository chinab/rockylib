using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.User
{
    [DataContract]
    public class UserEntity
    {
        [DataMember]
        public Guid UserID { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Mobile { get; set; }
        [DataMember]
        public UserFlags Flags { get; set; }
        [DataMember]
        public DateTime CreateDate { get; set; }
    }
}