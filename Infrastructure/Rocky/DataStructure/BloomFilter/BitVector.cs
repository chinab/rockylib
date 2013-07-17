using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace System.DataStructure
{
    /// <summary>
    /// 基于BitArray的BitVector
    /// </summary>
    public sealed class BitVector : IBitVector
    {
        #region Fields
        private uint _trueCount;
        private BitArray _bits;
        #endregion

        #region Properties
        public uint Capacity
        {
            get
            {
                return (uint)_bits.Length;
            }
            set
            {
                checked
                {
                    _bits.Length = (int)value;
                }
            }
        }
        public uint Count
        {
            get { return _trueCount; }
        }
        int ICollection.Count
        {
            get
            {
                checked
                {
                    return (int)this.Count;
                }
            }
        }
        public bool IsSynchronized
        {
            get { return _bits.IsSynchronized; }
        }
        public object SyncRoot
        {
            get { return _bits.SyncRoot; }
        }

        public bool this[uint index]
        {
            get
            {
                checked
                {
                    return _bits[(int)index];
                }
            }
            set
            {
                checked
                {
                    int offset = (int)index;
                    bool org = _bits[offset];
                    if (org == value)
                    {
                        return;
                    }

                    if (value)
                    {
                        _trueCount++;
                    }
                    else
                    {
                        _trueCount--;
                    }
                    _bits[offset] = value;
                }
            }
        }
        #endregion

        #region Methods
        public BitVector(uint capacity)
        {
            checked
            {
                _bits = new BitArray((int)capacity);
            }
        }

        public void CopyTo(Array array, int index)
        {
            _bits.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _bits.GetEnumerator();
        }
        #endregion
    }

    /// <summary>
    /// bit向量接口
    /// </summary>
    public interface IBitVector : ICollection
    {
        /// <summary>
        /// 元素的数目
        /// </summary>
        uint Capacity { get; set; }
        /// <summary>
        /// 包含为true的元素数
        /// </summary>
        new uint Count { get; }
        /// <summary>
        /// 获取或设置特定位置的位的值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        bool this[uint index] { get; set; }
    }
}