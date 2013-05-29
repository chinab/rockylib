using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class QueryComponentInfosParameter
    {
        public bool IncludeBlock { get; set; }
        public Guid? ServiceID { get; set; }
    }
}