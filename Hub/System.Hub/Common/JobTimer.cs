using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Threading;

namespace System
{
    public sealed class JobTimer : IDisposable
    {
        #region Fields
        private Timer _timer;
        private int _isRunning;
        private Action<object> _job;
        #endregion

        #region Properties
        public TimeSpan DueTime { get; private set; }
        public TimeSpan Period { get; private set; }
        public DateTime PreviousExecuteTime { get; private set; }
        public DateTime NextExecuteTime { get; private set; }
        #endregion

        #region Methods
        private JobTimer(Action<object> job, DateTime dueTime, TimeSpan period)
        {
            Contract.Requires(job != null);

            _job = job;
            if (dueTime != DateTime.MaxValue)
            {
                this.DueTime = dueTime - DateTime.UtcNow;
            }
            if (this.DueTime < TimeSpan.Zero)
            {
                this.DueTime = TimeSpan.Zero;
            }
            this.Period = period;

            Hub.DisposeService.Register(this.GetType(), this);
        }
        public JobTimer(Action<object> job, DateTime dueTime)
            : this(job, dueTime, TimeSpan.FromMilliseconds(Timeout.Infinite))
        {

        }
        public JobTimer(Action<object> job, TimeSpan period)
            : this(job, DateTime.MaxValue, period)
        {

        }

        public void Execute(object state)
        {
            if (Interlocked.Exchange(ref _isRunning, 1) == 0)
            {
                try
                {
                    _job(state);
                }
                catch (Exception ex)
                {
                    Hub.LogError(ex, "JobTimer");
                }
                finally
                {
                    this.PreviousExecuteTime = DateTime.UtcNow;
                    if (this.Period.Milliseconds == Timeout.Infinite)
                    {
                        this.NextExecuteTime = DateTime.MaxValue;  //下次执行时间不存在 
                        this.Dispose();
                    }
                    else
                    {
                        this.NextExecuteTime = this.PreviousExecuteTime + this.Period;
                    }

                    Interlocked.Exchange(ref _isRunning, 0);
                }
            }
        }

        public void Start(object state = null)
        {
            if (_timer == null)
            {
                _timer = new Timer(this.Execute, state, this.DueTime, this.Period);
            }
            else
            {
                _timer.Change(this.DueTime, this.Period);
            }
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            if (_job != null)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                }
                _job = null;

                Hub.DisposeService.Free(this.GetType(), this);
            }
        }
        #endregion
    }
}