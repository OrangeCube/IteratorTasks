namespace System
{
    /// <summary>
    /// Rx でいう IObservable。
    /// Rx が使えるならこんなクラス自前で用意しないのに。的なやつ。
    /// 標準の event が使いにくいので。
    /// <seealso cref="HandlerList{TArg}"/>
    /// </summary>
    /// <typeparam name="TArg">イベント引数の型。</typeparam>
    public interface IEvent<TArg>
    {
        /// <summary>
        /// イベントを購読。
        /// </summary>
        /// <param name="action">ハンドラー。</param>
        /// <returns>イベント購読解除用の<see cref="IDisposable"/></returns>
        IDisposable Subscribe(Handler<TArg> action);
    }
}
