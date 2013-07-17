using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    [ContractClass(typeof(ISocksRFCContract))]
    public interface ISocksRFC
    {
        /// <summary>
        /// 转换成数据包
        /// </summary>
        /// <returns></returns>
        byte[] ToPackets();
        /// <summary>
        /// 解析数据包覆盖当前值并验证当前数据包
        /// </summary>
        /// <param name="pPack"></param>
        /// <exception cref="System.Net.ProxyAuthException"></exception>
        void ParsePack(byte[] pPack);
    }

    [ContractClassFor(typeof(ISocksRFC))]
    internal abstract class ISocksRFCContract : ISocksRFC
    {
        byte[] ISocksRFC.ToPackets()
        {
            Contract.Ensures(!Contract.Result<byte[]>().IsNullOrEmpty());
            return default(byte[]);
        }

        void ISocksRFC.ParsePack(byte[] pPack)
        {
            Contract.Requires(!pPack.IsNullOrEmpty());
        }
    }
}