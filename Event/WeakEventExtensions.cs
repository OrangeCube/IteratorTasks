using System.Disposables;

namespace System
{
    /// <summary>
    /// 弱参照版イベント Subscribe。
    /// </summary>
    /// <remarks>
    /// <see cref="WeakSubscribe{T}(IEvent{T}, Handler{T})"/> の戻り値の <see cref="IDisposable"/> が GC されたあと、最初のイベント発火のタイミングで unsubscribe される。
    /// 極力は自力で <see cref="IDisposable.Dispose"/> すべき(GCの間隔は意外と長くて、その間、無駄にイベント飛ぶ)。
    /// あくまで救済策。
    /// </remarks>
    public static class WeakEventExtensions
    {
        /// <summary>
        /// 弱イベント参照。
        /// 戻り値の <see cref="IDisposable"/> を参照しているものが誰からも参照されなくなったら自動的にイベント購読解除する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        /// <remarks>
        /// <see cref="WeakReference"/> を使ってる。
        /// こいつの性質上、<see cref="GC"/> が走って初めて「誰も使っていない」判定を受ける。
        /// それまでは普通にイベントが飛んでくる。
        /// GC タイミングに左右される挙動とか推奨はできないんで、可能な限り明示的に <see cref="IDisposable.Dispose"/> を呼ぶべき。
        /// </remarks>
        public static IDisposable WeakSubscribe<T>(this IEvent<T> e, Handler<T> handler)
        {
            WeakReference subscription = null;
            IDisposable unsubscribe = null;

            unsubscribe = e.Subscribe((sender, arg) =>
            {
                var x = subscription.Target as IDisposable;
                if (x == null)
                {
                    Diagnostics.Debug.WriteLine("Warning: An event was automatically unsubsctibed on GC by WeakReference. It is recommended to be manually disposed.");
                    unsubscribe.Dispose();
                    return;
                }

                handler(sender, arg);
            });

            var d = Disposable.Create(unsubscribe.Dispose);
            subscription = new WeakReference(d);

            return d;
        }

        /// <summary>
        /// 弱イベント参照。
        /// 戻り値の <see cref="IDisposable"/> を参照しているものが誰からも参照されなくなったら自動的にイベント購読解除する。
        /// </summary>
        public static IDisposable WeakSubscribe<T>(this IEvent<T> e, Action<T> handler) { return e.WeakSubscribe((sender, arg) => handler(arg)); }
        /// <summary>
        /// 弱イベント参照。
        /// 戻り値の <see cref="IDisposable"/> を参照しているものが誰からも参照されなくなったら自動的にイベント購読解除する。
        /// </summary>
        public static IDisposable WeakSubscribe<T>(this IEvent<T> e, Action handler) { return e.WeakSubscribe((sender, arg) => handler()); }
    }
}
