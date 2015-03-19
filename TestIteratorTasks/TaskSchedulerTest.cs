using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IteratorTasks;
using System.Collections.Generic;

namespace TestIteratorTasks
{
    [TestClass]
    public class TaskSchedulerTest
    {
        [TestMethod]
        public void スケジューラーは明示的に指定可能()
        {
            const int N = 10;

            var s1 = new TaskScheduler();
            var s2 = new TaskScheduler();

            var t1 = Util.DelayFrame(N, s1);
            var t2 = Util.DelayFrame(N, s2);

            // s1 だけ何回 Update しても、s2 側を使って起動したタスクは終わらない。
            s1.Update(N + 1);

            Assert.IsTrue(t1.IsCompleted);
            Assert.IsFalse(t2.IsCompleted);

            // s2 も動かして初めてt2は終わる。
            s2.Update(N + 1);

            Assert.IsTrue(t2.IsCompleted);
        }

        [TestMethod]
        public void TaskScheduler_Shutdownで登録済みのタスクの終了待ちできる()
        {
            var scheduler = new TaskScheduler();
            var skipScheduler = new TaskScheduler();
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var t = Util.RandomTask(scheduler, skipScheduler);
                tasks.Add(t);
                scheduler.Update();
                skipScheduler.Update();
            }

            // RandomTask と WaitShutdown の実装上、5秒も待てば終わるはず
            var s = scheduler.BeginShutdown(TimeSpan.FromSeconds(10));

            while (true)
            {
                if (s.IsFaulted || s.IsCompleted || s.IsCanceled) break;
                for (int i = 0; i < 50; i++) scheduler.Update();
                for (int i = 0; i < 5; i++) skipScheduler.Update();
                System.Threading.Thread.Sleep(1);
            }

            Assert.AreEqual(TaskSchedulerStatus.ShutdownCompleted, scheduler.Status);

            foreach (var t in tasks)
            {
                Assert.AreEqual(TaskStatus.RanToCompletion, t.Status);
            }
        }

        [TestMethod]
        public void TaskSchedulerのシャットダウンにはタイムアウトを設定できる()
        {
            var scheduler = new TaskScheduler();

            Util.Delay(50, scheduler);

            // 50ミリ秒のDelayは100ミリ秒も待てば終わるはず。
            scheduler.BeginShutdown(TimeSpan.FromMilliseconds(100)).Wait();

            Assert.AreEqual(TaskSchedulerStatus.ShutdownCompleted, scheduler.Status);

            scheduler = new TaskScheduler();

            Util.Delay(200, scheduler);

            // 100ミリ秒のDelayは50ミリ秒で終わらずタイムアウトするはず。
            scheduler.BeginShutdown(TimeSpan.FromMilliseconds(50)).Wait();

            Assert.AreEqual(TaskSchedulerStatus.ShutdownTimeout, scheduler.Status);
        }

        [TestMethod]
        public void タスク中で例外が発生した時_UnhandledExceptionイベントが発生()
        {
            var scheduler = Task.DefaultScheduler;

            bool flag;
            scheduler.UnhandledException += _ => flag = true;

            // 例外が発生したタスクをほっておくと、UnhandledException イベントが発生。
            flag = false;
            var t1 = Task.Run(Coroutines.FErrorAsync);
            scheduler.Update(50);
            Assert.IsTrue(flag);

            // ContinueWith とかでの例がいも同様の扱い。
            flag = false;
            var t2 = Task.Run<double>(c => Coroutines.F1Async(10, c))
                .ContinueWith(_ => { throw new Exception(); });
            scheduler.Update(50);
            Assert.IsTrue(flag);

            flag = false;
            var t3 = Task.Run<double>(c => Coroutines.F1Async(10, c))
                .ContinueWithTask(_ => Task.Run(Coroutines.FErrorAsync));
            scheduler.Update(50);
            Assert.IsTrue(flag);

            // ContinueWith とかで Error の IsHandled を立てると UnhandledException は発生しない。
            flag = false;
            var t4 = Task.Run(Coroutines.FErrorAsync)
                .ContinueWith(t => { var e = t.Exception; e.IsHandled = true; });
            Assert.IsFalse(flag);
        }
    }
}
