
namespace IteratorTasks
{
    /// <summary>
    /// <see cref="TaskCompletionSource{T}"/> の拡張。
    /// </summary>
    public static class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// <paramref name="ct"/> がキャンセルされたときに自動的にこちらもキャンセルされる <see cref="TaskCompletionSource{T}"/> を作る。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tcs"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static TaskCompletionSource<T> WithCancellation<T>(this TaskCompletionSource<T> tcs, CancellationToken ct)
        {
            if (ct != CancellationToken.None)
                ct.Register(tcs.SetCanceled);

            return tcs;
        }
    }
}
