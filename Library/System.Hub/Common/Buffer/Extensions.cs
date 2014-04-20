using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using B = System.ArraySegment<byte>;

namespace System
{
    public static partial class Extensions
    {
        #region bytes
        public static int FindByteArray(this byte[] instance, int index, int count, byte[] bArray)
        {
            Contract.Requires(instance != null);

            int nRet = -1;
            if (count < bArray.Length)
            {
                return nRet;
            }

            int nSearchLen = bArray.Length;
            for (int i = index; i <= count - nSearchLen; i++)
            {
                bool bFoundHere = true;
                for (int s = 0; s < nSearchLen; s++)
                {
                    if (instance[i + s] != bArray[s])
                    {
                        bFoundHere = false;
                        break;
                    }
                }
                if (bFoundHere == true)
                {
                    return i;
                }
            }
            return nRet;
        }
        #endregion

        #region Stream
        public static int Read(this Stream instance, B bArray)
        {
            return instance.Read(bArray, 0, bArray.Count);
        }
        /// <summary>
        /// 读取bArray
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="bArray"></param>
        /// <param name="offset">相对偏移量</param>
        /// <param name="count">读取长度</param>
        /// <returns></returns>
        public static int Read(this Stream instance, B bArray, int offset, int count)
        {
            Contract.Requires(instance != null && bArray.Array != null);
            Contract.Requires(offset >= 0 && offset <= count);
            Contract.Requires(offset + count <= bArray.Count);

            offset += bArray.Offset;
            return instance.Read(bArray.Array, offset, count);
        }

        public static void Write(this Stream instance, B bArray)
        {
            instance.Write(bArray, 0, bArray.Count);
        }
        /// <summary>
        /// 写入bArray
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="bArray"></param>
        /// <param name="offset">相对偏移量</param>
        /// <param name="count">写入长度</param>
        public static void Write(this Stream instance, B bArray, int offset, int count)
        {
            Contract.Requires(instance != null);
            Contract.Requires(offset >= 0 && offset <= count);
            Contract.Requires(offset + count <= bArray.Count);

            offset += bArray.Offset;
            instance.Write(bArray.Array, offset, count);
        }

        public static void FixedCopyTo(this Stream instance, Stream destination, Func<int, bool> perAction = null)
        {
            Contract.Requires(instance != null && instance.CanRead);
            Contract.Requires(destination != null && destination.CanWrite);

            B buffer;
            BufferSegment.Instance.Take(out buffer);
            try
            {
                int read;
                while ((read = instance.Read(buffer.Array, buffer.Offset, buffer.Count)) != 0)
                {
                    destination.Write(buffer.Array, buffer.Offset, read);
                    if (perAction != null && !perAction(read))
                    {
                        break;
                    }
#if Sleep
                    Thread.Sleep(1);
#endif
                }
            }
            finally
            {
                BufferSegment.Instance.Return(ref buffer);
            }
        }
        public static void FixedCopyTo(this Stream instance, Stream destination, long length, Func<int, bool> perAction = null)
        {
            Contract.Requires(instance != null && instance.CanRead);
            Contract.Requires(destination != null && destination.CanWrite);

            B buffer;
            BufferSegment.Instance.Take(out buffer);
            try
            {
                int read;
                while (length > 0 && (read = instance.Read(buffer.Array, buffer.Offset, Math.Min(buffer.Count, (int)length))) != 0)
                {
                    destination.Write(buffer.Array, buffer.Offset, read);
                    length -= read;
                    if (perAction != null && !perAction(read))
                    {
                        break;
                    }
#if Sleep
                    Thread.Sleep(1);
#endif
                }
            }
            finally
            {
                BufferSegment.Instance.Return(ref buffer);
            }
        }
        #endregion
    }
}