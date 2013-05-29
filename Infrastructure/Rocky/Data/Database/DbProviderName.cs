using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rocky.Data
{
    public enum DbProviderName
    {
        OleDb,
        SQLServer,
        [Obsolete]
        Oracle
    }
}