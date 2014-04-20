using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public interface IDisposeService
    {
        void Register(Type owner, IDisposable instance);
        void Free(Type owner, IDisposable instance);
        void FreeAll(Type owner);
    }
}