using System.Collections;
using IteratorTasks;

namespace ConsoleApplication1
{
    class TypeName
    {
        private Task _task = Task.CompletedTask;

        public IEnumerator MethodName()
        {
            yield return _task;
        }
    }
}