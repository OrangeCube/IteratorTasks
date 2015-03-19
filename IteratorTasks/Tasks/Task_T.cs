using System;
using System.Collections;

namespace IteratorTasks
{
    /// <summary>
    /// .NET 4 の Task 的にコルーチンを実行するためのクラス。
    /// 戻り値あり版。
    /// </summary>
    /// <typeparam name="T">最終的に返す型。</typeparam>
    public partial class Task<T> : Task
    {
        /// <summary>
        /// 最終的に返したい値。
        /// </summary>
        virtual public T Result
        {
            get
            {
                if (Exception != null) throw Exception;
                return _result;
            }
        }
        private T _result = default(T);

        internal Task() { }

        public Task(Func<Action<T>, IEnumerator> routine)
        {
            Status = TaskStatus.Created;
            Routine = routine(SetResult);
        }

        internal Task(T result)
        {
            Status = TaskStatus.RanToCompletion;
            _result = result;
        }

        private void SetResult(T result)
        {
            _result = result;
        }

        public Task ContinueWith(Action<Task<T>> func)
        {
            return ContinueWithInternal<object>(() => { func(this); return default(object); });
        }

        public Task<U> ContinueWith<U>(Func<Task<T>, U> func)
        {
            return ContinueWithInternal<U>(() => func(this));
        }

        public Task ContinueWithIterator(Func<Task<T>, IEnumerator> routine)
        {
            return ContinueWithInternal<object>(() => Task.Run(() => routine(this), this.Scheduler));
        }

        public Task<U> ContinueWithIterator<U>(Func<Task<T>, Action<U>, IEnumerator> routine)
        {
            return ContinueWithInternal<U>(() => Task.Run<U>(callback => routine(this, callback), this.Scheduler));
        }

        public Task ContinueWithTask(Func<Task<T>, Task> starter)
        {
            return ContinueWithInternal<object>(() => starter(this));
        }

        public Task<U> ContinueWithTask<U>(Func<Task<T>, Task<U>> starter)
        {
            return ContinueWithInternal<U>(() => starter(this));
        }
    }
}
