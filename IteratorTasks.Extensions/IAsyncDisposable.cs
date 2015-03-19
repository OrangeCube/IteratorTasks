using IteratorTasks;

namespace System
{
    /// <summary>
    /// <see cref="IDisposable"/> の非同期版。
    /// </summary>
    public interface IAsyncDisposable
    {
        /// <summary>
        /// 破棄処理。
        /// </summary>
        /// <returns></returns>
        Task DisposeAsync();
    }
}
