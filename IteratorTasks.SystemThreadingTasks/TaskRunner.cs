using System;
using IT = IteratorTasks;
using ST = System.Threading;
using TT = System.Threading.Tasks;

namespace IteratorTasks
{
    /// <summary>
    /// スレッドプールで <see cref="IteratorTasks.TaskScheduler.Update"/> を回すクラス。
    /// 互換性用。
    /// 1 Runner 1 Scheduler なんだけど、1 Runner が複数持つように変えたいけど、既存のものはそっとしておきたいので別クラスを作った → <see cref="MultiTaskRunner"/>。
    /// </summary>
    public class TaskRunner
    {
        private TT.Task _task;
        private ST.CancellationTokenSource _cts = new ST.CancellationTokenSource();

        /// <summary>
        /// <see cref="IteratorTasks.TaskScheduler"/>
        /// </summary>
        public IT.TaskScheduler Scheduler { get; private set; }

        /// <summary>
        /// キャンセルトークン。
        /// </summary>
        public ST.CancellationToken Token { get { return _cts.Token; } }

        /// <summary>
        /// エラーが発生したかどうか。
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TaskRunner() : this(new IT.TaskScheduler()) { }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="scheduler"></param>
        public TaskRunner(IT.TaskScheduler scheduler)
        {
            Scheduler = scheduler;
            _task = UpdateLoop(scheduler);
        }

        private async TT.Task UpdateLoop(IT.TaskScheduler scheduler)
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var delayMilliseconds = scheduler.IsActive ? 5 : 50;
                    try
                    {
                        await TT.Task.Delay(delayMilliseconds, _cts.Token).ConfigureAwait(false);
                    }
                    catch (TT.TaskCanceledException) { }
                    // ↑Delay なし、専用スレッドで回りっぱなしとかがいいかもしれないし。

                    scheduler.Update();
                }
                catch (Exception ex)
                {
                    HasError = true;
                    OnError(ex);
                }
            }
        }

        /// <summary>
        /// タスクを止める。
        /// </summary>
        /// <returns></returns>
        public TT.Task Stop()
        {
            _cts.Cancel();
            return _task;
        }

        /// <summary>
        /// <see cref="System.Threading.Tasks.Task"/>
        /// </summary>
        public TT.Task Task { get { return _task; } }

        /// <summary>
        /// 例外発生時のイベント。
        /// </summary>
        public event EventHandler<Exception> Error;

        private void OnError(Exception ex)
        {
            var d = Error;
            if (d != null) d(this, ex);
        }
    }
}
