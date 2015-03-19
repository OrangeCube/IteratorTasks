using System.Disposables;

namespace System.Events
{
    /// <summary>
    /// <see cref="IEvent{T}"/>に対する拡張メソッド。
    /// </summary>
    public static class Event
    {
        /// <summary>
        /// <see cref="IEvent{TArg}"/> をデリゲートから作る。
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        public static IEvent<TArg> Create<TArg>(Func<HandlerList<TArg>, IDisposable> subscribe) { return new DelegateEvent<TArg>(subscribe); }

        /// <summary>
        /// イベント引数の型を変換。
        /// </summary>
        public static IEvent<TResult> Select<TSource, TResult>(this IEvent<TSource> source, Func<TSource, TResult> selector)
        {
            return Create<TResult>(e =>
            {
                return source.Subscribe((sender, arg) => e.Invoke(sender, selector(arg)));
            });
        }

        /// <summary>
        /// イベント引数を条件フィルタリング。
        /// </summary>
        public static IEvent<TSource> Where<TSource>(this IEvent<TSource> source, Func<TSource, bool> predicate)
        {
            return Create<TSource>(e =>
            {
                return source.Subscribe((sender, arg) => { if (predicate(arg)) e.Invoke(sender, arg); });
            });
        }

        /// <summary>
        /// object から具体的な型へのキャスト。
        /// 型が合わないイベントが来た時は例外。
        /// </summary>
        public static IEvent<T> Cast<T>(this IEvent<object> source)
        {
            return Create<T>(e =>
            {
                return source.Subscribe((sender, arg) => e.Invoke(sender, (T)arg));
            });
        }

        /// <summary>
        /// object から具体的な型への変換。
        /// <see cref="Cast{T}(IEvent{object})"/> と違って、型が合わないものは無視(イベントを起こさない)。
        /// </summary>
        public static IEvent<T> OfType<T>(this IEvent<object> source)
        {
            return Create<T>(e =>
            {
                return source.Subscribe((sender, arg) => e.Invoke(sender, (T)arg));
            });
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// </summary>
        public static IEvent<T> Merge<T>(this IEvent<T> source1, IEvent<T> source2)
        {
            return Merge(new[] { source1, source2 });
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// </summary>
        public static IEvent<T> Merge<T>(params IEvent<T>[] sources)
        {
            return Create<T>(list =>
            {
                var d = new CompositeDisposable();
                foreach (var s in sources)
                {
                    d.Add(s.Subscribe((sender, x) => list.Invoke(sender, x)));
                }
                return d;
            });
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// 型違い版。
        /// </summary>
        public static IEvent<object> Merge<T1, T2>(IEvent<T1> source1, IEvent<T2> source2)
        {
            return Merge<object>(
                source1.Select(x => (object)x),
                source2.Select(x => (object)x));
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// 型違い版。
        /// </summary>
        public static IEvent<object> Merge<T1, T2, T3>(IEvent<T1> source1, IEvent<T2> source2, IEvent<T3> source3)
        {
            return Merge<object>(
                source1.Select(x => (object)x),
                source2.Select(x => (object)x),
                source3.Select(x => (object)x));
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// 型違い版。
        /// </summary>
        public static IEvent<object> Merge<T1, T2, T3, T4>(IEvent<T1> source1, IEvent<T2> source2, IEvent<T3> source3, IEvent<T4> source4)
        {
            return Merge<object>(
                source1.Select(x => (object)x),
                source2.Select(x => (object)x),
                source3.Select(x => (object)x),
                source4.Select(x => (object)x));
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// 型違い版。
        /// </summary>
        public static IEvent<object> Merge<T1, T2, T3, T4, T5>(IEvent<T1> source1, IEvent<T2> source2, IEvent<T3> source3, IEvent<T4> source4, IEvent<T5> source5)
        {
            return Merge<object>(
                source1.Select(x => (object)x),
                source2.Select(x => (object)x),
                source3.Select(x => (object)x),
                source4.Select(x => (object)x),
                source5.Select(x => (object)x));
        }

        /// <summary>
        /// 複数のイベントを1つに併合。
        /// 型違い版。
        /// </summary>
        public static IEvent<object> Merge<T1, T2, T3, T4, T5, T6>(IEvent<T1> source1, IEvent<T2> source2, IEvent<T3> source3, IEvent<T4> source4, IEvent<T5> source5, IEvent<T6> source6)
        {
            return Merge<object>(
                source1.Select(x => (object)x),
                source2.Select(x => (object)x),
                source3.Select(x => (object)x),
                source4.Select(x => (object)x),
                source5.Select(x => (object)x),
                source6.Select(x => (object)x));
        }

        /// <summary>
        /// 何も起きないダミーイベント。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEvent<T> Empty<T>() { return EmptyEvent<T>.Singleton; }

        class EmptyEvent<T> : IEvent<T>
        {
            public static readonly EmptyEvent<T> Singleton = new EmptyEvent<T>();

            public IDisposable Subscribe(Handler<T> action)
            {
                return Disposable.Empty;
            }

            private static IDisposable e = new EmptyDisposer();

            private class EmptyDisposer : IDisposable
            {
                public void Dispose() { }
            }
        }
    }

    internal class DelegateEvent<TArg> : IEvent<TArg>
    {
        private Func<HandlerList<TArg>, IDisposable> _subscribe;
        public DelegateEvent(Func<HandlerList<TArg>, IDisposable> subscribe) { _subscribe = subscribe; }

        public IDisposable Subscribe(Handler<TArg> action)
        {
            var e = new HandlerList<TArg>();
            e.Subscribe(action);
            return _subscribe(e);
        }
    }
}
