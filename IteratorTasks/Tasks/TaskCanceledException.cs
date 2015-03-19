using System;

namespace IteratorTasks
{
    /// <summary>
    /// タスクがキャンセルされた。
    /// </summary>
    public class TaskCanceledException : OperationCanceledException
    {
        /// <summary>
        /// 何も指定なし。
        /// </summary>
        public TaskCanceledException() { }

        /// <summary>
        /// メッセージ指定。
        /// </summary>
        /// <param name="message"></param>
        public TaskCanceledException(string message) : base(message) { }

        /// <summary>
        /// メッセージと内部例外指定。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public TaskCanceledException(string message, Exception innerException) : base(message, innerException) { }
    }
}
