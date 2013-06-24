using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class ControlInfoEntity
    {
        public Guid RowID { get; set; }

        public Guid ComponentID { get; set; }

        public String Name { get; set; }

        public String Description { get; set; }

        public String Path { get; set; }

        public Int32 Sort { get; set; }

        public StatusKind Status { get; set; }
    }
}