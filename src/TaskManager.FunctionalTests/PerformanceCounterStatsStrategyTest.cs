using System;
using NUnit.Framework;

namespace TaskManager.FunctionalTests
{
	[TestFixture]
	public class PerformanceCounterStatsStrategyTest
	{
		[Test]
		public void Constructor_NoArgs_PerformanceCountersCreated ()
		{
            ProgramTest.Run("ServiceUninstall.cmd");   
			var target = new PerformanceCounterStatsStrategy();
			Assert.IsNotNull(target.AverageExecutionTime);
			Assert.IsNotNull(target.AverageLagTime);
			Assert.IsNotNull(target.BaseAverageExecutionTime);
			Assert.IsNotNull(target.BaseAverageLagTime);
			Assert.IsNotNull(target.ErrorsPerSecond);
			Assert.IsNotNull(target.MaxThreads);
			Assert.IsNotNull(target.ScheduledTasks);
			Assert.IsNotNull(target.SpawnedThreads);
			Assert.IsNotNull(target.Tasks);
			Assert.IsNotNull(target.TasksRunning);
			Assert.IsNotNull(target.TimeoutsPerSecond);
			Assert.IsNotNull(target.TotalExceptions);
			Assert.IsNotNull(target.TotalTimeouts);
		}
	}
}

