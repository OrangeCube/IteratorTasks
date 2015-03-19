using T = System.Threading;
using TT = System.Threading.Tasks;
using System.Linq;

namespace IteratorTasks
{
    /// <summary>
    /// IteratorTasks と .NET 標準 Task の interop 用拡張メソッド群。
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// <see cref="IteratorTasks.TaskScheduler"/> をスレッド プール上で Update する。
        /// </summary>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static TaskRunner BeginUpdate(this TaskScheduler scheduler)
        {
#if false
            // 複数個所で同じスケジューラーに対してこいつを呼ばれるとやばいので、変な挙動した場合、ここの ifdef true にして呼び出し元探してみる。
            var s = new System.Diagnostics.StackTrace();
            var sid = scheduler.Id;
            System.Diagnostics.Debug.WriteLine("scheduler " + sid + "runs on " + s.ToString());
#endif

            return new TaskRunner(scheduler);
        }

        /// <summary>
        /// <see cref="TaskScheduler"/> に対して
        /// 未処理例外が来た時に <see cref="System.Diagnostics.Debug"/> でログ出力。
        /// </summary>
        /// <param name="scheduler"></param>
        public static void DebugWriteOnUnhandledException(this TaskScheduler scheduler)
        {
            scheduler.UnhandledException += scheduler_UnhandledException;
        }

        private static void scheduler_UnhandledException(Task obj)
        {
            var e = obj.Exception;
            var a = e as AggregateException;

            if (a == null || !a.Exceptions.Any(x => x is TaskCanceledException))
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Task の
        /// System.Threading.Tasks → IteratorTasks。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task ToIterator(this TT.Task t, TaskScheduler scheduler)
        {
            var tcs = new TaskCompletionSource<object>(scheduler);
            t.ContinueWith(x =>
                {
                    if (x.IsCompleted)
                        tcs.SetResult(null);
                    else if (x.IsFaulted)
                        tcs.SetException(x.Exception);
                    else
                        tcs.SetCanceled();
                });
            return tcs.Task;
        }

        /// <summary>
        /// Task の
        /// System.Threading.Tasks → IteratorTasks。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task<T> ToIterator<T>(this TT.Task<T> t, TaskScheduler scheduler)
        {
            var tcs = new TaskCompletionSource<T>(scheduler);
            t.ContinueWith(x =>
                {
                    if (x.IsCanceled)
                        tcs.SetCanceled();
                    else if (x.IsFaulted)
                        tcs.SetException(x.Exception);
                    else
                        tcs.SetResult(x.Result);
                });
            return tcs.Task;
        }

        /// <summary>
        /// Task の
        /// IteratorTasks → System.Threading.Tasks。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static TT.Task ToTpl(this Task t)
        {
            var tcs = new TT.TaskCompletionSource<bool>();
            t.ContinueWith(x =>
                {
                    if (x.IsCanceled)
                        tcs.SetCanceled();
                    else if (x.IsFaulted)
                        tcs.SetException(x.Exception);
                    else
                        tcs.SetResult(false);
                });
            return tcs.Task;
        }

        /// <summary>
        /// Task の
        /// IteratorTasks → System.Threading.Tasks。
        /// System.Threading.Tasks → IteratorTasks。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static TT.Task<T> ToTpl<T>(this Task<T> t)
        {
            var tcs = new TT.TaskCompletionSource<T>();
            t.ContinueWith(x =>
                {
                    if (x.IsCanceled)
                        tcs.SetCanceled();
                    else if (x.IsFaulted)
                        tcs.SetException(x.Exception);
                    else
                        tcs.SetResult(t.Result);
                });
            return tcs.Task;
        }

        /// <summary>
        /// CancellationToken の
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static CancellationToken ToIterator(this T.CancellationToken ct)
        {
            var cts = new CancellationTokenSource();
            ct.Register(() => cts.Cancel());
            return cts.Token;
        }

        /// <summary>
        /// CancellationToken の
        /// IteratorTasks → System.Threading.Tasks。
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static T.CancellationToken ToTpl(this CancellationToken ct)
        {
            var cts = new T.CancellationTokenSource();
            ct.Register(() => cts.Cancel());
            return cts.Token;
        }

        /// <summary>
        /// キャンセルを待つだけのタスクを作る。
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static TT.Task ToTask(this T.CancellationToken ct)
        {
            var tcs = new TT.TaskCompletionSource<bool>();
            ct.Register(() => tcs.SetResult(false));
            return tcs.Task;
        }

        /// <summary>
        /// IProgress の
        /// IteratorTasks → System.Threading.Tasks。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static System.IProgress<T> ToTpl<T>(this IProgress<T> progress)
        {
            var p = new System.Progress<T>();
            p.ProgressChanged += (sender, x) => progress.Report(x);
            return p;
        }
    }

}
