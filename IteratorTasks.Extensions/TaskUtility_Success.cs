using System;

namespace IteratorTasks
{
    public static partial class TaskUtility
    {
        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWith"/>。
        /// </summary>
        public static Task OnSuccess(this Task t, Action succesHandler)
        {
            return t.ContinueWith(x =>
            {
                succesHandler();
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWith{T}"/>。
        /// </summary>
        public static Task OnSuccess<T>(this Task<T> t, Action<T> succesHandler)
        {
            return t.ContinueWith(x =>
            {
                succesHandler(t.Result);
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWith{T}"/>。
        /// </summary>
        public static Task<T> OnSuccess<T>(this Task t, Func<T> succesHandler)
        {
            return t.ContinueWith<T>(x =>
            {
                return succesHandler();
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWith{T}"/>。
        /// </summary>
        public static Task<U> OnSuccess<T, U>(this Task<T> t, Func<T, U> succesHandler)
        {
            return t.ContinueWith<U>(x =>
            {
                return succesHandler(t.Result);
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWithTask"/>。
        /// </summary>
        public static Task OnSuccessWithTask(this Task t, Func<Task> succesHandler)
        {
            return t.ContinueWithTask(x =>
            {
                return succesHandler();
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWithTask{T}"/>。
        /// </summary>
        public static Task OnSuccessWithTask<T>(this Task<T> t, Func<T, Task> succesHandler)
        {
            return t.ContinueWithTask(x =>
            {
                return succesHandler(t.Result);
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWithTask{T}"/>。
        /// </summary>
        public static Task<T> OnSuccessWithTask<T>(this Task t, Func<Task<T>> succesHandler)
        {
            return t.ContinueWithTask<T>(x =>
            {
                return succesHandler();
            }, true);
        }

        /// <summary>
        /// タスクが正常終了したときのみ実行される<see cref="Task.ContinueWithTask{T}"/>。
        /// </summary>
        public static Task<U> OnSuccessWithTask<T, U>(this Task<T> t, Func<T, Task<U>> succesHandler)
        {
            return t.ContinueWithTask<U>(x =>
            {
                return succesHandler(t.Result);
            }, true);
        }
    }
}
