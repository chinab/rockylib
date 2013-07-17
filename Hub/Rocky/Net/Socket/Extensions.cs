//#define Sleep
#undef Sleep
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    public static partial class Extensions
    {
        #region Socks
        /// <summary>
        /// 同步连接
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        public static void Connect(this Socket client, DistributedEndPoint endpoint)
        {
            var addr = endpoint.Addresses.Where(t => client.AddressFamily == t.AddressFamily);
            client.Connect(addr.ToArray(), endpoint.Port);
        }

        public static void Send<T>(this Socket client, T packModel)
        {
            var stream = Serializer.Serialize(packModel);
            int length = (int)stream.Length;
            client.Send(BitConverter.GetBytes(length));
            var netStream = new NetworkStream(client, FileAccess.ReadWrite, false);
            stream.FixedCopyTo(netStream, length);
        }

        public static void Receive<T>(this Socket client, out T packModel)
        {
            byte[] data = new byte[4];
            client.Receive(data, 0, 4, SocketFlags.None);
            int length = BitConverter.ToInt32(data, 0);
            var stream = new MemoryStream();
            var netStream = new NetworkStream(client, FileAccess.ReadWrite, false);
            netStream.FixedCopyTo(stream, length);
            stream.Position = 0L;
            packModel = (T)Serializer.Deserialize(stream);
        }

        /// <summary>
        /// 1.Call shutdown with how=SD_SEND.
        /// 2.Call recv until zero returned, or SOCKET_ERROR.
        /// 3.Call closesocket.
        /// </summary>
        /// <param name="client"></param>
        public static void Disconnect(this Socket client)
        {
            client.Shutdown(SocketShutdown.Send);
            //client.Disconnect(true);
            client.Close(1);
        }
        #endregion

        #region Options
        /// <summary>
        /// 设置心跳包捕获ConnectionReset 10054异常
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="keepalive_time">多长时间后开始第一次探测（单位：毫秒）</param>
        /// <param name="keepalive_interval">探测时间间隔（单位：毫秒）</param>
        public static void SetKeepAlive(this Socket instance, ulong keepalive_time, ulong keepalive_interval)
        {
            Contract.Requires(instance != null);

            int bytes_per_long = 32 / 8;
            byte[] keep_alive = new byte[3 * bytes_per_long];
            ulong[] input_params = new ulong[3];
            int i1;
            int bits_per_byte = 8;
            if (keepalive_time == 0 || keepalive_interval == 0)
            {
                input_params[0] = 0;
            }
            else
            {
                input_params[0] = 1;
            }
            input_params[1] = keepalive_time;
            input_params[2] = keepalive_interval;
            for (i1 = 0; i1 < input_params.Length; i1++)
            {
                keep_alive[i1 * bytes_per_long + 3] = (byte)(input_params[i1] >> ((bytes_per_long - 1) * bits_per_byte) & 0xff);
                keep_alive[i1 * bytes_per_long + 2] = (byte)(input_params[i1] >> ((bytes_per_long - 2) * bits_per_byte) & 0xff);
                keep_alive[i1 * bytes_per_long + 1] = (byte)(input_params[i1] >> ((bytes_per_long - 3) * bits_per_byte) & 0xff);
                keep_alive[i1 * bytes_per_long + 0] = (byte)(input_params[i1] >> ((bytes_per_long - 4) * bits_per_byte) & 0xff);
            }
            instance.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, keep_alive);
        }

        /// <summary>
        /// 端口劫持
        /// </summary>
        /// <param name="instance"></param>
        public static void ReuseAddress(this Socket instance, IPEndPoint ipe = null)
        {
            Contract.Requires(instance != null);

            instance.ExclusiveAddressUse = false;
            instance.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            if (ipe != null)
            {
                instance.Bind(ipe);
            }
        }
        #endregion
    }
}