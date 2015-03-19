using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskUtil
{
    // 同名のクラスが IteratorTasks の方にもあるけど、それの System.Threading.Tasks 版。
    // この名前空間以下でいいのかは微妙（もっと汎用）だけど、移すかどうかは必要になってから考える。

    /// <summary>
    /// 複数のタスクの完了を待つクラス。
    /// </summary>
    public class TaskPool
    {
        private readonly List<Task> _list = new List<Task>();

        /// <summary>
        /// タスクを追加登録する。
        /// </summary>
        /// <param name="t"></param>
        public void Register(Task t) { _list.Add(t); }

        /// <summary>
        /// 登録されてるタスクがすべて完了するのを待つ。
        /// </summary>
        /// <returns></returns>
        public Task Await()
        {
            var copy = _list.ToArray();
            _list.Clear();
            return Task.WhenAll(copy);
        }
    }
}
