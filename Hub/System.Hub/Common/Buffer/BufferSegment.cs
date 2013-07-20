using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using B = System.ArraySegment<byte>;

namespace System
{
    public sealed class BufferSegment
    {
        #region Static
        internal readonly static BufferSegment Instance;

        static BufferSegment()
        {
            Instance = new BufferSegment((int)BufferSizeof.MaxSocket, 200, bufferSize => new B(new byte[bufferSize]));
        }
        #endregion

        #region Fields
        private byte[] _buffer;
        private readonly int _bufferSize;
        private int _currentIndex;
        private ConcurrentStack<int> _freeStack;
        private Func<int, B> _overflow;
        #endregion

        #region Properties
        public int BufferSize
        {
            get { return _bufferSize; }
        }
        public int TotalBytes
        {
            get { return _buffer.Length; }
        }
        #endregion

        #region Constructors
        public BufferSegment(int bufferSize, int bufferCount, Func<int, B> overflow = null)
        {
            Contract.Requires(bufferSize > 0, "bufferSize");
            Contract.Requires(bufferCount > 0, "bufferCount");

            _bufferSize = bufferSize;
            int totalBytes = _bufferSize * bufferCount;
            _buffer = new byte[totalBytes];
            _overflow = overflow;
            _freeStack = new ConcurrentStack<int>();
        }
        #endregion

        #region Methods
        [DebuggerStepThrough]
        private bool CheckOverflow(out B bArray)
        {
            if ((_currentIndex + _bufferSize) > _buffer.Length)
            {
                if (_overflow == null || (bArray = _overflow(_bufferSize)) == default(B))
                {
                    throw new InternalBufferOverflowException();
                }
                return true;
            }
            bArray = default(B);
            return false;
        }

        public void Take(out B bArray)
        {
            int freeIndex;
            if (_freeStack.TryPop(out freeIndex))
            {
                bArray = new B(_buffer, freeIndex, _bufferSize);
                return;
            }

            if (!this.CheckOverflow(out bArray))
            {
                bArray = new B(_buffer, _currentIndex, _bufferSize);
                Interlocked.Add(ref _currentIndex, _bufferSize);
            }
        }
        public void Return(ref B bArray)
        {
            if (bArray.Array != _buffer)
            {
                return;
            }

            _freeStack.Push(bArray.Offset);
            bArray = default(B);
        }

        public MemoryStream Take()
        {
            B bArray;
            int freeIndex;
            if (_freeStack.TryPop(out freeIndex))
            {
                bArray = new B(_buffer, freeIndex, _bufferSize);
            }
            if (!this.CheckOverflow(out bArray))
            {
                bArray = new B(_buffer, _currentIndex, _bufferSize);
                Interlocked.Add(ref _currentIndex, _bufferSize);
            }

            return new BufferedMemoryStream(this, bArray);
        }
        #endregion
    }
}