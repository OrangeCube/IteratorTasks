using System.Disposables;

namespace System
{
    public static partial class EventExtensions
    {
        /// <summary>
        /// 任意のイベントなどから指定した型の<see cref="IEvent{T}"/>に変換する。
        /// </summary>
        /// <typeparam name="TEventArg"></typeparam>
        /// <typeparam name="TEventHandler"></typeparam>
        /// <param name="addHandler"></param>
        /// <param name="removeHandler"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static IEvent<TEventArg> FromEventPattern<TEventArg, TEventHandler>(
            Action<TEventHandler> addHandler,
            Action<TEventHandler> removeHandler,
            Func<Handler<TEventArg>, TEventHandler> converter)
        {
            return new DelegateEventHandler<TEventArg, TEventHandler>(addHandler, removeHandler, converter);
        }
    }

    class DelegateEventHandler<TEventArg, TEventHandler> : IEvent<TEventArg>
    {
        private readonly Action<TEventHandler> _addHandler;
        private readonly Action<TEventHandler> _removeHandler;
        private readonly Func<Handler<TEventArg>, TEventHandler> _converter;

        public DelegateEventHandler(Action<TEventHandler> addHandler, Action<TEventHandler> removeHandler, Func<Handler<TEventArg>, TEventHandler> converter)
        {
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _converter = converter;
        }

        public IDisposable Subscribe(Handler<TEventArg> action)
        {
            var h = _converter(action);
            _addHandler(h);
            return Disposable.Create(() => _removeHandler(h));
        }
    }
}
