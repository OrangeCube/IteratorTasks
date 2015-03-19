using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using IteratorTasks;
using System.Threading;
using System.Linq;

namespace TestIteratorTasks
{
    [TestClass]
    public class AwaiterTest
    {
        [TestMethod]
        public void IAwaiterをyield_returnして返せば_Awaiter側完了までいったんTaskをサスペンド()
        {
            var values = Enumerable.Range(0, 100).Select(x => 10 * x).ToArray();

            var tasks = values.Select(x => Task.Run<int>(c => Delay4Times(x, c))).ToArray();

            var t = Task.WhenAll(tasks);

            var scheduler = Task.DefaultScheduler;

            while (true)
            {
                if (t.IsCompleted)
                    break;

                scheduler.Update();
                _s2.Update();

                Thread.Sleep(1);
            }

            // 取りこぼし残ってないか確認
            Assert.AreEqual(0, scheduler.RunningTasks.Count());
            Assert.AreEqual(0, scheduler.SuspendedTasks.Count());

            // 全部のタスクが終わっているか確認
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                var task = tasks[i];

                Assert.IsTrue(task.IsCompleted);
                Assert.AreEqual(v, task.Result);
            }
        }

        Random rnd = new Random();

        IEnumerator Delay4Times(int x, Action<int> callback)
        {
            for (int i = 0; i < 4; i++)
            {
                // 5～10ミリ秒のサスペンド
                yield return Util.Delay(rnd.Next(5, 10));

                // 5～10フレームのサスペンド
                yield return DelayFrame(rnd.Next(5, 10));

                // その後、5～10フレーム空アップデート
                var n = rnd.Next(5, 10);
                for (int j = 0; j < n; j++) yield return null;
            }

            var t = DelayedValue(x, 10);
            yield return t;

            callback(t.Result);
        }

        TaskScheduler _s2 = new TaskScheduler();

        Task DelayFrame(int frames)
        {
            return Util.DelayFrame(frames, _s2);
        }

        /// <summary>
        /// 遅延後に値を返す。
        /// </summary>
        /// <param name="x">返す値。</param>
        /// <param name="delayTime">遅延時間。</param>
        /// <returns></returns>
        Task<int> DelayedValue(int x, int delayTime)
        {
            return Util.Delay(delayTime).ContinueWith(_ => x);
        }
    }
}
