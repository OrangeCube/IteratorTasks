using System;

namespace IteratorTasks
{
    /// <summary>
    /// タスクのキャンセル用トークン。
    /// キャンセルする側。
    /// </summary>
    public class CancellationTokenSource
    {
        /// <summary>
        /// 初期化。
        /// </summary>
        public CancellationTokenSource()
        {
            Token = new CancellationToken(this);
        }

        /// <summary>
        /// キャンセル用トークン。
        /// </summary>
        public CancellationToken Token { get; private set; }

        /// <summary>
        /// キャンセル要求を出したかどうか。
        /// </summary>
        public bool IsCancellationRequested { get; private set; }

        /// <summary>
        /// キャンセル。
        /// </summary>
        public void Cancel()
        {
            if (IsCancellationRequested) return;

            var d = _canceled;
            if (d != null) d();
            d = null;

            IsCancellationRequested = true;
        }

        /// <summary>
        /// キャンセルの原因となる例外を指定してのキャンセル要求。
        /// </summary>
        public void Cancel(Exception cancelReason)
        {
            _cancelReason = cancelReason;

            this.Cancel();
        }

        internal event Action Canceled
        {
            add
            {
                if (IsCancellationRequested)
                {
                    if(value != null)
                        value();
                }
                else
                {
                    _canceled += value;
                }
            }
            remove { _canceled -= value; }
        }
        private Action _canceled;

        private Exception _cancelReason;
        internal Exception CancelReason { get { return _cancelReason; } }
    }
}
