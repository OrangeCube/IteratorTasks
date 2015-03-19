//#define CaptureStackTrace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IteratorTasks
{
    /// <summary>
    /// .NET 4 の Task 的にコルーチンを実行するためのクラス。
    /// 戻り値なし版。
    /// </summary>
    public partial class Task : IAwaiter, IEnumerator, IDisposable
    {
        /// <summary>
        /// TaskScheduler で使うフラグ。
        /// RunTask の直前で落として、RunTask 実行後に立てる。
        /// あんまり internal にしたくないけども妥協。
        /// </summary>
        internal bool _updated;

#if CaptureStackTrace && DEBUG
        System.Diagnostics.StackTrace _stackTrace;
#endif

        internal Task()
        {
#if CaptureStackTrace && DEBUG
            _stackTrace = new System.Diagnostics.StackTrace();
#endif
        }

        /// <summary>
        /// イテレーターからタスクを作る。
        /// </summary>
        /// <param name="routine"></param>
        public Task(IEnumerator routine) : this()
        {
            if (routine == null)
                throw new ArgumentNullException("routine");

            Status = TaskStatus.Created;
            Routine = routine;
        }

        /// <summary>
        /// イテレーター生成メソッドを渡してタスクを作る。
        /// </summary>
        /// <param name="routine"></param>
        public Task(Func<IEnumerator> routine) : this()
        {
            Status = TaskStatus.Created;
            try
            {
                Routine = routine();
            }
            catch (Exception e)
            {
                AddError(e);
                Complete();
            }
        }

        private void RunOnce()
        {
            try
            {
                if (!Routine.MoveNext())
                {
                    Complete();
                    return;
                }
                Status = TaskStatus.Running;
                _firstRunning = true;
            }
            catch (Exception e)
            {
                AddError(e);
                Complete();
            }
        }

        /// <summary>
        /// 中で動いているコルーチン。
        /// </summary>
        protected IEnumerator Routine { get; set; }

        /// <summary>
        /// タスク実行中に発生した例外。
        /// </summary>
        public AggregateException Exception { get; private set; }

        /// <summary>
        /// 例外を追加。
        /// <see cref="TaskCompletionSource{T}"/> とかで、例外を伝搬させるために使う。
        /// </summary>
        /// <param name="exc"></param>
        protected void AddError(Exception exc)
        {
            var agg = exc as AggregateException;
            if (agg != null)
            {
                if (Exception == null)
                    Exception = agg;
                else
                    Exception.Merge(agg);
            }
            else
            {
                if (Exception == null)
                    Exception = new AggregateException(exc);
                else
                    Exception.Merge(exc);
            }
        }

        /// <summary>
        /// タスクの現在の状態。
        /// </summary>
        public TaskStatus Status { get; protected internal set; }

        // こういうフラグわざわざ持ちたくないけども、
        // Start 直後から TaskStatus.Running にして、かつ、Start 時点で RunOnce にしようと思うと必要。
        // 今のところ他にいい方法思いつかない。
        private bool _firstRunning;

        /// <summary>
        /// 正常・例外問わず、完了済みかどうか。
        /// </summary>
        public bool IsCompleted { get { return Status == TaskStatus.RanToCompletion || IsCanceled || IsFaulted; } }

        /// <summary>
        /// キャンセルされて終了した。
        /// </summary>
        public bool IsCanceled { get { return Status == TaskStatus.Canceled; } }

        /// <summary>
        /// 例外が出て終了した。
        /// </summary>
        public bool IsFaulted { get { return Status == TaskStatus.Faulted; } }

        bool IAwaiter.IsCompleted { get { return IsCompleted; } }

        void IAwaiter.OnCompleted(Action continuation)
        {
            if (IsCompleted)
                Scheduler.Post(continuation);
            else
            {
                lock (_callback)
                {
                    _callback.Add(continuation);
                }
            }
        }

        List<Action> _callback = new List<Action>();

        object IEnumerator.Current
        {
            get
            {
                if (IsCanceled) return null;
                return Routine == null ? null : Routine.Current;
            }
        }

        bool IEnumerator.MoveNext()
        {
            if (Status == TaskStatus.Created || _firstRunning)
            {
                Status = TaskStatus.Running;
                _firstRunning = false;

                // 最初の一回、Start 時に RunOnce して、そこで MoveNext が false ならもう RanToEnd になってるはずなので。
                return true;
            }

            if (Status != TaskStatus.Running) return false;
            if (Routine == null) return false;

            bool hasNext;

            try
            {
                hasNext = Routine.MoveNext();
            }
            catch (Exception exc)
            {
                AddError(exc);
                hasNext = false;
            }

            if (!hasNext)
            {
                Complete();
            }
            return hasNext;
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            var d = Routine as IDisposable;
            if (d != null) d.Dispose();
            Routine = null;
        }

        /// <summary>
        /// 終了処理。
        /// </summary>
        protected void Complete()
        {
            if (Status == TaskStatus.Canceled)
                return;

            if (Exception != null)
                Status = TaskStatus.Faulted;
            else
                Status = TaskStatus.RanToCompletion;

            var d = _beforeCallback;
            if (d != null) d();

            lock (_callback)
            {
                if (_callback.Count != 0)
                {
                    foreach (var c in _callback)
                    {
                        Scheduler.Post(c);
                    }
                }
                _callback.Clear();
            }
        }

        internal event Action BeforeCallback { add { _beforeCallback += value; } remove { _beforeCallback -= value; } }
        private Action _beforeCallback;

        /// <summary>
        /// このタスクをキャンセルするためのトークン。
        /// </summary>
        public CancellationTokenSource Cancellation { get; private set; }

        /// <summary>
        /// タスクをキャンセルします。
        /// CancellationToken 越しにキャンセルするので、コルーチン側が対応していない場合には即座にはタスク終了しない。
        /// Task を作るときに CancellationToken を渡していないものに対して呼び出すと例外発生。
        /// </summary>
        /// <remarks>
        /// 注: 本家 Task にはないもの。
        /// </remarks>
        public void Cancel()
        {
            if (Cancellation == null)
                throw new InvalidOperationException("Can't cancel Task.");

            Cancellation.Cancel();
            ((IEnumerator)this).MoveNext();
        }

        /// <summary>
        /// タスクをキャンセルします。
        /// Task を作るときに CancellationToken を渡していないものに対して呼び出すと何もしない。
        /// </summary>
        /// <remarks>
        /// 注: 本家 Task にはないもの。
        /// </remarks>
        public void TryCancel()
        {
            if (Cancellation == null)
                return;

            Cancellation.Cancel();
            ((IEnumerator)this).MoveNext();
        }

        /// <summary>
        /// Cancel と同様。
        /// キャンセル理由を指定できるバージョン。
        /// </summary>
        /// <param name="e">キャンセル理由。</param>
        public void Cancel(Exception e)
        {
            if (Cancellation == null)
                throw new InvalidOperationException("Can't cancel Task.");

            Cancellation.Cancel(e);

            ((IEnumerator)this).MoveNext();
        }

        /// <summary>
        /// 既定のタスクスケジューラー。
        /// </summary>
        public static TaskScheduler DefaultScheduler { get { return ___xxx; } } private static TaskScheduler ___xxx = new TaskScheduler(-1);

        /// <summary>
        /// このタスクに紐づいているスケジューラー。
        /// </summary>
        public TaskScheduler Scheduler { get { return _scheduler ?? DefaultScheduler; } }
        internal TaskScheduler _scheduler;

        /// <summary>
        /// タスク開始。
        /// new Task(ルーチン) しただけだと開始してない状態（cold start）なので、Startを呼ぶ必要がある。
        /// Task.Run(ルーチン) なら介した状態（hot start）のタスクが返る。
        /// </summary>
        public void Start()
        {
            Start(DefaultScheduler);
        }

        /// <summary>
        /// スケジューラーを明示してタスク開始。
        /// </summary>
        /// <param name="scheduler"></param>
        public void Start(TaskScheduler scheduler)
        {
            if (Status == TaskStatus.Created)
            {
                if (scheduler == null)
                    throw new ArgumentNullException();

                RunOnce();
                scheduler.QueueTask(this);
                _scheduler = scheduler;
            }
            else
                throw new InvalidOperationException();
        }

        /// <summary>
        /// 開始済みのタスクを作る。
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static Task Run(IEnumerator routine) { return Run(routine, null); }

        /// <summary>
        /// スケジューラーを指定して開始済みのタスクを作る。
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task Run(IEnumerator routine, TaskScheduler scheduler)
        {
            var t = new Task(routine);
            t.Start(scheduler ?? DefaultScheduler);
            return t;
        }

        /// <summary>
        /// 開始済みのタスクを作る。
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static Task Run(Func<IEnumerator> routine) { return Run(routine, null); }

        /// <summary>
        /// 開始済みのタスクを作る。
        /// キャンセル可能。
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public static Task Run(Func<CancellationToken, IEnumerator> routine, CancellationTokenSource cts) { return Run(routine, cts, null); }

        /// <summary>
        /// 開始済みのタスクを作る。
        /// キャンセル可能。
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="cts"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task Run(Func<CancellationToken, IEnumerator> routine, CancellationTokenSource cts, TaskScheduler scheduler)
        {
            var t = Run(() => routine(cts.Token), scheduler);
            t.Cancellation = cts;
            return t;
        }

        /// <summary>
        /// スケジューラーを指定して開始済みのタスクを作る。
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task Run(Func<IEnumerator> routine, TaskScheduler scheduler)
        {
            var t = new Task(routine);
            if (!t.IsCompleted) t.Start(scheduler ?? DefaultScheduler);
            return t;
        }

        /// <summary>
        /// イテレーターを渡して開始済みのタスクを作る。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(Func<Action<T>, IEnumerator> routine) { return Run(routine, null); }

        /// <summary>
        /// イテレーターを渡して開始済みのタスクを作る。
        /// キャンセル可能。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routine"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(Func<Action<T>, CancellationToken, IEnumerator> routine, CancellationTokenSource cts) { return Run(routine, cts, null); }

        /// <summary>
        /// イテレーターを渡して開始済みのタスクを作る。
        /// キャンセル可能。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routine"></param>
        /// <param name="cts"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(Func<Action<T>, CancellationToken, IEnumerator> routine, CancellationTokenSource cts, TaskScheduler scheduler)
        {
            var t = Run<T>(callback => routine(callback, cts.Token), scheduler);
            t.Cancellation = cts;
            return t;
        }

        /// <summary>
        /// イテレーターを渡して開始済みのタスクを作る。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routine"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(Func<Action<T>, IEnumerator> routine, TaskScheduler scheduler)
        {
            var t = new Task<T>(routine);
            if (!t.IsCompleted) t.Start(scheduler ?? DefaultScheduler);
            return t;
        }

        /// <summary>
        /// ただの値をタスク化。
        /// 作ったタスクは、最初から完了済みで、Result で値を取れる。
        /// </summary>
        public static Task<T> FromResult<T>(T value) { return new Task<T>(value); }

        /// <summary>
        /// 完了済みのタスクを取得する。
        /// </summary>
        /// <remarks>
        /// その時点でのDefaultSchedularを拾う。
        /// (毎回新しいタスクインスタンスを作成する)
        /// </remarks>
        public static Task CompletedTask
        {
            get
            {
                return Task.FromResult<object>(default(object)); ;
            }
        }

        /// <summary>
        /// 例外を直接渡して、最初から完了済みのタスクを作る。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="error"></param>
        /// <returns></returns>
        public static Task<T> FromException<T>(Exception error)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(error);
            return tcs.Task;
        }

        /// <summary>
        /// 例外を直接渡して、最初から完了済みのタスクを作る。
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static Task FromException(Exception error)
        {
            return FromException<object>(error);
        }

        /// <summary>
        /// 複数のタスクが完了するのを待つタスクを作る。
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        /// <remarks>
        /// foreach しかしてないのに IEnumerable 版がないのは、遅延評価されたくないから。
        /// </remarks>
        public static Task WhenAll(params Task[] tasks)
        {
            int count = tasks.Length;
            if (count == 0) return Task.FromResult<object>(null);

            var tcs = new TaskCompletionSource<object>();
            AggregateException ex = new AggregateException();
            var isAllErrorHandled = true;
            foreach (var t in tasks)
            {
                var tLocal = t;
                Action callback = null;
                callback = () =>
                {
                    tLocal.BeforeCallback -= callback;
                };
                tLocal.BeforeCallback += callback;

                var a = (IAwaiter)tLocal;
                a.OnCompleted(() =>
                {
                    if (tLocal.IsFaulted)
                    {
                        if (!tLocal.Exception.IsHandled)
                            isAllErrorHandled = false;
                        tLocal.Exception.IsHandled = true;
                        lock (ex) ex.Merge(tLocal.Exception);
                    }

                    System.Threading.Interlocked.Decrement(ref count);

                    if (count == 0)
                    {
                        if (ex.Exceptions.Any())
                        {
                            ex.IsHandled = isAllErrorHandled;
                            tcs.SetException(ex);
                        }
                        else
                            tcs.SetResult(null);
                    }
                });
            }

            return tcs.Task;
        }

        /// <summary>
        /// <see cref="ContinueWith(Action{Task})"/> の内部実装。
        /// <see cref="Task"/> と <see cref="Task{T}"/> での共通処理。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="starter"></param>
        /// <returns></returns>
        protected Task<T> ContinueWithInternal<T>(Func<Task> starter)
        {
            var tcs = new TaskCompletionSource<T>();

            var a1 = (IAwaiter)this;

            a1.OnCompleted(() =>
            {
                try
                {
                    var continuation = starter();
                    var a2 = (IAwaiter)continuation;

                    a2.OnCompleted(() => tcs.Propagate(continuation));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// <see cref="ContinueWith(Action{Task})"/> の内部実装。
        /// <see cref="Task"/> と <see cref="Task{T}"/> での共通処理。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        protected Task<T> ContinueWithInternal<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>(Scheduler);

            var a1 = (IAwaiter)this;

            a1.OnCompleted(() =>
            {
                if (this.IsCanceled)
                {
                    tcs.SetCanceled();
                    return;
                }

                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// タスク完了後に別の処理を行う。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public Task ContinueWith(Action<Task> func)
        {
            return ContinueWithInternal<object>(() => { func(this); return default(object); });
        }

        /// <summary>
        /// タスク完了後に別の処理を行う。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public Task<T> ContinueWith<T>(Func<Task, T> func)
        {
            return ContinueWithInternal<T>(() => func(this));
        }

        /// <summary>
        /// タスク完了後に別の処理を行う。
        /// イテレーターを渡して新しいタスクを起動。
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public Task ContinueWithIterator(Func<Task, IEnumerator> routine)
        {
            return ContinueWithInternal<object>(() => Task.Run(() => routine(this), this.Scheduler));
        }

        /// <summary>
        /// タスク完了後に別の処理を行う。
        /// イテレーターを渡して新しいタスクを起動。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routine"></param>
        /// <returns></returns>
        public Task<T> ContinueWithIterator<T>(Func<Task, Action<T>, IEnumerator> routine)
        {
            return ContinueWithInternal<T>(() => Task.Run<T>(callback => routine(this, callback), this.Scheduler));
        }

        /// <summary>
        /// タスク完了後に別の処理を行う。
        /// タスク開始用のメソッドを渡して、新しいタスクを起動。
        /// </summary>
        /// <param name="starter"></param>
        /// <returns></returns>
        public Task ContinueWithTask(Func<Task, Task> starter)
        {
            return ContinueWithInternal<object>(() => starter(this));
        }

        /// <summary>
        /// タスク完了後に別の処理を行う。
        /// タスク開始用のメソッドを渡して、新しいタスクを起動。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="starter"></param>
        /// <returns></returns>
        public Task<T> ContinueWithTask<T>(Func<Task, Task<T>> starter)
        {
            return ContinueWithInternal<T>(() => starter(this));
        }

        /// <summary>
        /// 例外に処理済みフラグを立てる。
        /// </summary>
        [Obsolete("Exception.IsHandled を使って")]
        public bool IsHandled { get { return Exception == null || Exception.IsHandled; } set { if (Exception != null) Exception.IsHandled = value; } }
    }
}
