using System;
using System.Collections.Generic;
using System.Linq;

#if UseIteratorTasks
using IteratorTasks;
#else
using System.Threading;
using System.Threading.Tasks;
#endif

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
    public class AsyncHandlerList<TArg> : IAsyncEvent<TArg>
    {
        private AsyncHandler<TArg>[] _list;
        private object _sync = new object();

        /// <summary>
        /// 1つでもハンドラーが刺さってたら true。
        /// </summary>
        public bool HasAny { get { return _list != null && _list.Length != 0; } }

        /// <summary>
        /// イベントを起こす。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
#if !UseIteratorTasks
        async
#endif

        public Task InvokeAsync(object sender, TArg args)
        {
            // ↓これだけでいいはずなんだけども、たぶん iOS AOT で動かない。
            // var actions = System.Threading.Interlocked.Exchange(ref _list, null);
            // _sync が要らなかったら、単なる配列の薄いラッパーだし、struct にしたいんだけども。
            AsyncHandler<TArg>[] actions;
            lock (_sync)
            {
                actions = _list;
            }

#if UseIteratorTasks
            if (actions == null) return Task.CompletedTask;

            var task = default(Task);

            foreach (var a in actions)
            {
                if (a == null) continue;

                if (task == null) task = a(sender, args);
                else task = task.ContinueWithTask(_ => a(sender, args));
            }
            return task;
#else
            if (actions != null) 
                foreach (var a in actions)
                    await a(sender, args);
#endif
        }

        /// <summary>
        /// <see cref="IAsyncEvent{TArg}.Add(AsyncHandler{TArg})"/>
        /// </summary>
        /// <param name="action"></param>
        public void Add(AsyncHandler<TArg> action)
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
        /// <see cref="IAsyncEvent{TArg}.Remove(AsyncHandler{TArg})"/>
        /// </summary>
        /// <param name="action"></param>
        public void Remove(AsyncHandler<TArg> action)
        {
            lock (_sync)
            {
                _list = _list == null
                    ? new AsyncHandler<TArg>[] { }
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
    public class AsyncHandlerList : AsyncHandlerList<Null>
    {
        /// <summary>
        /// <see cref="AsyncHandlerList{TArg}.InvokeAsync(object, TArg)"/>
        /// 引数を渡す必要がないので、省略した版。
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public Task InvokeAsync(object sender) { return InvokeAsync(sender, null); }
    }
}
