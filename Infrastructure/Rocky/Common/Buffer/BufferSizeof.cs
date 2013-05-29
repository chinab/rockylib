using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rocky
{
    public enum BufferSizeof
    {
        /// <summary>
        /// 1K
        /// </summary>
        InMemory = 1024,
        /// <summary>
        /// 4K
        /// </summary>
        File = InMemory * 4,
        /// <summary>
        /// 8K
        /// </summary>
        MaxSocket = InMemory * 8,
        /// <summary>
        /// 1M
        /// </summary>
        ThreadStatic = InMemory ^ 2
    }
}