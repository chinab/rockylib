using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class QueryRoleResult
    {
        public RoleEntity Role { get; set; }
        public OneToManyResult<ComponentResult, ControlResult> ComponentSet { get; set; }
    }

    [Serializable]
    public class ComponentResult
    {
        public string ServiceName { set; get; }
        public string ComponentName { set; get; }
    }
    [Serializable]
    public class ControlResult
    {
        public Guid RowID { set; get; }
        public string Name { set; get; }
        public string Description { set; get; }
        public int PermissionFlags { set; get; }
    }
}