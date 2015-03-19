using IteratorTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;


namespace TestIteratorTasks
{
	[TestClass]
	public class TaskTestSchedulingTiming
	{
		[TestMethod]
		public void タスク完了と同フレームでContinueWithが呼ばれる()
		{
			var random = new Random();
			int n = random.Next(10, 300);
			var scheduler = new TaskSchedulerHelper(Task.DefaultScheduler);
			int count = 0;

			var t1 = Task.Run(() => Coroutines.NFrame(n))
				.ContinueWith( (_) => count = scheduler.FrameCount );
			
			for(var i = 0; i < 10000; ++i)
			{
				scheduler.Update();

				if (t1.IsCompleted)
				{
					Assert.AreEqual(n, i);
					break;
				}
			}

			Assert.AreEqual(n, count);
		}
	}
}
