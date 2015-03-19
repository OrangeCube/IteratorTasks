using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IteratorTasks;

namespace TestIteratorTasks
{
    [TestClass]
    public class TaskTest
    {
        [TestMethod]
        public void Nフレーム実行するイテレーターが丁度N引く1フレームIsCompleted_falseになってることを確認()
        {
            // 開始直後に RunOnce する仕様になったので、Update ループ回数は1少ない
            const int N = 50;

            var task = Coroutines.NFrameTask(N);

            var scheduler = Task.DefaultScheduler;

            for (int i = 0; i < 2 * N; i++)
            {
                scheduler.Update();

                if (i < N)
                    Assert.IsFalse(task.IsCompleted, "expected false, but actual true on i = " + i);
                else
                    Assert.IsTrue(task.IsCompleted, "expected true, but actual false on i = " + i);
            }
        }

        [TestMethod]
        public void new_Taskでコールドスタート_Task_Runでホットスタート()
        {
            var x = 10;

            var t1 = new Task<double>(c => Coroutines.F1Async(x, c));

            Assert.AreEqual(TaskStatus.Created, t1.Status);

            t1.Start();

            Assert.AreEqual(TaskStatus.Running, t1.Status);

            var t2 = Task.Run<double>(c => Coroutines.F1Async(x, c));

            Assert.AreEqual(TaskStatus.Running, t2.Status);

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(10);

            Assert.AreEqual(TaskStatus.RanToCompletion, t1.Status);
            Assert.AreEqual(TaskStatus.RanToCompletion, t2.Status);
        }

        [TestMethod]
        public void Task_Tで正常終了するとResultに結果が入る()
        {
            var x = 10;
            var y = Coroutines.F1(x);

            var task = Task.Run<double>(c => Coroutines.F1Async(x, c))
                .OnComplete(t => Assert.AreEqual(t.Result, y));

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(10);

            Assert.AreEqual(y, task.Result);
        }

        [TestMethod]
        public void 一度完了したタスク_何度でも結果が取れる()
        {
            var x = 10;
            var y = Coroutines.F1(x);

            var task = Task.Run<double>(c => Coroutines.F1Async(x, c));

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(10);

            Assert.AreEqual(y, task.Result);
            Assert.AreEqual(y, task.Result);
            Assert.AreEqual(y, task.Result);
            Assert.AreEqual(y, task.Result);
        }

