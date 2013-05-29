using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Rocky.Net
{
    /// <summary>
    /// 分布式终结点
    /// </summary>
    [Serializable]
    public class DistributedEndPoint
    {
        public IPAddress[] Addresses { get; set; }
        public int Port { get; set; }
    }
}