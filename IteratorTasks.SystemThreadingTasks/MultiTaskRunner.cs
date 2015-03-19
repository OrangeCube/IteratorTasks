using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using IT = IteratorTasks;
using ST = System.Threading;
using TT = System.Threading.Tasks;

namespace IteratorTasks
{
    /// <summary>
    /// 複数のスケジューラーを、同時に1個ずつしか動かさない保証付で Update を掛けるクラス。
    /// あくまで、このランナーに登録したスケジューラーは1度に1個しか動かないというだけで、実行スレッドはころころ変わるので注意(Sleep じゃなくて await Delay してるので)。
    /// もちろん、別ランナーに登録したスケジューラーは同時に複数動く。
    /// </summary>
    public class MultiTaskRunner
    {
        private TT.Task _task;
        private volatile bool _isAlive;
        private ST.CancellationTokenSource _cts = new ST.CancellationTokenSource();

        public IEnumerable<IT.TaskScheduler> Schedulers { get { return _schedulers; } }
        private ImmutableList<IT.TaskScheduler> _schedulers = ImmutableList<IT.TaskScheduler>.Empty;
        public ST.CancellationToken Token { get { return _cts.Token; } }

        public bool HasError { get; private set; }

        public MultiTaskRunner()
        {
            _task = UpdateLoop();
        }

        private async TT.Task UpdateLoop()
        {
            _isAlive = true;
            while (_isAlive)
            {
                var any = false;

                foreach (var s in _schedulers)
                {
                    try
                    {
                        s.Update();
                    }
                    catch (Exception ex)
                    {
                        HasError = true;
                        OnError(ex, s);
                    }
                    any |= s.IsActive;
                }

                var delayMilliseconds = any ? 1 : 50;
                await TT.Task.Delay(delayMilliseconds).ConfigureAwait(false);
            }
        }

        public TT.Task Stop()
        {
            _cts.Cancel();
            _isAlive = false;
            return _task;
        }

        public void Add(IT.TaskScheduler s)
        {
            _schedulers = _schedulers.Add(s);
        }

        public void Remove(IT.TaskScheduler s)
        {
            _schedulers = _schedulers.Remove(s);
        }

        public TT.Task Task { get { return _task; } }

        public event EventHandler<ErrorHandlerAgs> Error;

        public class ErrorHandlerAgs
        {
            public Exception Error { get; private set; }
            public IT.TaskScheduler Scheduler { get; private set; }

            public ErrorHandlerAgs(Exception error, IT.TaskScheduler scheduler)
            {
                Error = error;
                Scheduler = scheduler;
            }
        }

        private void OnError(Exception ex, IT.TaskScheduler scheduler)
        {
            var d = Error;
            if (d != null) d(this, new ErrorHandlerAgs(ex, scheduler));
        }
    }
}
