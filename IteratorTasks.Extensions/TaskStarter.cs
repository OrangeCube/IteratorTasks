namespace IteratorTasks
{
    /// <summary>
    /// <see cref="Task"/> を開始するだけのデリゲート。
    /// 「メソッド内で、引数でスターターを受け取って、他の処理を挟んだ後にタスクを開始」みたいな要件で使う。
    /// 意図をはっきりさせるために、<see cref="System.Func{Task}"/> でなくて名前付きのデリゲートを用意。
    /// </summary>
    public delegate Task TaskStarter();
}
