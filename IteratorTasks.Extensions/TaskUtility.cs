using System;
using System.Collections;
using System.Threading;

namespace IteratorTasks
{
    /// <summary>
    /// Task 本体をコンパクトにおさめたかったので外だししたいくつかの機能。
    /// </summary>
    /// <remarks>
    /// 特に、タイマー関連は、マルチスレッドになってしまうので Unity とは相性よくないかも。
    /// Unity は yield return new WaitForSeconds(t); とか使ってほしそうだし。
    /// </remarks>
    public static partial class TaskUtility
    {
        /// <summary>
        /// 二重起動防止付きのタスク生成関数を返す。
        /// 
        /// ・内部的にタスクをキャッシュ
        /// ・そのタスクが null の時だけ新規タスク起動
        /// ・完了とともにタスク キャッシュを null に戻す。
        /// ・起動中（Running）のタスク キャッシュがすでにある場合はそれを返す。
        /// </summary>
        /// <param name="starter">新規タスク起動用の関数。</param>
        /// <returns>二重起動防止付きのタスク生成関数。</returns>
        public static Func<Task> Distinct(Func<Task> starter)
        {
            Task task = null;

            return () =>
            {
                if (task == null)
                {
                    task = starter();
                    task.ContinueWith(t => task = null);
                }
                return task;
            };
        }

        /// <summary>
        /// 失敗しても指定回数リトライするタスクを作る。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="starter">タスクを開始する出理ゲート。</param>
        /// <param name="retryCount">リトライ回数。</param>
        /// <returns></returns>
        public static Task<T> Retry<T>(Func<Task<T>> starter, int retryCount)
        {
            var tcs = new TaskCompletionSource<T>();
            Task<T> task = null;

            Action a = null;

            a = () =>
            {
                task = starter();

                ((IAwaiter)task).OnCompleted(() =>
                {
                    if (task.Status == TaskStatus.RanToCompletion) tcs.SetResult(task.Result);
                    else if (retryCount == 0)
                    {
                        tcs.SetException(task.Exception);
                    }
                    else
                    {
                        retryCount--;
                        a();
                    }
                });
            };

            a();

            return tcs.Task;
        }

        /// <summary>
        /// 指定した<see cref="TimeSpan"/>後にキャンセルされる<see cref="Task"/>を作成する。
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task RunWithTimeout(Func<CancellationToken, IEnumerator> routine, TimeSpan timeout)
        {
            var cts = CancelAfter(timeout);
            return Task.Run(routine, cts);
        }

        /// <summary>
        /// 指定した<see cref="TimeSpan"/>後にキャンセルされる<see cref="Task{T}"/>を作成する。
        /// </summary>
        /// <param name="starter"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task RunWithTimeout(Func<CancellationToken, Task> starter, TimeSpan timeout)
        {
            var cts = CancelAfter(timeout);
            return starter(cts.Token);
        }

        /// <summary>
        /// タイムアウトつきのタスクを作る。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routine"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(Func<Action<T>, CancellationToken, IEnumerator> routine, TimeSpan timeout)
        {
            var cts = CancelAfter(timeout);
            return Task.Run<T>(routine, cts);
        }

        /// <summary>
        /// 一定時間後にキャンセル。
        /// ただし、タスク t が Cancellation を持っている前提。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="timeout"></param>
        public static void CancelAfter(this Task t, TimeSpan timeout)
        {
            DelayTimer(timeout, () => t.Cancel(new TimeoutException()));
        }

        /// <summary>
        /// 一定時間後にキャンセルされる CancellationTokenSource を作る。
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static CancellationTokenSource CancelAfter(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            DelayTimer(timeout, () => cts.Cancel(new TimeoutException()));
            return cts;
        }

        /// <summary>
        /// キャンセル伝搬しつつ、タイムアウト時にキャンセルするctsを作る。
        /// </summary>
        public static CancellationTokenSource CancelAfter(this CancellationToken ct, TimeSpan timeout)
        {
            var cts = ct.ToCancellationTokenSourceOneWay();
            DelayTimer(timeout, () => cts.Cancel(new TimeoutException()));
            return cts;
        }

        private static void DelayTimer(TimeSpan timeout, Action callback)
        {
            DelayTimer((int)timeout.TotalMilliseconds, callback);
        }

        private static void DelayTimer(int milliSecond, Action callback)
        {
            Timer timer = null;

            timer = new Timer(_ =>
            {
                timer.Dispose();
                callback();
            }, null, milliSecond, Timeout.Infinite);
        }
    }
}
