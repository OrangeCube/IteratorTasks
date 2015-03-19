using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// <see cref="IEvent{T}"/> 関連拡張メソッド。
    /// </summary>
    public static partial class EventExtensions
    {
        /// <summary>
        /// イベントを購読する。
        /// </summary>

        public static IDisposable Subscribe<T>(this IEvent<T> e, Action<T> handler)
        {
            return e.Subscribe((_1, arg) => handler(arg));
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IEvent<T> e, Action handler)
        {
            return e.Subscribe((_1, _2) => handler());
        }

        #region object から具体的な型へのキャスト

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <remarks>
        /// <paramref name="e"/> の Subscribe に対して Dispose するすべがないんで戻り値で返したイベントの寿命に注意。
        /// </remarks>
        [Obsolete("Use the System.Events.Event.Cast instead")]
        public static IEvent<T> Cast<T>(this IEvent<object> e)
        {
            var h = new HandlerList<T>();
            e.Subscribe((sender, arg) => h.Invoke(sender, (T)arg));
            return h;
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IEvent<object> e, Handler<T> handler)
        {
            Handler<object> objHandler = (sender, arg) =>
            {
                if (arg is T)
                    handler(sender, (T)arg);
            };
            return e.Subscribe(objHandler);
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IEvent<object> e, Action<T> handler)
        {
            return e.Subscribe((_1, arg) =>
            {
                if (arg is T)
                    handler((T)arg);
            });
        }

        #endregion
    }
}
