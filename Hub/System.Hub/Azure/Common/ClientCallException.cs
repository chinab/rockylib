using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Client
{
    [Serializable]
    public class ClientCallException : Exception
    {
        public ClientCallException() { }
        public ClientCallException(string message) : base(message) { }
        public ClientCallException(string message, Exception inner) : base(message, inner) { }
        protected ClientCallException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}