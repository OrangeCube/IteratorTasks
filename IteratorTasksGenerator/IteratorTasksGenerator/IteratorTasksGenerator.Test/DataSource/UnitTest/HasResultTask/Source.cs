using System.Collections;
using IteratorTasks;

namespace ConsoleApplication1
{
    class TypeName
    {
        private Task<int> _task = Task<int>.CompletedTask;

        public IEnumerator MethodName()
        {
            yield return _task;
        }
    }
}