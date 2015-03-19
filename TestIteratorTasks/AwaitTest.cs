using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IteratorTasks;
using System.Collections;

namespace TestIteratorTasks
{
    [TestClass]
    public class AwaitTest
    {
        [TestMethod]
        public void コルーチンで指定フレームちゃんと待つかどうか()
        {

            var scheduler = Task.DefaultScheduler;
            var task = Task.Run<int>(Await5Frame);

            while(!task.IsCompleted)
                scheduler.Update();
            Assert.AreEqual(task.Result, 6);       // yield returnで1フレーム経過するので+1
        }

        private IEnumerator Await5Frame(Action<int> completedFrameCount)
        {
            var s = Task.DefaultScheduler.UpdateCount;
            yield return Coroutines.NFrame(5);
            completedFrameCount(Task.DefaultScheduler.UpdateCount - s);
        }
    }
}
