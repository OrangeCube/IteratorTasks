using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using IteratorTasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;


namespace TestIteratorTasks
{
    [TestClass]
    public class TaskWhenAnyTest
    {
        private int?[] iteratorCount = new int?[3];

        [TestMethod()]
        public void WhenAny後にContinueWithでフレームが進むかどうか()
        {
            int?[] continueWithCount = new int?[4];
            int? completedCount = null;
            int? runCompletedCount = null;

            var t0 = Task.Run(TestIteratorZero);
            var t1 = Task.Run(TestIteratorOne);
            var t2 = Task.Run(TestIteratorTwo);

            // yield return の後のUpdateCountの値を確かめる。
            Task.WhenAny(t0).ContinueWith(_ => continueWithCount[0] = Task.DefaultScheduler.UpdateCount);
            Task.WhenAny(t1).ContinueWith(_ => continueWithCount[1] = Task.DefaultScheduler.UpdateCount);
            Task.WhenAny(t2).ContinueWith(_ => continueWithCount[2] = Task.DefaultScheduler.UpdateCount);

            ///複数タスクを入れた時に最初に終わるタスクと同時に終わるかどうか。
            Task.WhenAny(t0, t1, t2).ContinueWith(_ => continueWithCount[3] = Task.DefaultScheduler.UpdateCount);

            // CompletedTaskのcontinueWithは、Updatecountを挟まず処理できる。
            Task.WhenAny(Task.CompletedTask).ContinueWith(_ => completedCount = Task.DefaultScheduler.UpdateCount);

            // Task.Run(CompletedTask)の場合でも、UpdateCountを挟まず処理できる。
            Task.WhenAny(Task.Run(Task.CompletedTask)).ContinueWith(_ => runCompletedCount = Task.DefaultScheduler.UpdateCount);


            Task.DefaultScheduler.Update(10);

            Assert.AreEqual(iteratorCount[0], continueWithCount[0]);
            Assert.AreEqual(iteratorCount[1], continueWithCount[1]);
            Assert.AreEqual(iteratorCount[2], continueWithCount[2]);
            Assert.AreEqual(iteratorCount[0], continueWithCount[3]);
            Assert.AreEqual(0, completedCount);
            Assert.AreEqual(0, runCompletedCount);
        }

        private IEnumerator TestIteratorZero()
        {
            iteratorCount[0] = Task.DefaultScheduler.UpdateCount;
            yield break;
        }

        private IEnumerator TestIteratorOne()
        {
            yield return null;

            iteratorCount[1] = Task.DefaultScheduler.UpdateCount;
        }

        private IEnumerator TestIteratorTwo()
        {
            yield return null;
            yield return null;

            iteratorCount[2] = Task.DefaultScheduler.UpdateCount;
        }

        /// <summary>
        /// Update1回経過後にエラーを出す
        /// </summary>
        /// <returns></returns>
        private IEnumerator TestIteratorThrowExceptionOne()
        {
            yield return null;
            throw new Exception("throw error on one");


        }

        [TestMethod()]
        public void WhenAnyに入れたタスクが例外を出した場合に正常に終わるかどうか()
        {
            int?[] continueWithCount = new int?[3];
            var scheduler = new TaskScheduler();

            var te1 = Task.Run(TestIteratorThrowExceptionOne, scheduler);
            var t0 = Task.Run(TestIteratorZero, scheduler);
            var t1 = Task.Run(TestIteratorOne, scheduler);
            var t2 = Task.Run(TestIteratorTwo, scheduler);

            var a0 = Task.WhenAny(scheduler, null, t0, te1);
            a0.ContinueWith(_ => continueWithCount[0] = scheduler.UpdateCount);
            var a1 = Task.WhenAny(scheduler, null, t1, te1);
            a1.ContinueWith(_ => continueWithCount[1] = scheduler.UpdateCount);
            var a2 = Task.WhenAny(scheduler, null, t2, te1);
            a2.ContinueWith(_ => continueWithCount[2] = scheduler.UpdateCount);

            scheduler.Update(10);

            Assert.AreEqual(0, continueWithCount[0]);
            Assert.AreEqual(1, continueWithCount[1]);
            Assert.AreEqual(1, continueWithCount[2]);
            Assert.IsNull(a0.Result.Exception);
            Assert.AreEqual(a1.Result.Exception.Message, "throw error on one");
            Assert.AreEqual(a2.Result.Exception.Message, "throw error on one");
        }

    }
}
