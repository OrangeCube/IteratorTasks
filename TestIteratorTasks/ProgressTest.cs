using IteratorTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestIteratorTasks
{
    [TestClass]
    public class ProgressTest
    {
        [TestMethod]
        public void Progressでは_Reportが呼ばれるたびにProgressChangedイベントが起こる()
        {
            var progress = new IteratorTasks.Progress<int>();
            var reportedItems = new List<int>();

            progress.ProgressChanged += i =>
            {
                reportedItems.Add(i);
            };

            var t = Task.Run<int>(c => 進捗報告付きのコルーチン(c, progress));

            var scheduler = Task.DefaultScheduler;

            // RunOnce 仕様のために、最初の1回だけ特殊(Update より前に Report 呼ばれる)
            // テスト仕様的には変だし何とかしたいけども
            scheduler.Update();

            for (int i = 1; i < 100; i++)
            {
                Assert.AreEqual(i, reportedItems.Count);
                scheduler.Update();
                Assert.AreEqual(i + 1, reportedItems.Count);
                Assert.AreEqual(i, reportedItems.Last());
            }
        }

        static System.Collections.IEnumerator 進捗報告付きのコルーチン(Action<int> completed, IteratorTasks.IProgress<int> progress)
        {
            for (int i = 0; i < 100; i++)
            {
                progress.Report(i);
                yield return null;
            }

            completed(100);
        }
    }
}
