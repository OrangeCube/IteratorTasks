namespace System
{
    /// <summary>
    /// null しか作れないアレ。
    /// </summary>
    /// <remarks>
    /// Unit (単元、{0} みたいな集合)だと、チーム中のユニットと紛らわしいので悩む。
    /// かといって、void は void で、<see cref="System.Void"/> があるので悩むというか、実際 using System; してると死ぬ。
    /// </remarks>
    public class Null
    {
        private Null() { }
    }
}
