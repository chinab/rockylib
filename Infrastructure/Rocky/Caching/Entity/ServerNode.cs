using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace System.Caching
{
    /// <summary>
    /// 服务节点
    /// </summary>
    public class ServerNode
    {
        /// <summary>
        /// 服务地址
        /// </summary>
        public IPEndPoint IPEndPoint { get; set; }
        /// <summary>
        /// 验证凭据
        /// </summary>
        public NetworkCredential Credentials { get; set; }
        /// <summary>
        /// 只读服务
        /// </summary>
        public bool ReadOnly { get; set; }
    }
}