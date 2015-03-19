#if UseIteratorTasks
using IteratorTasks;
#else
using System.Threading;
using System.Threading.Tasks;
#endif

using System.Disposables;

namespace System
{
    /// <summary>
    /// <see cref="IEvent{T}"/> と <see cref="Task"/> 相互運用がらみ拡張メソッド。
    /// </summary>
    public static partial class AysncEventTaskExtensions
    {
        #region Task 化、CancellationToken 化

        /// <summary>
        /// イベントが起きた時にキャンセルするキャンセルトークンに変換。
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static CancellationToken ToCancellationToken<TArg>(this IEvent<TArg> e)
        {
            var cts = new CancellationTokenSource();
            e.Subscribe(cts.Cancel);
            return cts.Token;
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, CancellationToken ct)
        {
            return FirstAsync(e, _ => true, ct);
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e)
        {
            return FirstAsync(e, CancellationToken.None);
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// predicate を満たすまでは何回でもイベントを受け取る。
        /// </summary>
        /// <typeparam name="TArg">イベント引数の型。</typeparam>
        /// <param name="e">イベント発生元。</param>
        /// <param name="predicate">受け取り条件。</param>
        /// <param name="ct">キャンセル用。</param>
        /// <returns>イベントが1回起きるまで待つタスク。</returns>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, Func<TArg, bool> predicate, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TArg>();

            IDisposable subscription = null;

            subscription = e.Subscribe((sender, args) =>
            {
                if (predicate(args))
                {
                    subscription.Dispose();
                    tcs.TrySetResult(args);
                }
            });

            if (ct != CancellationToken.None)
            {
                ct.Register(() =>
                {
                    subscription.Dispose();
                    tcs.TrySetCanceled();
                });
            }

            return tcs.Task;
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// predicate を満たすまでは何回でもイベントを受け取る。
        /// predicate が非同期処理なバージョン。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, Func<TArg, Task<bool>> predicate, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TArg>();

            IDisposable subscription = null;

            subscription = e.Subscribe((sender, args) =>
            {
                predicate(args).ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        subscription.Dispose();
                        tcs.TrySetResult(args);
                    }
                });
            });

            if (ct != CancellationToken.None)
            {
                ct.Register(() =>
                {
                    subscription.Dispose();
                    tcs.TrySetCanceled();
                });
            }

            return tcs.Task;
        }

        #endregion
        #region Subscribe

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Handler<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Action<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Action handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        #endregion
        #region Subscribe 非同期版

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, AsyncHandler<T> handler)
        {
            e.Add(handler);
            return Disposable.Create(() => e.Remove(handler));
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, Func<T, Task> handler)
        {
            return Subscribe(e, (_1, arg) => handler(arg));
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, Func<Task> handler)
        {
            return Subscribe(e, (_1, _2) => handler());
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, AsyncHandler<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, Func<T, Task> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, Func<Task> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        #endregion
    }
}
