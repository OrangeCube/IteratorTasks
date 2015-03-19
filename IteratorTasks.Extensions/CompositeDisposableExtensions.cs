using System.Collections.Generic;

namespace System.Reactive.Disposables
{
    /// <summary>
    /// <see cref="CompositeDisposable"/>に対する拡張メソッド。
    /// </summary>
    public static class CompositeDisposableExtensions
    {
        /// <summary>
        /// まとめて<see cref="IDisposable"/>を追加。
        /// </summary>
        /// <param name="composite"></param>
        /// <param name="items"></param>
        public static void AddRange(this CompositeDisposable composite, IEnumerable<IDisposable> items)
        {
            foreach (var i in items)
            {
                composite.Add(i);
            }
        }
    }
}
