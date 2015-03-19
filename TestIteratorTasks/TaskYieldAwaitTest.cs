using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IteratorTasks;
using System.Collections;

namespace TestIteratorTasks
{
	[TestClass]
	public class TaskYieldAwaitTest
	{
		[TestMethod]
		public void yield_returnでawait的なことができる()
		{
			const int MaxDelay = 1000;
			var scheduler = new TaskSchedulerHelper(Task.DefaultScheduler);

			int i = 0;

			Task.Run(yield_returnでawait的なことができるIterator(MaxDelay, scheduler));

			for (i = 0; i < MaxDelay; i++)
			{
				scheduler.Update();
			}

		}

		private IEnumerator yield_returnでawait的なことができるIterator(int maxDelay, TaskSchedulerHelper s)
		{
			var random = new Random();
			int n = random.Next(19, maxDelay);

			var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(Coroutines.NFrame(n))).ToArray();

			foreach (var t in tasks)
			{
				if (t.IsCompleted)
				{
					// 完了済みのタスクを yield return すると、1フレーム後にすぐ戻ってくるはず
					var countBefore = s.FrameCount;
					yield return t;
					var countAfter = s.FrameCount;

					Assert.AreEqual(countBefore + 1, countAfter);
				}
				else
				{
					yield return t;
					// yield return するとタスク完了待てる
					Assert.IsTrue(t.IsCompleted);
				}
			}

			foreach (var t in tasks)
			{
				Assert.IsTrue(t.IsCompleted);
			}
		}
	}
}
