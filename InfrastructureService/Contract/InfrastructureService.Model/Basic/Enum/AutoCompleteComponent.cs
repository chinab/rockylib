using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public enum AutoCompleteComponent
    {
        [EnumMember]
        Rhnz = 0,
        [EnumMember]
        Xfjob_Job = 1
    }
}