using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoSQL
{
    /// <summary>
    /// 缓存key生成
    /// </summary>
    public interface ICacheKeyGenerator
    {
        /// <summary>
        /// 是否自动调用HashKey方法
        /// </summary>
        bool AutoHash { get; set; }
        /// <summary>
        /// 把长Key Hash成相对短Key
        /// </summary>
        /// <param name="longKey"></param>
        void HashKey(ref string longKey);
        /// <summary>
        /// 根据实体生成键
        /// </summary>
        /// <param name="value">DO</param>
        /// <returns>Key</returns>
        string GenerateKey(object value);
    }
}