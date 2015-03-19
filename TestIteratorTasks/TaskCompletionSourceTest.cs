using IteratorTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace TestIteratorTasks
{
    [TestClass]
    public class TaskCompletionSourceTest
    {
        [TestMethod]
        public void TaskCompletionSource_SetResultを使って任意タイミングで完了するTaskを作れる()
        {
            const int v1 = 0;
            const int v2 = 10;

            var tcs = new TaskCompletionSource<int>();

            var t = tcs.Task;
            int result = v1;

            t.ContinueWith(x => result = x.Result);

            Assert.AreEqual(v1, result);

            System.Threading.Tasks.Task.Delay(5).ContinueWith(_ =>
            {
                tcs.SetResult(v2);
            }).Wait();

            Task.DefaultScheduler.Update();

            Assert.AreEqual(v2, result);
        }

        [TestMethod]
        public void TaskCompletionSource_SetExceptionを使って任意タイミングで失敗するTaskを作れる()
        {
            var tcs = new TaskCompletionSource<int>();

            var t = tcs.Task;
            Exception e = null;

            t.ContinueWith(x => e = x.Exception);

            Assert.IsNull(e);

            System.Threading.Tasks.Task.Delay(5).ContinueWith(_ =>
            {
                tcs.SetException(new Exception());
            }).Wait();

            Task.DefaultScheduler.Update();

            Assert.IsNotNull(e);
        }

        [TestMethod]
        public void SetResultするまではRunnint状態_後はIsCompleted()
        {
            var tcs = new TaskCompletionSource<int>();

            var task = tcs.Task;
            CheckRunning(task);

            tcs.SetResult(100);

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(100, task.Result);
        }

        private static void CheckRunning(Task<int> task)
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(task.Status == TaskStatus.Running);
            }
        }

        [TestMethod]
        public void SetExceptionするまではRunnint状態_後はIsFaulted()
        {
            var tcs = new TaskCompletionSource<int>();

            var task = tcs.Task;
            CheckRunning(task);

            const string errorMessage = "some error";
            tcs.SetException(new Exception(errorMessage));

            Assert.IsTrue(task.IsFaulted);

            try
            {
                var result = task.Result;

                Assert.Fail("例外が発生していないとおかしい");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(errorMessage, ex.Message);
            }
        }

        [TestMethod]
        public void SetCanceldするまではRunnint状態_後はIsFaulted()
        {
            var tcs = new TaskCompletionSource<int>();

            var task = tcs.Task;
            CheckRunning(task);

            tcs.SetCanceled();

            Assert.IsTrue(task.IsFaulted);

            try
            {
                var result = task.Result;

                Assert.Fail("例外が発生していないとおかしい");
            }
            catch (IteratorTasks.AggregateException ex)
            {
                Assert.IsTrue(ex.Exceptions.First() is TaskCanceledException);
            }
        }
    }
}
