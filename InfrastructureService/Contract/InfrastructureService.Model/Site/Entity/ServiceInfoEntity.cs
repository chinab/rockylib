using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class ServiceInfoEntity
    {
        public Guid RowID { get; set; }

        public String Name { get; set; }

        public String Description { get; set; }

        public Int32 Sort { get; set; }

        public Int32 Status { get; set; }
    }
}