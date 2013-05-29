using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Rocky
{
    public abstract class Disposable : IDisposable
    {
        private bool _disposed;

        protected bool IsDisposed
        {
            get { return _disposed; }
        }

        ~Disposable()
        {
            Dispose(false);
        }

        [DebuggerStepThrough]
        protected virtual void CheckDisposed()
        {
            if (_disposed)
            {
                string typeName = this.GetType().FullName;
                throw new ObjectDisposedException(typeName, string.Format("Cannot access a disposed {0}.", typeName));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                DisposeInternal(disposing);
                _disposed = true;
            }
        }
        /// <summary>
        /// Free native resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to Dispose managed resources</param>
        protected abstract void DisposeInternal(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}