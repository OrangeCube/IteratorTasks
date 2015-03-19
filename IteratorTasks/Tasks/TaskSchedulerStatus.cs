using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IteratorTasks
{
    /// <summary>
    /// <see cref="TaskScheduler"/> の状態。
    /// </summary>
    public enum TaskSchedulerStatus
    {
        /// <summary>
        /// 実行中。
        /// </summary>
        Running,

        /// <summary>
        /// シャットダウン処理に入った。
        /// (以降は新規タスクをもう受け付けないけども、今あるタスクを実行中。)
        /// </summary>
        Shutdown,

        /// <summary>
        /// シャットダウン処理完了。
        /// (内部のタスクがもうなくなった。)
        /// </summary>
        ShutdownCompleted,

        /// <summary>
        /// <see cref="Shutdown"/> に入ってから、一定時間経過してもまだ内部のタスクが終わらなかったらこの状態に。
        /// </summary>
        ShutdownTimeout,
    }
}
