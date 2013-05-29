using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Serializable]
    public class QueryControlInfosParameter
    {
        public bool IncludeBlock { get; set; }
        public Guid? ComponentID { get; set; }
    }
}