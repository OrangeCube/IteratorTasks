// System.Threading.Tasks だけじゃなくて、Rx 系も互換ライブラリを

using System.Collections;
using System.Collections.Generic;

namespace System.Reactive.Disposables
{
    /// <summary>
    /// 同時に <see cref="IDisposable.Dispose"/> される複数の disposable をグループ化して管理するクラス。
    /// </summary>
    public class CompositeDisposable : IDisposable, IEnumerable<IDisposable>, IEnumerable
    {
        private List<IDisposable> _list = new List<IDisposable>();

        /// <summary>
        /// 追加。
        /// </summary>
        /// <param name="d"></param>
        public void Add(IDisposable d)
        {
            _list.Add(d);
        }

        /// <summary>
        /// 追加。
        /// </summary>
        /// <param name="onDipose">破棄処理。</param>
        public void Add(Action onDipose)
        {
            _list.Add(new ActionDisposer(onDipose));
        }

        /// <summary>
        /// グループ内の disposable をまとめて <see cref="IDisposable.Dispose"/>。
        /// </summary>
        public void Dispose()
        {
            foreach (var x in _list)
            {
                if (x != null)
                    x.Dispose();
            }
            _list.Clear();
        }

        IEnumerator<IDisposable> IEnumerable<IDisposable>.GetEnumerator() { return _list.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _list.GetEnumerator(); }
    }
}
