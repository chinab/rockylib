using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rocky.Caching
{
    /// <summary>
    /// 分布式缓存异常
    /// </summary>
    [Serializable]
    public class DistributedException : Exception
    {
        /// <summary>
        /// 缓存提供者的固定名称
        /// </summary>
        public string ProviderInvariantName { get; set; }

        public DistributedException() { }
        public DistributedException(string message) : base(message) { }
        public DistributedException(string message, Exception inner) : base(message, inner) { }
        protected DistributedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}