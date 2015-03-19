using System;

namespace IteratorTasks
{
    public partial class Task
    {
        /// <summary>
        /// 複数のタスクのうち、最初に終わったタスクを返す。
        /// </summary>
        public static Task<Task<T>> WhenAny<T>(params Task<T>[] tasks) { return WhenAny(null, tasks); }


        /// <summary>
        /// 複数のタスクのうち、最初に終わったタスクを返す。
        /// </summary>
        public static Task<Task<T>> WhenAny<T>(TaskScheduler scheduler, params Task<T>[] tasks)
        {
            if (tasks.Length == 0)
                throw new ArgumentNullException("tasks must contain at least one task", "tasks");

            var tcs = new TaskCompletionSource<Task<T>>(scheduler);

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("task must not null", "tasks");

                task.ContinueWith(tcs.TrySetResult);
            }

            return tcs.Task;
        }

        /// <summary>
        /// 複数のタスクのうち、最初に終わったタスクを返す。
        /// </summary>
        public static Task<Task> WhenAny(params Task[] tasks) { return WhenAny(null, tasks); }

        /// <summary>
        /// 複数のタスクのうち、最初に終わったタスクを返す。
        /// </summary>
        /// <param name="tasks">最初の1つを待ちたいタスク一覧。</param>
        /// <param name="scheduler">このタスクを実行するスケジューラ。</param>
        /// <returns>最初の1つだけ終わったら完了になるタスク。</returns>
        public static Task<Task> WhenAny(TaskScheduler scheduler, params Task[] tasks)
        {
            if (tasks.Length == 0)
                throw new ArgumentNullException("tasks must contain at least one task", "tasks");

            var tcs = new TaskCompletionSource<Task>(scheduler);

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("task must not null", "tasks");

                task.ContinueWith(tcs.TrySetResult);
            }

            return tcs.Task;
        }
    }
}
