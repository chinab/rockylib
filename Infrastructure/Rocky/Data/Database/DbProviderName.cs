﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Data
{
    public enum DbProviderName
    {
        OleDb,
        SQLServer,
        [Obsolete]
        Db4o
    }
}