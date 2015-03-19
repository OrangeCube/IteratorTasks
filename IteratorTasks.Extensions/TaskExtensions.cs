using System;
using System.Collections;
using System.Collections.Generic;

namespace IteratorTasks
{
    /// <summary>
    /// <see cref="Task"/>の拡張。
    /// </summary>
    /// <remarks>
    /// <see cref="Task"/>の方は、極力 .NET 4 以降標準の System.Threading.Tasks.Task に合わせたい。
    /// 標準 Task にない機能は極力こっちに書く。
    /// </remarks>
    public static class TaskExtensions
    {
        /// <summary>
        /// タスク正常終了時にのみ呼ばれる。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Task Then(this Task t, Action a)
        {
            t.ContinueWith(x =>
            {
                if (x.Status == TaskStatus.RanToCompletion)
                    a();
            });
            return t;
        }

        /// <summary>
        /// タスク正常終了時にのみ呼ばれる。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Task<T> Then<T>(this Task<T> t, Action<T> a)
        {
            Action<Task<T>> continuation = x =>
            {
                if (x.Status == TaskStatus.RanToCompletion)
                    a(x.Result);
            };
            t.ContinueWith(continuation);
            return t;
        }

        /// <summary>
        /// タスクがエラー終了時、かつ、指定した例外が出た場合にのみ呼ばれる。
        /// </summary>
        /// <typeparam name="TEx">指定したい例外の型</typeparam>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Task OnError<TEx>(this Task t, Action<TEx> a)
            where TEx : Exception
        {
            t.ContinueWith(x =>
            {
                if (t.Exception != null)
                    foreach (var e in t.Exception.Exceptions)
                    {
                        var et = e as TEx;
                        if (et != null)
                            a(et);
                    }
            });
            return t;
        }

        /// <summary>
        /// タスクがエラー終了時、かつ、指定した例外が出た場合にのみ呼ばれる。
        /// </summary>
        /// <typeparam name="T">戻り値の型。</typeparam>
        /// <typeparam name="TEx">指定したい例外の型</typeparam>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Task<T> OnError<T, TEx>(this Task<T> t, Action<TEx> a)
            where TEx : Exception
        {
            t.ContinueWith(x =>
            {
                if (t.Exception != null)
                    foreach (var e in t.Exception.Exceptions)
                    {
                        var et = e as TEx;
                        if (et != null)
                            a(et);
                    }
            });
            return t;
        }

        /// <summary>
        /// エラーがあるときにエラーを再 throw する。
        /// 主にタスクのコルーチン内で子タスクを走らせるときのエラー処理用。
        /// </summary>
        public static void ThrowIfException(this Task t)
        {
            if (t.Exception != null) throw t.Exception;
        }

        /// <summary>
        /// 条件付きでエラーがあるときにエラーを再 throw する。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="check">再 throw 条件チェック。 trueを返すとエラーを throw する</param>
        public static void ThrowIfException(this Task t, Func<Task, bool> check)
        {
            if (t.Exception != null && check(t)) throw t.Exception;
        }

        /// <summary>
        /// <see cref="Task.IsHandled"/> を立ててから返す。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Task WithExceptionHandled(this Task t)
        {
            t.ContinueWith(_ =>
            {
                if (t.Exception != null)
                {
                    t.Exception.IsHandled = true;
                }
            });
            return t;
        }

        /// <summary>
        /// <see cref="Task.IsHandled"/> を立ててから返す。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Task<TResult> WithExceptionHandled<TResult>(this Task<TResult> t)
        {
            t.ContinueWith(_ =>
            {
                if (t.Exception != null)
                {
                    t.Exception.IsHandled = true;
                }
            });
            return t;
        }

