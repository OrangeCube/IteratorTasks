using System.Collections;
using IteratorTasks;

namespace ConsoleApplication1
{
    class TypeName
    {
        private Task _task = Task.CompletedTask;

        public IEnumerator MethodName()
        {
            var t = _task;
            if (!t.IsCompleted)
                yield return t;
            t.ThrowIfException();
        }
    }
}