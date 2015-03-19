using IteratorTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace TestIteratorTasks
{
    [TestClass]
    public class CancellationTokenTest
    {
        [TestMethod]
        public void キャンセルトークンを渡しても_Cancelを呼ばなければ正常終了()
        {
            var x = 10;
            var scheduler = Task.DefaultScheduler;

            var t = Task.Run<double>(c => Coroutines.F1Cancelable(x, 20, c, CancellationToken.None));

            while (!t.IsCompleted)
                scheduler.Update();

            Assert.AreEqual(Coroutines.F1(x), t.Result);
        }

        [TestMethod]
        public void キャンセルしたときにOperationCanceld例外発生()
        {
            var x = 10;
            var scheduler = Task.DefaultScheduler;

            var cts = new CancellationTokenSource();
            var t = Task.Run<double>(c => Coroutines.F1Cancelable(x, 20, c, cts.Token));

            scheduler.Update(5);
            cts.Cancel();

            // 次の1回の実行でタスクが終わるはず
            scheduler.Update();

            // この場合は IsCanceled にならない
            Assert.IsTrue(t.IsFaulted);
            Assert.AreEqual(typeof(TaskCanceledException), t.Exception.Exceptions.Single().GetType());
        }

        [TestMethod]
        public void CancellationTokenを使うバージョンのRunでタスク開始するとTask_Cancel可能()
        {
            var x = 10;
            var scheduler = Task.DefaultScheduler;

            var cts = new CancellationTokenSource();
            var t = Task.Run<double>((c, ct) => Coroutines.F1Cancelable(x, 20, c, ct), cts);

            scheduler.Update(5);
            t.Cancel();

            // 次の1回の実行でタスクが終わるはず
            scheduler.Update();

            // この場合は IsCanceled にならない
            Assert.IsTrue(t.IsFaulted);
            Assert.AreEqual(typeof(TaskCanceledException), t.Exception.Exceptions.Single().GetType());
        }

        [TestMethod]
        public void Cancel時にRegisterで登録したデリゲートが呼ばれる()
        {
            var scheduler = Task.DefaultScheduler;

            {
                // キャンセルしない場合
                var cts = new CancellationTokenSource();
                var t = Task.Run<string>(c => Cancelで戻り値が切り替わるコルーチン(10, c, cts.Token));

                scheduler.Update(20);

                Assert.IsTrue(t.IsCompleted);
                Assert.AreEqual(CompletedMessage, t.Result);
            }

            {
                // キャンセルする場合
                var cts = new CancellationTokenSource();
                var t = Task.Run<string>(c => Cancelで戻り値が切り替わるコルーチン(10, c, cts.Token));

                scheduler.Update(5);
                cts.Cancel();
                scheduler.Update(5);

                Assert.IsTrue(t.IsCompleted);
                Assert.AreEqual(CanceledMessage, t.Result);
            }
        }

        const string CompletedMessage = "最後まで実行された時の戻り値";
        const string CanceledMessage = "キャンセルされた時の戻り値";

        static System.Collections.IEnumerator Cancelで戻り値が切り替わるコルーチン(int n, Action<string> completed, CancellationToken ct)
        {
            var message = CompletedMessage;

            ct.Register(() =>
            {
                message = CanceledMessage;
            });

            for (int i = 0; i < n; i++)
            {
                if (ct.IsCancellationRequested)
                    break;

                yield return null;
            }

            completed(message);
        }
    }
}
