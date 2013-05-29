using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class CreateRoleParameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid[] ControlIDs { get; set; }
        public PermissionFlags[] Permissions { get; set; }
    }
}