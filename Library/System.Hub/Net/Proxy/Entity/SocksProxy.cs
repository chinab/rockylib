using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace System.Net
{
    public class SocksProxy
    {
        public SocksProxyType ProxyType { get; set; }
        public IPEndPoint Address { get; set; }
        public NetworkCredential Credential { get; set; }

        internal object State { get; set; }
    }
}