using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rocky.Data
{
    public interface IRequiresFactory
    {
        DbFactory Factory { get; }
    }
}