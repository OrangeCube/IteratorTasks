using System;
using System.Threading;

namespace IteratorTasks
{
    public partial class Task
    {
        public static Task Delay(TimeSpan span) { return Delay((int)span.TotalMilliseconds, CancellationToken.None, null); }
        public static Task Delay(int delayMilliseconds) { return Delay(delayMilliseconds, CancellationToken.None, null); }
        public static Task Delay(int delayMilliseconds, TaskScheduler scheduler)
        {
            return Delay(delayMilliseconds, CancellationToken.None, scheduler);
        }

        public static Task Delay(TimeSpan span, CancellationToken ct) { return Delay((int)span.TotalMilliseconds, ct, null); }
        public static Task Delay(int delayMilliseconds, CancellationToken ct) { return Delay(delayMilliseconds, ct, null); }
        public static Task Delay(int delayMilliseconds, CancellationToken ct, TaskScheduler scheduler)
        {
            var tcs = new TaskCompletionSource<object>(scheduler);

            Timer timer = null;

            timer = new Timer(state =>
            {
                timer.Dispose();
                tcs.TrySetResult(null);
            }, null, delayMilliseconds, Timeout.Infinite);

            if (ct != CancellationToken.None)
                ct.Register(() => { timer.Dispose(); tcs.TrySetCanceled(); });

            return tcs.Task;
        }
    }
}
