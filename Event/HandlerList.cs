using System.Disposables;
using System.Linq;

namespace System
{
    /// <summary>
    /// Rx でいう Subject : IObserver, IObservable。
    /// <see cref="IEvent{TArg}"/> の実装。
    /// <see cref="Handler{TArg}"/> を束ねるクラス。
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// public IEvent<T> SomeEvent { get { return _someEvent; } }
    /// private HandlerList<T> _someEvent = new HandlerList<T>();
    /// ]]>
    /// </example>
    /// <typeparam name="TArg">イベント引数の型。</typeparam>
    public class HandlerList<TArg> : IEvent<TArg>
    {
        private Handler<TArg>[] _list;
        private object _sync = new object();

        /// <summary>
        /// 誰かに購読されているかどうか。
        /// </summary>
        public bool HasAny { get { return _list != null && _list.Length != 0; } }

        /// <summary>
        /// イベントを通知。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void Invoke(object sender, TArg args)
        {
            // ↓これだけでいいはずなんだけども、たぶん iOS AOT で動かない。
            // var actions = System.Threading.Interlocked.Exchange(ref _list, null);
            // _sync が要らなかったら、単なる配列の薄いラッパーだし、struct にしたいんだけども。
            Handler<TArg>[] actions;
            lock (_sync)
            {
                actions = _list;
            }

            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a != null) a(sender, args);
            }
        }

        /// <summary>
        /// <see cref="IEvent{T}"/>
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IDisposable Subscribe(Handler<TArg> action)
        {
            Add(action);
            return Disposable.Create(() => Remove(action));
        }

        /// <summary>
        /// イベントを購読。
        /// </summary>
        /// <param name="action"></param>
        public void Add(Handler<TArg> action)
        {
            // こっちは CompareExchage
            lock (_sync)
            {
                _list = _list == null
                    ? new[] { action }
                    : _list.Concat(new[] { action }).ToArray();
            }
        }

        /// <summary>
        /// イベントを購読解除。
        /// </summary>
        /// <param name="action"></param>
        public void Remove(Handler<TArg> action)
        {
            lock (_sync)
            {
                _list = _list == null
                    ? new Handler<TArg>[] { }
                    : _list.Where(x => x != action).ToArray();
            }
        }
    }

    /// <summary>
    /// 非ジェネリック版。
    /// </summary>
    /// <remarks>
    /// <see cref="Handler{TArg}"/> とは別型にしたいものの、実装がほぼコピペの似て非なるコードになるのがつらすぎて断念した結果。
    /// void はジェネリックの敵。
    /// </remarks>
    public class HandlerList : HandlerList<Null>
    {
        /// <summary>
        /// イベントを通知。
        /// </summary>
        /// <param name="sender"></param>
        public void Invoke(object sender) { Invoke(sender, null); }
    }
}