        /// <summary>
        /// 前段のタスクでエラー出てたらそれを伝搬するだけで、実際にはContinueWithしないバージョン。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="func"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task ContinueWith(this Task t, Action<Task> func, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWith(func);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException(x.Exception);
                else return x.ContinueWith(func);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="func"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task<T> ContinueWith<T>(this Task t, Func<Task, T> func, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWith(func);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException<T>(x.Exception);
                else return x.ContinueWith(func);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// イテレーター直指定で継続処理を書く版。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="routine"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task ContinueWithIterator(this Task t, Func<Task, IEnumerator> routine, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithIterator(routine);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException(x.Exception);
                else return x.ContinueWithIterator(routine);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// 継続処理も非同期版。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="starter"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task ContinueWithTask(this Task t, Func<Task, Task> starter, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithTask(starter);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException(x.Exception);
                else return x.ContinueWithTask(starter);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// 継続処理も非同期版。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="starter"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task<T> ContinueWithTask<T>(this Task t, Func<Task, Task<T>> starter, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithTask(starter);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException<T>(x.Exception);
                else return x.ContinueWithTask(starter);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// 継続処理も非同期版。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="func"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task ContinueWith<T>(this Task<T> t, Action<Task<T>> func, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWith(func);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException(x.Exception);
                else return x.ContinueWith(func);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <param name="func"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task<U> ContinueWith<T, U>(this Task<T> t, Func<Task<T>, U> func, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWith(func);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException<U>(x.Exception);
                else return x.ContinueWith(func);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// イテレーター直指定で継続処理を書く版。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="routine"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task ContinueWithIterator<T>(this Task<T> t, Func<Task<T>, IEnumerator> routine, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithIterator(routine);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException(x.Exception);
                else return x.ContinueWithIterator(routine);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// イテレーター直指定で継続処理を書く版。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <param name="routine"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task<U> ContinueWithIterator<T, U>(this Task<T> t, Func<Task<T>, Action<U>, IEnumerator> routine, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithIterator(routine);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException<U>(x.Exception);
                else return x.ContinueWithIterator(routine);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// 継続処理も非同期版。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="starter"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task ContinueWithTask<T>(this Task<T> t, Func<Task<T>, Task> starter, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithTask(starter);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException(x.Exception);
                else return x.ContinueWithTask(starter);
            });
        }

        /// <summary>
        /// 前段のタスクで例外が出ていたら、その例外を投げなおす。
        /// 継続処理も非同期版。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <param name="starter"></param>
        /// <param name="rethrowOnError"></param>
        /// <returns></returns>
        public static Task<U> ContinueWithTask<T, U>(this Task<T> t, Func<Task<T>, Task<U>> starter, bool rethrowOnError)
        {
            if (!rethrowOnError) return t.ContinueWithTask(starter);
            return t.ContinueWithTask(x =>
            {
                if (x.Exception != null) return Task.FromException<U>(x.Exception);
                else return x.ContinueWithTask(starter);
            });
        }

        /// <summary>
        /// 引数がキャンセルされたら連動してキャンセルされる <see cref="CancellationTokenSource"/> を作ります。
        /// 一方通行(戻り値の <see cref="CancellationTokenSource"/> がキャンセルされても、引数側はキャンセルされない)。
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static CancellationTokenSource ToCancellationTokenSourceOneWay(this CancellationToken ct)
        {
            var cts = new CancellationTokenSource();
            if (ct != CancellationToken.None)
                ct.Register(() => cts.Cancel());
            return cts;
        }

        /// <summary>
        /// 引数のどれか1個でもキャンセルされたら連動してキャンセルされる <see cref="CancellationTokenSource"/> を作ります。
        /// 一方通行(戻り値の <see cref="CancellationTokenSource"/> がキャンセルされても、引数側はキャンセルされない)。
        /// </summary>
        /// <param name="ctList"></param>
        /// <returns></returns>
        public static CancellationTokenSource ToCancellationTokenSourceOneWay(this IEnumerable<CancellationToken> ctList)
        {
            var cts = new CancellationTokenSource();
            foreach (var ct in ctList)
                if (ct != CancellationToken.None)
                    ct.Register(() => cts.Cancel());
            return cts;
        }

        /// <summary>
        /// 2つのトークンのうち、片方でもキャンセルされたらキャンセルされるような新しいトークンを作る。
        /// </summary>
        public static CancellationToken Merge(this CancellationToken t1, CancellationToken t2) { return new []{t1, t2}.ToCancellationTokenSourceOneWay().Token; }

        /// <summary>
        /// CancellationToken からキャンセルされたときに完了するタスクを作成する。
        /// </summary>
        public static Task ToTask(this CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            ct.Register(() => tcs.SetResult(false));
            return tcs.Task;
        }
    }
}
