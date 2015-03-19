using IteratorTasks;

namespace TestIteratorTasks
{
	class TaskSchedulerHelper
	{
		private readonly TaskScheduler _scheduler;
		public int FrameCount { get; private set; }

		public TaskSchedulerHelper(TaskScheduler scheduler)
		{
			_scheduler = scheduler;
		}

		public void Update()
		{
			_scheduler.Update();
			++FrameCount;
		}
	}
}
