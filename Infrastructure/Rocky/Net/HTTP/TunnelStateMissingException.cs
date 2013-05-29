using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Rocky.Net
{
    [Serializable]
    public class TunnelStateMissingException : InvalidOperationException
    {
        public Socket Client { get; set; }

        public TunnelStateMissingException() { }
        public TunnelStateMissingException(string message) : base(message) { }
        public TunnelStateMissingException(string message, Exception inner) : base(message, inner) { }
        protected TunnelStateMissingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}