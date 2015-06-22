using System.Collections;
using IteratorTasks;

namespace ConsoleApplication1
{
    class TypeName
    {
        private Task<int> _task = Task<int>.CompletedTask;

        public IEnumerator MethodName()
        {
            var t = _task;
            if (!t.IsCompleted)
                yield return t;
            t.ThrowIfException();
            var result = t.Result;
        }
    }
}