using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rocky.UnitTesting
{
    [Serializable]
    public class UserTableEntity
    {
        public int RowID { get; set; }
        public string UserName { get; set; }
        public DateTime CreateDate { get; set; }
    }
}