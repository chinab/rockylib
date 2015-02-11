using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System.Net
{
    /// <summary>
    /// more info: http://stackoverflow.com/a/21808747/1768303
    /// </summary>
    public class MessageLoopApartment : IDisposable
    {
        /// <summary>
        /// the STA thread
        /// </summary>
        private Thread _thread;
        private TaskScheduler _taskScheduler;

        /// <summary>
        /// the STA thread's task scheduler
        /// </summary>
        public TaskScheduler TaskScheduler
        {
            get { return _taskScheduler; }
        }

        /// <summary>
        /// MessageLoopApartment constructor
        /// </summary>
        public MessageLoopApartment()
        {
            var tcs = new TaskCompletionSource<TaskScheduler>();
            // start an STA thread and gets a task scheduler
            _thread = new Thread(startArg =>
            {
                EventHandler idleHandler = null;
                idleHandler = (s, e) =>
                {
                    // handle Application.Idle just once
                    Application.Idle -= idleHandler;
                    // return the task scheduler
                    tcs.SetResult(TaskScheduler.FromCurrentSynchronizationContext());
                };

                // handle Application.Idle just once
                // to make sure we're inside the message loop
                // and SynchronizationContext has been correctly installed
                Application.Idle += idleHandler;
                Application.Run();
            });
            _thread.IsBackground = true;
            _thread.SetApartmentState(ApartmentState.STA);
            //try
            //{
            _thread.Start();
            //}
            //catch (OutOfMemoryException ex)
            //{
            //    Environment.Exit(ex.HResult);
            //}
            _taskScheduler = tcs.Task.Result;
        }
        /// <summary>
        /// Shutdown the STA thread
        /// </summary>
        public void Dispose()
        {
            if (_taskScheduler != null)
            {
                var taskScheduler = _taskScheduler;
                _taskScheduler = null;

                // execute Application.ExitThread() on the STA thread
                Task.Factory.StartNew(Application.ExitThread, CancellationToken.None, TaskCreationOptions.None, taskScheduler).Wait();
                _thread.Join();
                _thread = null;
            }
        }

        public void Invoke(Action func)
        {
            Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, _taskScheduler).Wait();
        }
        public TResult Invoke<TResult>(Func<TResult> func)
        {
            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, _taskScheduler).Result;
        }

        internal void Invoke(Action<object> func, object state)
        {
            Task.Factory.StartNew(func, state, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }
    }
}