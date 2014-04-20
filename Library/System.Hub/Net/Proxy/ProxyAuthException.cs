using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace System.Net
{
    /// <summary>
    /// Proxy authentication failure.
    /// </summary>
    [Serializable]
    public class ProxyAuthException : HttpException
    {
        public ProxyAuthException(int httpCode, string message) : base(httpCode, message) { }
        public ProxyAuthException(int httpCode, string message, Exception inner) : base(httpCode, message, inner) { }
        protected ProxyAuthException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}