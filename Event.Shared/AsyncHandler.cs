#if UseIteratorTasks
using IteratorTasks;
#else
using System.Threading;
using System.Threading.Tasks;
#endif

namespace System
{
    /// <summary>
    /// <see cref="System.EventHandler{TEventArgs}"/> と全く一緒。
    /// 名前がかぶってると using System; で死ねるのでこんな名前に。
    /// </summary>
    /// <remarks>
    /// Unity のせい。
    /// .NET 4.5 で、EventHandler の仕様が変わってて(where TArg : EventArgs 制約が消えた)、
    /// そのせいだと思うんだけども、Visual Studio でビルドした DLL に EventHandler{T} が含まれていると、iOS ビルドで死ぬ。
    /// </remarks>
    /// <typeparam name="TArg"></typeparam>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate Task AsyncHandler<TArg>(object sender, TArg args);
}
