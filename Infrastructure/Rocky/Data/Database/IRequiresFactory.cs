﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Data
{
    public interface IRequiresFactory
    {
        DbFactory Factory { get; }
    }
}