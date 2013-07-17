using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Caching
{
    public sealed class LockWrapper : IDisposable
    {
        #region Static
        public static TResult ReadLock<TResult>(ReaderWriterLockSlim locker, int timeout, Func<TResult> doWork)
        {
            bool releaseLock = false;
            if (!locker.IsWriteLockHeld && !locker.IsUpgradeableReadLockHeld && !locker.IsReadLockHeld)
            {
                releaseLock = locker.TryEnterReadLock(timeout);
            }
            try
            {
                return doWork();
            }
            finally
            {
                if (releaseLock)
                {
                    locker.ExitReadLock();
                }
            }
        }

        public static TResult WriteLock<TResult>(ReaderWriterLockSlim locker, int timeout, Func<TResult> doWork)
        {
            LockStatus status = locker.IsWriteLockHeld ? LockStatus.WriteLock : (locker.IsReadLockHeld ? LockStatus.ReadLock : LockStatus.Unlocked);
            bool releaseLock = false;
            switch (status)
            {
                case LockStatus.ReadLock:
                    releaseLock = locker.TryEnterUpgradeableReadLock(timeout);
                    break;
                case LockStatus.Unlocked:
                    releaseLock = locker.TryEnterWriteLock(timeout);
                    break;
            }
            try
            {
                return doWork();
            }
            finally
            {
                if (releaseLock)
                {
                    switch (status)
                    {
                        case LockStatus.ReadLock:
                            locker.ExitUpgradeableReadLock();
                            break;
                        case LockStatus.Unlocked:
                            locker.ExitWriteLock();
                            break;
                    }
                }
            }
        }
        #endregion

        #region Fields
        private ReaderWriterLock _locker;
        private LockStatus _status;
        private int _timeout;
        private LockCookie _cookie;
        private bool _upgraded;
        #endregion

        #region Properties
        public LockStatus Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    if (_status == LockStatus.Unlocked)
                    {
                        _upgraded = false;
                        if (value == LockStatus.ReadLock)
                        {
                            _locker.AcquireReaderLock(_timeout);
                        }
                        else if (value == LockStatus.WriteLock)
                        {
                            _locker.AcquireWriterLock(_timeout);
                        }
                    }
                    else if (value == LockStatus.Unlocked)
                    {
                        _locker.ReleaseLock();
                    }
                    else if (value == LockStatus.WriteLock) // && status==LockStatus.ReadLock
                    {
                        _cookie = _locker.UpgradeToWriterLock(_timeout);
                        _upgraded = true;
                    }
                    else if (_upgraded) // value==LockStatus.ReadLock && status==LockStatus.WriteLock
                    {
                        _locker.DowngradeFromWriterLock(ref _cookie);
                        _upgraded = false;
                    }
                    else
                    {
                        _locker.ReleaseLock();
                        _status = LockStatus.Unlocked;
                        _locker.AcquireReaderLock(_timeout);
                    }
                    _status = value;
                }
            }
        }
        #endregion

        #region Methods
        public LockWrapper(ReaderWriterLock locker, LockStatus status, int timeoutMS)
        {
            this._locker = locker;
            this.Status = status;
            this._timeout = timeoutMS;
        }

        public void Dispose()
        {
            this.Status = LockStatus.Unlocked;
        }
        #endregion
    }

    public enum LockStatus
    {
        Unlocked,
        ReadLock,
        WriteLock
    }
}