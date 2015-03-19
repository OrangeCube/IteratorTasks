using System;
using System.Linq;
using System.Collections.Generic;

namespace IteratorTasks
{
    /// <summary>
    /// 複数の例外を束ねる例外クラス。
    /// 並行動作してると、複数のタスク内で同時に例外が発生する可能性があるので。
    /// </summary>
    public class AggregateException : Exception
    {
        /// <summary>
        ///    処理済みフラグ。
        ///    このフラグが立っていない例外があった場合、TaskScheduler.UnhandledException イベントが発生。
        ///    （手動で立てる。既定では false のままなので、必ずイベント発生。）
        ///    （.NET 4のTaskのUnobservedTaskExceptionみたいに、Errorプロパティを誰かが参照しただけで立つフラグみたいな機構は持ってない。）
        /// </summary>
        public bool IsHandled { get; set; }

        private Exception[] _exceptions;

        /// <summary>
        /// 例外一覧。
        /// </summary>
        public IEnumerable<Exception> Exceptions { get { return _exceptions; } }

        /// <summary>
        /// params 配列版。
        /// </summary>
        /// <param name="exceptions"></param>
        public AggregateException(params Exception[] exceptions) : this((IEnumerable<Exception>)exceptions) { }

        /// <summary>
        /// IEnumerable 版。
        /// </summary>
        /// <param name="exceptions"></param>
        public AggregateException(IEnumerable<Exception> exceptions)
        {
            _exceptions = exceptions.ToArray();
        }

        internal void Merge(AggregateException ex)
        {
            Merge(ex._exceptions);
        }

        internal void Merge(params Exception[] exceptions)
        {
            _exceptions = _exceptions.Concat(exceptions.Where(e => e != null)).ToArray();
        }

        /// <summary>
        /// <see cref="Exceptions"/> が1個だけならそれの <see cref="Exception.Message"/> をそのまま、
        /// 複数なら個数を表示。
        /// </summary>
        public override string Message
        {
            get
            {
                var count = _exceptions.Length;

                if (count == 1) return _exceptions[0].Message;
                else if (count > 1) return string.Format("AggregateException: {0} errors", count);
                else return base.Message;
            }
        }

        /// <summary>
        /// <see cref="Exceptions"/> を全部表示。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(base.ToString());
            sb.AppendLine();
            sb.AppendLine("inner exceptions:");

            foreach (var ex in Exceptions)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }
    }
}
