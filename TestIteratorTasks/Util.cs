using IteratorTasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestIteratorTasks
{
    class Util
    {
        /// <summary>
        /// ランダムに Delay とか DelayFrame とかを10回 yield return するタスク。
        /// 作ったタスクはワースト ケースでも 25ミリ秒 + Update 50回 程度で終わる。
        /// </summary>
        /// <param name="skipScheduler">DelayFrame 実行用のスケジューラー。DefaultScheduler とは別系統のスケジューラーを用意しないと、Shutdown 後に DelayFrame が開始できなくなって詰む。</param>
        /// <returns></returns>
        public static Task RandomTask(TaskScheduler scheduler, TaskScheduler skipScheduler)
        {
            return RandomTask(new Random(), scheduler, skipScheduler);
        }

        public static Task RandomTask(Random random, TaskScheduler scheduler, TaskScheduler skipScheduler)
        {
            return Task.Run(RandomCoroutine(random, skipScheduler), scheduler);
        }

        static IEnumerator RandomCoroutine(Random rand, TaskScheduler skipScheduler)
        {
            for (int i = 0; i < 5; i++)
            {
                var cond = rand.Next(0, 3);
                switch (cond)
                {
                    case 0:
                        yield return Delay(rand.Next(2, 5), skipScheduler);
                        break;
                    case 1:
                        yield return DelayFrame(rand.Next(2, 5), skipScheduler);
                        break;
                    default:
                        var n = rand.Next(2, 5);
                        for (int j = 0; j < n; j++) yield return null;
                        break;
                }
            }
        }

        /// <summary>
        /// 特定のスケジューラー上で Delay 待ちする。
        /// </summary>
        /// <param name="milliSecond"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        //public static Task Delay(int milliSecond, TaskScheduler scheduler)
        //{
        //    return Task.Run(DelayIterator(milliSecond), scheduler);
        //}

        //static IEnumerator DelayIterator(int milliSecond)
        //{
        //    yield return Delay(milliSecond);
        //}

        public static Task Delay(int milliSecond)
        {
            return Delay(milliSecond, Task.DefaultScheduler);
        }

        /// <summary>
        /// タイマーを使って、指定した遅延時間[ミリ秒]後に完了するタスクを作る。
        /// </summary>
        /// <param name="milliSecond">遅延時間</param>
        /// <returns></returns>
        public static Task Delay(int milliSecond, TaskScheduler scheduler)
        {
            var tcs = new TaskCompletionSource<object>(scheduler);

            Timer timer = null;

            timer = new Timer(_ =>
            {
                timer.Dispose();
                tcs.SetResult(null);
            }, null, milliSecond, Timeout.Infinite);

            return tcs.Task;
        }

        /// <summary>
        /// 空 yield return して、指定フレーム数スキップするタスクを作る。
        /// </summary>
        /// <param name="frames">フレーム数。</param>
        /// <returns></returns>
        public static Task DelayFrame(int frames, TaskScheduler scheduler)
        {
            return Task.Run(Skip(frames), scheduler);
        }

        public static IEnumerator Skip(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        public static Task DelayFrame(int frames, IteratorTasks.CancellationToken ct, TaskScheduler scheduler)
        {
            return Task.Run(Skip(frames, ct), scheduler);
        }

        public static IEnumerator Skip(int frames, IteratorTasks.CancellationToken ct)
        {
            for (int i = 0; i < frames; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return null;
            }
        }

        public static Task Delay(int milliSecond, IteratorTasks.CancellationToken ct)
        {
            return Delay(milliSecond, ct, Task.DefaultScheduler);
        }

        public static Task Delay(int milliSecond, IteratorTasks.CancellationToken ct, TaskScheduler scheduler)
        {
            var tcs = new TaskCompletionSource<object>(scheduler);

            Timer timer = null;

            timer = new Timer(_ =>
            {
                timer.Dispose();
                tcs.SetResult(null);
            }, null, milliSecond, Timeout.Infinite);

            ct.Register(() =>
            {
                timer.Dispose();
                tcs.SetCanceled();
            });

            return tcs.Task;
        }
    }
}
