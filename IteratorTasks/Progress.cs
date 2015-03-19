using System;

namespace IteratorTasks
{
    /// <summary>
    /// イベントを使って進捗報告を受け取るためのクラス。
    /// </summary>
    /// <typeparam name="T">進捗度合を表す型。</typeparam>
    public class Progress<T> : IProgress<T>
    {
        /// <summary>
        /// 後から <see cref="ProgressChanged"/> でハンドラーを追加する想定。
        /// </summary>
        public Progress() { }

        /// <summary>
        /// 最初からハンドラーを1つ追加するコンストラクター。
        /// </summary>
        /// <param name="onProgressChanged"></param>
        public Progress(Action<T> onProgressChanged) { ProgressChanged += onProgressChanged; }

        /// <summary>
        /// 進捗状況が変化したときに起こすイベント。
        /// </summary>
        public event Action<T> ProgressChanged { add { _progressChanged += value; } remove { _progressChanged -= value; } }
        private Action<T> _progressChanged;

        void IProgress<T>.Report(T value)
        {
            var d = _progressChanged;
            if (d != null) d(value);
        }
    }
}
