using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IteratorTasks
{
    /// <summary>
    /// <see cref="Task"/> の実行スケジューラー。
    /// </summary>
    public class TaskScheduler
    {
        static int gid = 0;
        int id;

        /// <summary>
        /// 主に診断用。
        /// スケジューラーのID。
        /// </summary>
        public int Id { get { return id; } }

        /// <summary>
        /// <see cref="Id"/> を連番で振る。
        /// </summary>
        public TaskScheduler() { id = ++gid; }

        /// <summary>
        /// 主に診断用。
        /// ID を直接指定。
        /// </summary>
        internal TaskScheduler(int id) { this.id = id; }

        /// <summary>
        /// 実行中のタスク。
        /// </summary>
        private Task[] _runningTasks = new Task[0];

        /// <summary>
        /// サスペンド中（他のタスクの完了待ちとか）のタスク。
        /// </summary>
        private Task[] _suspendedTasks = null;

        /// <summary>
        /// ロック用オブジェクト
        /// </summary>
        private object sync = new object();

        /// <summary>
        /// ContinueWith とかで実行したい処理。
        /// コルーチンと違って即実行、同期処理。
        /// ただし、スタックトレースが深くなりすぎないように、一度リストに貯めて呼ぶ。
        /// </summary>
        private List<Action> _continuationActions = new List<Action>();

        private void Add(ref Task[] array, Task item)
        {
            lock (sync)
            {
                array = array == null ?
                    new[] { item } :
                    array.Concat(new[] { item }).ToArray();
            }
        }

        private void Remove(ref Task[] array, Task item)
        {
            lock (sync)
            {
                array = array.Where(x => x != item).ToArray();
            }
        }

        private void Move(ref Task[] source, ref Task[] destination, Task item)
        {
            lock (sync)
            {
                source = source.Where(x => x != item).ToArray();
                destination = destination == null ?
                    new[] { item } :
                    destination.Concat(new[] { item }).ToArray();
            }
        }

        private bool Exists(Task task)
        {
            lock (sync)
                return _runningTasks.Any(x => x == task) || (_suspendedTasks != null && _suspendedTasks.Any(x => x == task));
        }

        TaskSchedulerStatus _status = TaskSchedulerStatus.Running;

        /// <summary>
        /// 現在のスケジューラーの状態。
        /// </summary>
        public TaskSchedulerStatus Status
        {
            get { return _status; }
        }

        /// <summary>
        /// タスクをスケジューラーに登録。
        /// </summary>
        /// <param name="task"></param>
        public void QueueTask(Task task)
        {
            if (_status == TaskSchedulerStatus.Running)
            {
                if(!Exists(task))
                    Add(ref _runningTasks, task);
            }
            else
                throw new InvalidOperationException("can not start task on shutdown mode");
        }

        /// <summary>
        /// タスク完了トークンをスケジューラーに登録。
        /// 
        /// TaskCompletionSource 越しに作られるタスクは、実行自体はスケジューラーの管轄外ではあるものの、
        /// UnhandledException で未処理例外を拾うためには登録が必要。
        /// </summary>
        /// <param name="tcs"></param>
        public void QueueTask<T>(TaskCompletionSource<T> tcs)
        {
            lock (sync)
            {
                _tcsTasks.Add(tcs.Task);
            }

            var a = (IAwaiter)tcs.Task;
            a.OnCompleted(() =>
            {
                lock (sync)
                    _tcsTasks.Remove(tcs.Task);

                AddToErrorCheck(tcs.Task);
            });
        }

        List<Task> _tcsTasks = new List<Task>();

        /// <summary>
        /// シャットダウン開始。
        /// シャットダウンを始めると、新規タスクの追加ができなくなる。
        /// 現在実行中のタスクが全部終了するか、タイムアウトしたらcallbackが呼ばれる。
        /// </summary>
        /// <param name="callback"></param>
        public void Shutdown(Action<TaskSchedulerStatus> callback)
        {
            if (_status == TaskSchedulerStatus.Running)
            {
                _status = TaskSchedulerStatus.Shutdown;
                _shutdownBiginingTime = DateTime.Now;
                _shutdownCallback = callback;
            }
            else
                callback(_status);
        }

        private DateTime _shutdownBiginingTime;
        Action<TaskSchedulerStatus> _shutdownCallback;

        /// <summary>
        /// シャットダウン開始後、実行中のタスクの終了をどのくらいの時間待つか。
        /// この時間を過ぎると、強制終了。
        /// </summary>
        public TimeSpan ShutdownTimeout
        {
            get { return _shutdownTimeout; }
            set { _shutdownTimeout = value; }
        }
        TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(18);

        /// <summary>
        /// 現在実行中のタスク。
        /// </summary>
        public IEnumerable<Task> RunningTasks { get { return _runningTasks; } }

        /// <summary>
        /// 現在サスペンド状態のタスク。
        /// タスクの中で yield return IAwaiter すると、Awaiter の方が終わるまでタスクがサスペンドする。
        /// </summary>
        public IEnumerable<Task> SuspendedTasks { get { return _suspendedTasks; } }

        /// <summary>
        /// Running 状態のタスクが1つでもある。
        /// </summary>
        public bool IsActive
        {
            get { return _runningTasks != null && _runningTasks.Length != 0; }
        }

        /// <summary>
        /// Running もしくは Suspended なタスクが1つでもある。
        /// </summary>
        public bool HasAnyTask
        {
            get
            {
                return (_runningTasks != null && _runningTasks.Length != 0) || (_suspendedTasks != null && _suspendedTasks.Length != 0);
            }
        }


        /// <summary>
        /// RunningTasks と SuspendedTasks のすべてを強制キャンセルする。
        /// 強制キャンセルされたタスクはCompleteが呼ばれないので注意
        /// </summary>
        public void Cancel()
        {
            var tasks = RunningTasks;
            if(SuspendedTasks != null)
                tasks = tasks.Concat(SuspendedTasks);

            foreach (var t in tasks)
                t.TryCancel();
        }

        /// <summary>
        /// 1フレームに1回呼んでもらう前提のメソッド。
        /// </summary>
        public void Update()
        {
            try
            {
#if DEBUG
                if (_updating)
                    throw new InvalidOperationException("Update is not thread safe. Do not call Update from multiple threads");
                _updating = true;
#endif

                UpdateBody();
            }
            catch (Exception ex)
            {
                var d = _errorOnUpdate;
                if (d != null) d(ex);
            }
            finally
            {
#if DEBUG
                _updating = false;
                ++UpdateCount;
#endif
            }
        }

        private void UpdateBody()
        {
            PrepareRunTasks(_runningTasks);

            while (true)
            {
                // RunTask 中で新たに QueueTask されたタスクはそのフレーム中で実行したいので、こういうループで囲む。
                var running = GetUnupdatedTasks(_runningTasks);

                RunTasks(running);
                RunActionsIteration();
                CheckErrors();

                if (!running.Any())
                    break;
            }
            CheckShutdown();
        }

#if DEBUG
        // スケジューラーを間違って複数のスレッドから呼んでたときの挙動が意味不明すぎてデバッグがつらいので、怪しい動きになってた時に例外出す。
        volatile bool _updating;

        internal int UpdateCount { get; private set; }
#endif

        /// <summary>
        /// タスクの合間に（コルーチンでないただの）アクションを実行。
        /// スタックトレースを深くしないように、一度リストとして持っておいて実行。
        /// ContinueWith 用。
        /// </summary>
        /// <param name="action">実行したいアクション。</param>
        public void Post(Action action)
        {
            lock (_continuationActions)
            {
                _continuationActions.Add(action);
            }
        }

        const int IterationMax = 5;

        private void RunActionsIteration()
        {
            for (var i = 0; i < IterationMax; i++)
            {
                lock (_continuationActions)
                    if (_continuationActions.Count == 0)
                        return;

                RunActions();
            }
        }

        private void RunActions()
        {
            while (true)
            {
                Action[] actions;
                lock (_continuationActions)
                {
                    if (!_continuationActions.Any()) return;

                    actions = _continuationActions.ToArray();
                    _continuationActions.Clear();
                }

                try
                {
                    foreach (var action in actions)
                    {
                        action();
                    }
                }
                catch(Exception e)
                {
                    // todo ちゃんとハンドルできるようにする
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
        }

        private void CheckShutdown()
        {
            if (_status == TaskSchedulerStatus.Shutdown)
            {
                // しばらく実行。タスクが1つもなくなったら完了
                if (!_runningTasks.Any() && (_suspendedTasks == null || !_suspendedTasks.Any()) && !_tcsTasks.Any())
                {
                    _status = TaskSchedulerStatus.ShutdownCompleted;
                    var d = _shutdownCallback;
                    if (d != null) d(_status);

                    return;
                }

                // タイムアウトのチェック。タイムアウトしてたら強制終了。
                var now = DateTime.Now;
                var span = now - _shutdownBiginingTime;

                if (span > ShutdownTimeout)
                {
                    _status = TaskSchedulerStatus.ShutdownTimeout;

                    foreach (var t in _runningTasks) t.TryCancel();
                    if (_suspendedTasks != null)
                        foreach (var t in _suspendedTasks) t.TryCancel();

                    var d = _shutdownCallback;
                    if (d != null) d(_status);
                }
            }
        }

        private void PrepareRunTasks(IEnumerable<Task> tasks)
        {
            foreach (var t in tasks)
            {
                t._updated = false;
            }
        }

        private Task[] GetUnupdatedTasks(IEnumerable<Task> tasks)
        {
            return tasks.Where(t => !t._updated).ToArray();
        }

        private void RunTasks(IEnumerable<Task> tasks)
        {
            foreach (var t in tasks)
            {
                RunTask(t);
                t._updated = true;
            }
        }

        private bool _guardRunTask;

        private void RunTask(Task t)
        {
            if (_guardRunTask) throw new InvalidOperationException("RunTask is not thread safe. Do not call Update from multiple threads");

            _guardRunTask = true;

            try
            {
                RunTaskInternal(t);
            }
            finally
            {
                _guardRunTask = false;
            }
        }

        private void RunTaskInternal(Task t)
        {
            var e = (IEnumerator)t;

            if (e.MoveNext())
            {
                if (e.Current == null) return;

                var a = e.Current as IAwaiter;

                if (a != null)
                {
                    if (!a.IsCompleted)
                        Suspend(t, a);

                    return;
                }

                var en = e.Current as IEnumerator;

                if (en != null)
                {
                    var t1 = Task.Run(en, this);
                    a = (IAwaiter)t1;

                    if (!a.IsCompleted)
                        Suspend(t, a);
                }
            }
            else
            {
                Remove(ref _runningTasks, t);
                AddToErrorCheck(t);
            }
        }

        private void Suspend(Task local, IAwaiter a)
        {
            Move(ref _runningTasks, ref _suspendedTasks, local);

            a.OnCompleted(() =>
            {
                // タイマーとかの OnCompleted 待ちすることもあるので、
                // この中、別スレッドで呼ばれる可能性あり。
                if (_status != TaskSchedulerStatus.ShutdownTimeout)
                {
                    Move(ref _suspendedTasks, ref _runningTasks, local);
                }
            });
        }

        #region エラー チェック

        // 完了したタスクは一度 completed tasks リストに記録。
        // Update の最後でそのリストを調べて、Error が IsHandled でないものがあったら UnhandledException イベントを起こす。

        private void AddToErrorCheck(Task t)
        {
            lock (_completedTasks)
                _completedTasks.Add(t);
        }

        private void CheckErrors()
        {
            List<Task> removed;
            lock (_completedTasks)
            {
                removed = _completedTasks;
                _completedTasks = new List<Task>();
            }
            foreach (var t in removed)
            {
                CheckError(t);
            }
        }

        List<Task> _completedTasks = new List<Task>();

        private void CheckError(Task t)
        {
            if (t.Exception != null && !t.Exception.IsHandled)//!t.IsHandled)
            {
                t.Exception.IsHandled = true;
                var d = _unhandledException;
                if (d != null) d(t);
            }

            // todo: 未ハンドル例外がうまく取れないので仮にログ出すようにする
            //if (t.Error != null)
            //{
            //    foreach (var e in t.Error.Exceptions)
            //        OrangeCube.TSS.Diagnostics.Logger.Write("IsHandled:" + t.IsHandled + " " + e.ToString());
            //}
        }

        /// <summary>
        /// 誰にも処理されなかった例外があった時に起こすイベント。
        /// 
        /// スケジューラーの Update 内で、決定的なタイミングで呼ぼうとしてるがために、かえって使い勝手悪いかも。
        /// 本家 .NET 4 の Task クラスだと、Finalizer でチェックしてる（この場合、タスクが完了直後には呼ばれないし、タイミングも不定）。
        /// </summary>
        /// <remarks>
        /// 自動実装な event だと iOS で動作しないのでこういう作りに（他の同様の作りしてるものも同じ理由）。
        /// 
        /// 原因は C# 4.0 で生成されるコードが変わった（Interlocked.CompareExchange[T] を使った lock-free 同期）せい（Mono 2.6 の AOT の制限で、CompareExchange[T] 動かない）。
        /// 
        /// 対策としてこれまで、DLL は Unity 付属の MonoDevelop（Mono 2.6 だし、C# 3.0）でビルドしてた。
        /// 
        /// C# 3.0 だとデリゲートのオーバーロード解決ルールが馬鹿すぎてしんどい（list.Select(x => Method(x)) って書かなきゃいけない。4.0 なら Select(Method) 可能）ので、
        /// いっそのこと自動実装 event を辞めてみることに（こっちの方が利用頻度低い）。
        /// 
        /// Unity 前提なので、マルチスレッド動作は想定してない（lock とか使わない素の実装）。
        /// 
        /// 当然、こういう作りにしないと行けないのはゲーム本体で使うものだけ。
        /// Win 上の編集ツールだけで使うもの（ViewModels とか）に関しては自動実装 event でも問題出ない。
        /// </remarks>
        public event Action<Task> UnhandledException { add { _unhandledException += value; } remove { _unhandledException -= value; } }
        private Action<Task> _unhandledException;

        /// <summary>
        /// Update ループ中で意図しない例外が出てた時に呼ぶイベント。
        /// マルチスレッドがらみの競合とかでしか起きないと思う。
        /// 競合回避コードも書いてるつもりだけども、マルチスレッドがらみのバグは頑張ってもとり切れないときはとり切れないので。
        /// </summary>
        public event Action<Exception> ErrorOnUpdate { add { _errorOnUpdate += value; } remove { _errorOnUpdate -= value; } }
        private Action<Exception> _errorOnUpdate;

        #endregion
    }
}
