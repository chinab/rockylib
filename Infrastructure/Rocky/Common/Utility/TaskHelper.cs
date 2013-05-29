using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rocky
{
    public static class TaskHelper
    {
        public static TaskFactory Factory
        {
            get { return Task.Factory; }
        }

        static TaskHelper()
        {
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(TaskScheduler_UnobservedTaskException);
        }
        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Runtime.LogError(e.Exception, "UnobservedTaskException");
        }

        public static Task ObservedException(this Task task)
        {
            return task.ContinueWith(t =>
            {
                if (t.Exception == null)
                {
                    return;
                }

                var aggException = t.Exception.Flatten();
                foreach (var ex in aggException.InnerExceptions)
                {
                    Runtime.LogError(ex, "UnobservedTaskException");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}