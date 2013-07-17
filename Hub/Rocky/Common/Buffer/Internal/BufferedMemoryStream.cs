using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using B = System.ArraySegment<byte>;

namespace System
{
    internal sealed class BufferedMemoryStream : MemoryStream
    {
        private BufferSegment _bSegment;
        private B _bArray;

        public BufferedMemoryStream(BufferSegment owner, B bArray)
            : base(bArray.Array, bArray.Offset, bArray.Count, true, false)
        {
            _bSegment = owner;
            _bArray = bArray;
            base.Capacity = bArray.Count;
        }

        protected override void Dispose(bool disposing)
        {
            if (_bSegment != null)
            {
                _bSegment.Return(ref _bArray);
            }
            _bSegment = null;
            base.Dispose(disposing);
        }

        public override void SetLength(long value)
        {
            if (value == -1L)
            {
                value = base.Capacity;
            }
            base.SetLength(value);
        }
    }
}