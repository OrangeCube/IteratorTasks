using System;

namespace IteratorTasks
{
    /// <summary>
    /// タスクのキャンセル用トークン。
    /// キャンセルを受ける側。
    /// </summary>
    public struct CancellationToken
    {
        private readonly CancellationTokenSource _source;

        internal CancellationToken(CancellationTokenSource source) { _source = source; }

        /// <summary>
        /// キャンセル要求が出ているかどうか。
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                if (_source == null)
                    return false;
                else
                    return _source.IsCancellationRequested;
            }
        }

        /// <summary>
        /// キャンセル要求時に通知を受け取るためのデリゲートを登録。
        /// </summary>
        /// <param name="onCanceled">キャンセル要求時に呼ばれるデリゲート。</param>
        public void Register(Action onCanceled)
        {
            if (_source != null)
            {
                if (_source.IsCancellationRequested) onCanceled();
                _source.Canceled += onCanceled;
            }
        }

        /// <summary>
        /// 空のトークン。
        /// </summary>
        public static CancellationToken None = new CancellationToken();

        /// <summary>
        /// キャンセル要求が出ている場合、OperationCanceledException をスローする。
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                var e = _source.CancelReason ?? new TaskCanceledException();
                throw e;
            }
        }

#pragma warning disable 1591
        public static bool operator ==(CancellationToken x, CancellationToken y) { return x._source == y._source; }
        public static bool operator !=(CancellationToken x, CancellationToken y) { return x._source != y._source; }
        public override bool Equals(object obj) { return obj is CancellationToken && _source == ((CancellationToken)obj)._source; }
        public override int GetHashCode() { return _source.GetHashCode(); }
#pragma warning restore
    }
}
