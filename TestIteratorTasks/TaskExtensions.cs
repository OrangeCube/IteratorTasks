using IteratorTasks;
using System;
using System.Linq;
using System.Threading;
using T = System.Threading.Tasks;

namespace TestIteratorTasks
{
    public static class TaskExtensions
    {
        public static void Update(this TaskScheduler scheduler, int n)
        {
            for (int i = 0; i < n; i++)
            {
                scheduler.Update();
            }
        }

        public static void Wait(this Task t)
        {
            var e = new System.Threading.ManualResetEvent(false);

            t.ContinueWith(_ => e.Set());

            e.WaitOne();
        }

        /// <summary>
        /// スケジューラーのシャットダウンを開始する。
        /// Shutdown 呼びだし後、タイマーでしばらく Update を書ける。
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static T.Task BeginShutdown(this TaskScheduler scheduler, TimeSpan timeout)
        {
            var tcs = new T.TaskCompletionSource<object>();

            scheduler.Shutdown(_ => tcs.SetResult(null));

            scheduler.ShutdownTimeout = timeout;

            Timer timer = null;

            timer = new Timer(_ =>
            {
                if (scheduler.Status != TaskSchedulerStatus.Shutdown)
                {
                    timer.Dispose();
                    return;
                }

                scheduler.Update(10);
            }, null, 0, 1);

            return tcs.Task;
        }
    }
}
