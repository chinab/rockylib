using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model.Site
{
    [DataContract]
    public enum RenderKind
    {
        [EnumMember]
        Text = 0,
        [EnumMember]
        Media = 1
    }
}