        [TestMethod]
        public void タスク完了時にContinueWithが呼ばれる()
        {
            var x = 10;
            var y = Coroutines.F1(x);

            bool called = false;

            var task = Task.Run<double>(c => Coroutines.F1Async(x, c));
            task.ContinueWith(t => called = true);

            Assert.IsFalse(called);

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(10);

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void エラー終了でもContinueWithが呼ばれる()
        {
            bool called = false;

            var task = Task.Run<int>(Coroutines.FErrorAsync);
            task.ContinueWith(t => called = true);

            Assert.IsFalse(called);

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(10);

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void CompletedTaskでContinueWithするとUpdateCountを進めずに処理できる()
        {
            int updateCount = 0;
            Task.CompletedTask.ContinueWith(t => updateCount = Task.DefaultScheduler.UpdateCount );

            Assert.AreEqual(updateCount, 0);
        }

        [TestMethod]
        public void 完了済みのタスクでContinueWithすると_次のUpdateでコールバックが呼ばれる()
        {
            var x = 10;
            var y = Coroutines.F1(x);

            var task = Task.Run<double>(c => Coroutines.F1Async(x, c));

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(10);

            Assert.IsTrue(task.IsCompleted);

            bool called = false;

            task.ContinueWith(t => called = true);
            scheduler.Update();

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void 開始前_実行中_正常終了_エラー終了_がわかる()
        {
            var scheduler = Task.DefaultScheduler;

            var x = 10.0;
            var task = Task.Run<double>(c => Coroutines.F1Async(x, c));

            Assert.AreEqual(TaskStatus.Running, task.Status);

            scheduler.Update(10);

            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);

            var errorTask = Task.Run(Coroutines.FErrorAsync);

            Assert.AreEqual(TaskStatus.Running, errorTask.Status);

            scheduler.Update(10);

            Assert.AreEqual(TaskStatus.Faulted, errorTask.Status);
        }

        [TestMethod]
        public void 実行途中のタスクを再スタートしようとしたら例外を出す()
        {
            var x = 10.0;
            var task = Task.Run<double>(c => Coroutines.F1Async(x, c));

            var scheduler = Task.DefaultScheduler;
            scheduler.Update();

            Assert.AreEqual(TaskStatus.Running, task.Status);

            try
            {
                task.Start();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void タスク中で例外が出たらErrorプロパティに例外が入る()
        {
            var task = Task.Run<int>(Coroutines.FErrorAsync)
                .OnComplete(t =>
                {
                    Assert.IsTrue(t.IsFaulted);
                    Assert.IsNotNull(t.Exception);
                });

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(20);
        }

        [TestMethod]
        public void タスクが正常終了した時だけThenが呼ばれる()
        {
            var value = 10.0;
            var t1 = Task.Run<double>(c => Coroutines.F1Async(value, c));

            double result1 = 0;
            int result2 = 0;

            t1.Then(x => result1 = x); // 呼ばれる

            var t2 = Task.Run<int>(Coroutines.FErrorAsync);

            t2.Then(x => result2 = -1); // 呼ばれない

            Assert.AreEqual(0.0, result1);
            Assert.AreEqual(0, result2);

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(20);

            Assert.AreEqual(Coroutines.F1(value), result1);
            Assert.AreEqual(0, result2);
        }

        [TestMethod]
        public void タスク中で例外が出たらOnErrorが呼ばれる_特定の型の例外だけ拾う()
        {
            var notSupportedCalled = false;
            var outOfRangeCalled = false;

            var task = Task.Run(Coroutines.FErrorAsync)
                .OnError<NotSupportedException>(e => notSupportedCalled = true) // 呼ばれる
                .OnError<IndexOutOfRangeException>(e => outOfRangeCalled = true); // 呼ばれない

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(20);

            Assert.IsTrue(notSupportedCalled);
            Assert.IsFalse(outOfRangeCalled);
        }

        [TestMethod]
        public void OnCompleteは_直前のタスク完了時_エラーも正常終了も_どちらも呼ばれる()
        {
            var errorTaskCalled = false;
            var normalTaskCalled = false;

            var normalTask = Task.Run(() => Coroutines.NFrame(5))
                .OnComplete(t => normalTaskCalled = true);
            var errorTask = Task.Run<int>(Coroutines.FErrorAsync)
                .OnComplete(t => errorTaskCalled = true);

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(20);

            Assert.IsTrue(normalTaskCalled);
            Assert.IsTrue(errorTaskCalled);
        }

        [TestMethod]
        public void タスク中で例外が出たときにResultをとろうとすると例外再スロー()
        {
            var task = Task.Run<int>(Coroutines.FErrorAsync)
                .ContinueWith(t =>
                {
                    Assert.IsTrue(t.IsFaulted);

                    try
                    {
                        var result = t.Result;
                    }
                    catch
                    {
                        return;
                    }
                    Assert.Fail();
                });

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(20);

            // catch して無視したので、task の Error は null のはず。
            Assert.IsNull(task.Exception);
        }

        [TestMethod]
        public void ContinueWithで継続処理を実行できる()
        {
            var x = 10.0;
            var x1 = Coroutines.F1(x);
            var x2 = Coroutines.F2(x1);
            var x3 = Coroutines.F3(x2);

            var task = Task.Run<double>(c => Coroutines.F1Async(x, c))
                .OnComplete(t => Assert.AreEqual(t.Result, x1))
                .ContinueWithIterator<string>((t, callback) => Coroutines.F2Async(t.Result, callback))
                .OnComplete(t => Assert.AreEqual(t.Result, x2))
                .ContinueWithIterator<int>((t, callback) => Coroutines.F3Async(t.Result, callback))
                .OnComplete(t => Assert.AreEqual(t.Result, x2))
                ;

            var scheduler = Task.DefaultScheduler;
            scheduler.Update(30);
        }
    }

    static class TaskTestExtensions
    {
        public static Task OnComplete(this Task t, Action<Task> a)
        {
            t.ContinueWith(a);
            return t;
        }
        public static Task<T> OnComplete<T>(this Task<T> t, Action<Task<T>> a)
        {
            t.ContinueWith(a);
            return t;
        }
    }
}
