using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Rocky.Net
{
    [Serializable]
    public class HolePunchingModel
    {
        public HolePunchingCommand Command { get; set; }
        /// <summary>
        /// 请求打洞的对方IP
        /// </summary>
        public IPAddress RequestIP { get; set; }
        /// <summary>
        /// 响应打洞的对方终结点
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }
    }
}