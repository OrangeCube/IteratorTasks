// System.Threading.Tasks だけじゃなくて、Rx 系も互換ライブラリを


namespace System.Reactive.Disposables
{
    /// <summary>
    /// <see cref="IDisposable"/> 関連、ユーティリティ メソッドや拡張。
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// Action から <see cref="IDisposable"/> を作る。
        /// </summary>
        /// <param name="dispose"><see cref="IDisposable.Dispose"/> で呼びたい処理。</param>
        /// <returns><see cref="IDisposable"/> 化したもの。</returns>
        public static IDisposable Create(Action dispose)
        {
            return new ActionDisposer(dispose);
        }

        /// <summary>
        /// 何もしないダミー。
        /// </summary>
        public readonly static IDisposable Empty = new EmptyDisposer();
    }

    internal class ActionDisposer : IDisposable
    {
        Action _onDispose;

        public ActionDisposer(Action onDispose)
        {
            if (onDispose == null)
                throw new ArgumentNullException();
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }

    internal class EmptyDisposer : IDisposable
    {
        public void Dispose() { }
    }
}
