using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class TermControlResult
    {
        public Guid ControlID { get; set; }
        public string ControlName { get; set; }
        public PermissionFlags Permission { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}