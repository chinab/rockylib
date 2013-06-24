using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class RoleEntity
    {
        public Guid RowID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? AssignUserCount { get; set; }
    }
}