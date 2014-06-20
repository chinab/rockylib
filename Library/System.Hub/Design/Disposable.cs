using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    [ComVisible(true)]
    public abstract class Disposable : IDisposable
    {
        #region Fields
        [DllImport("Psapi.dll")]
        internal static extern bool EmptyWorkingSet(IntPtr hProcess);
        private static int MemLocker;

        private volatile bool _disposed;
        #endregion

        #region Properties
        protected bool IsDisposed
        {
            get { return _disposed; }
        }
        #endregion

        #region Constructor
        ~Disposable()
        {
            Dispose(false);
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
        #endregion

        #region Methods
        [DebuggerStepThrough]
        protected virtual void CheckDisposed()
        {
            if (_disposed)
            {
                string typeName = this.GetType().FullName;
                throw new ObjectDisposedException(typeName, string.Format("Can't access a disposed {0}.", typeName));
            }
        }

        protected void DisposeObject(object obj)
        {
            var free = obj as IDisposable;
            if (free != null)
            {
                free.Dispose();
            }
        }

        protected void ReleaseMemory()
        {
            if (Interlocked.Exchange(ref MemLocker, 1) == 0)
            {
                try
                {
                    //long m = 100 * 1024;
                    //GC.AddMemoryPressure(m);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    //GC.WaitForFullGCComplete();
                    //GC.Collect();
                    //GC.RemoveMemoryPressure(m);
                    var proc = Process.GetCurrentProcess();
                    EmptyWorkingSet(proc.Handle);
                    App.LogInfo("ReleaseMemory...");
                }
                finally
                {
                    Interlocked.Exchange(ref MemLocker, 0);
                }
            }
            else
            {
                Thread.Sleep(20 * 1000);
            }
        }
        #endregion
    }
}