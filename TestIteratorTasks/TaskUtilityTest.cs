using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IteratorTasks;
using System.Threading;

namespace TestIteratorTasks
{
    [TestClass]
    public class TaskUtilityTest
    {
        [TestMethod]
        public void タスクの2重起動防止()
        {
            const int Frame = 30;
            const int N = 10;

            var scheduler = new TaskScheduler();

            Func<Task> starter = () => Util.DelayFrame(Frame, scheduler);
            var distinct = TaskUtility.Distinct(starter);

            Task task = distinct();

            for (int i = 0; i < (Frame + 1) * N; i++)
            {
                var t = distinct();

                if (task.IsCompleted)
                {
                    // タスク完了してたら次のタスクを起動してるはず。
                    Assert.IsFalse(object.ReferenceEquals(task, t));

                    task = t;
                }
                else
                {
                    // タスク完了するまでは同じタスクが何度も帰ってくるはず。
                    Assert.IsTrue(object.ReferenceEquals(task, t));
                }

                scheduler.Update();
            }
        }

        [TestMethod]
        public void タスクのタイムアウト()
        {
            const int N = 10;
            const int T1 = 50;
            const int T2 = 100;

            var scheduler = new TaskScheduler();

            // 50秒でタイムアウトのものを100秒放置して、タイムアウトさせる
            {
                var t = TaskUtility.RunWithTimeout(ct => Util.DelayFrame(N, ct, scheduler), TimeSpan.FromMilliseconds(T1));
                Thread.Sleep(T2);

                scheduler.Update(N + 1);

                Assert.IsTrue(t.IsFaulted);
                Assert.IsTrue(t.Exception.Exceptions.Any(x => x is TimeoutException));
            }

            // 100秒でタイムアウトのものを50秒放置後、ちゃんと完了させる
            {
                var t = TaskUtility.RunWithTimeout(ct => Util.DelayFrame(N, ct, scheduler), TimeSpan.FromMilliseconds(T2));
                Thread.Sleep(T1);

                scheduler.Update(N + 1);

                Assert.IsTrue(t.IsCompleted);
            }

            // 50秒でタイムアウトのものを100秒放置して、タイムアウトさせる
            {
                var t = TaskUtility.RunWithTimeout(ct => Util.Delay(T2, ct), TimeSpan.FromMilliseconds(T1));
                while (!t.IsCompleted)
                {
                    scheduler.Update();
                    Thread.Sleep(1);
                }

                Assert.IsTrue(t.IsFaulted);
            }

            // 100秒でタイムアウトのものを50秒放置後、ちゃんと完了させる
            {
                var t = TaskUtility.RunWithTimeout(ct => Util.Delay(T1, ct), TimeSpan.FromMilliseconds(T2));
                while (!t.IsCompleted)
                {
                    scheduler.Update();
                    Thread.Sleep(1);
                }

                Assert.IsTrue(t.IsCompleted);
            }
        }
    }
}
