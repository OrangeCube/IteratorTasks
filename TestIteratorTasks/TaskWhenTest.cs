using IteratorTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;

namespace TestIteratorTasks
{
	[TestClass]
	public class TaskWhenTest
	{
		[TestMethod]
		public void WhenAllでタスクの並行動作できる()
		{
			var t1 = Task.Run(() => Coroutines.NFrame(3));
			var t2 = Task.Run(() => Coroutines.NFrame(5));
			var t3 = Task.Run(() => Coroutines.NFrame(7));

			var task = Task.WhenAll(t1, t2, t3)
				.OnComplete(t =>
				{
					Assert.IsTrue(t1.IsCompleted);
					Assert.IsTrue(t2.IsCompleted);
					Assert.IsTrue(t3.IsCompleted);
				});

			var scheduler = Task.DefaultScheduler;
			scheduler.Update(20);

			Assert.IsTrue(task.IsCompleted);
			Assert.IsNull(task.Exception);
		}

		[TestMethod]
		public void WhenAnyで何か1つのタスクが終わるのを待てる()
		{
			var t = Task.Run(WhenAnyで何か1つのタスクが終わるのを待てるIterator);

			var scheduler = Task.DefaultScheduler;
			scheduler.Update(15);
		}

		private IEnumerator WhenAnyで何か1つのタスクが終わるのを待てるIterator()
		{
			var delays = new[] { 3, 5, 7, 9, 11, 13 };

			var tasks = delays.Select(d => Task.Run(() => Coroutines.NFrame(d))).ToArray();

			while (true)
			{
				var task = Task.WhenAny(tasks);

				yield return task;

				var first = task.Result;

				// delays が昇順で並んでいるので、前のタスクから終わるはず
				Assert.AreEqual(first, tasks[0]);

				// delays に同じ値を入れていないので、同時に1個ずつしか終わらないはず
				foreach (var t in tasks.Skip(1))
				{
					Assert.IsFalse(t.IsCompleted);
				}

				// 終わったやつを除外。全部終わったら終了
				tasks = tasks.Where(t => t != first).ToArray();
				if (tasks.Length == 0)
					break;
			}
		}
	}
}
