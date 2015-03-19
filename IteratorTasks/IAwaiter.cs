using System;

namespace IteratorTasks
{
    /// <summary>
    /// await 可能なものに実装するインターフェイス。
    /// 本家 Task で言うところの awaitable/awaiter パターン用。
    /// こっちでは await は使えなくて、yield return で返す。
    /// <see cref="TaskScheduler"/> 内で、このインターフェイスが来たら「中断と再開」処理をやる。
    /// </summary>
    public interface IAwaiter
    {
        /// <summary>
        /// すでに完了済みかどうか。
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 非同期処理が終わった時のコールバックを呼んでもらう。
        /// </summary>
        /// <param name="continuation">継続処理。</param>
        void OnCompleted(Action continuation);
    }
}
