using System;
using System.Threading;

namespace IteratorTasks
{
    public partial class Task
    {
        public static Task Delay(TimeSpan span) { return Delay(span, CancellationToken.None); }
        public static Task Delay(int delayMilliseconds) { return Delay(delayMilliseconds, CancellationToken.None, null); }
        public static Task Delay(int delayMilliseconds, TaskScheduler scheduler)
        {
            return Delay(delayMilliseconds, CancellationToken.None, scheduler);
        }

        public static Task Delay(TimeSpan span, CancellationToken ct)
        {
            var ms = span.TotalMilliseconds;
            // Delayの最大待ち時間(約４年)より長い場合はキャンセルされるまで待つタスクになる
            if(int.MaxValue < ms)
            {
                var tcs = new TaskCompletionSource<object>();
                ct.Register(() => tcs.SetResult(default(object)));
                return tcs.Task;
            }
            return Delay((int)ms, ct, null);
        }
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
