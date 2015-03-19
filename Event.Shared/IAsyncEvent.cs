namespace System
{
    /// <summary>
    /// <see cref="IEvent{TArg}"/> の非同期対応亜種。
    /// イベントの Invoke を await しないと行けない場面で使う。
    /// </summary>
    /// <typeparam name="TArg">イベント引数の型。</typeparam>
    public interface IAsyncEvent<TArg>
    {
        /// <summary>
        /// ハンドラー追加。
        /// </summary>
        /// <param name="action"></param>
        void Add(AsyncHandler<TArg> action);

        /// <summary>
        /// ハンドラー削除。
        /// </summary>
        /// <param name="action"></param>
        void Remove(AsyncHandler<TArg> action);
    }
}
