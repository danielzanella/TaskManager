using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
	/// <summary>
	/// An IStatsStrategy's that use PerformanceCounter to hold statistics data.
	/// </summary>
    public class PerformanceCounterStatsStrategy : IStatsStrategy
    {
		#region Fields
        /// <summary>
        /// Performance counter category.
        /// </summary>
        private PerformanceCounterCategory _performanceCounterCategory;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskManager.PerformanceCounterStatsStrategy"/> class.
		/// </summary>
		public PerformanceCounterStatsStrategy()
		{
			CreatePerformanceCounters ();
			InitializePerformanceCounters ();
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the average execution time.
		/// </summary>
		/// <value>The average execution time.</value>
        public IStatsData AverageExecutionTime { get; private set; }

		/// <summary>
		/// Gets the average lag time.
		/// </summary>
		/// <value>The average lag time.</value>
        public IStatsData AverageLagTime { get; private set; }

		/// <summary>
		/// Gets the base average execution time.
		/// </summary>
		/// <value>The base average execution time.</value>
        public IStatsData BaseAverageExecutionTime { get; private set; }

		/// <summary>
		/// Gets the base average lag time.
		/// </summary>
		/// <value>The base average lag time.</value>
        public IStatsData BaseAverageLagTime { get; private set; }

		/// <summary>
		/// Gets the errors per second.
		/// </summary>
		/// <value>The errors per second.</value>
        public IStatsData ErrorsPerSecond { get; private set; }

		/// <summary>
		/// Gets the max threads.
		/// </summary>
		/// <value>The max threads.</value>
        public IStatsData MaxThreads { get; private set; }

		/// <summary>
		/// Gets the scheduled tasks.
		/// </summary>
		/// <value>The scheduled tasks.</value>
        public IStatsData ScheduledTasks { get; private set; }

		/// <summary>
		/// Gets the spawned threads.
		/// </summary>
		/// <value>The spawned threads.</value>
        public IStatsData SpawnedThreads { get; private set; }

		/// <summary>
		/// Gets the tasks.
		/// </summary>
		/// <value>The tasks.</value>
        public IStatsData Tasks { get; private set; }

		/// <summary>
		/// Gets the tasks running.
		/// </summary>
		/// <value>The tasks running.</value>
        public IStatsData TasksRunning { get; private set; }

		/// <summary>
		/// Gets the timeouts per second.
		/// </summary>
		/// <value>The timeouts per second.</value>
        public IStatsData TimeoutsPerSecond { get; private set; }

		/// <summary>
		/// Gets the total exceptions.
		/// </summary>
		/// <value>The total exceptions.</value>
        public IStatsData TotalExceptions { get; private set; }

		/// <summary>
		/// Gets the total timeouts.
		/// </summary>
		/// <value>The total timeouts.</value>
        public IStatsData TotalTimeouts { get; private set; }
		#endregion

		#region Methods
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the
		/// <see cref="TaskManager.PerformanceCounterStatsStrategy"/>. The <see cref="Dispose"/> method leaves the
		/// <see cref="TaskManager.PerformanceCounterStatsStrategy"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the
		/// <see cref="TaskManager.PerformanceCounterStatsStrategy"/> so the garbage collector can reclaim the memory that the
		/// <see cref="TaskManager.PerformanceCounterStatsStrategy"/> was occupying.</remarks>
        public void Dispose()
        {
			((PerformanceCounterStatsData)AverageExecutionTime).Dispose ();
			((PerformanceCounterStatsData)AverageLagTime).Dispose ();
			((PerformanceCounterStatsData)BaseAverageExecutionTime).Dispose ();
			((PerformanceCounterStatsData)BaseAverageLagTime).Dispose ();
			((PerformanceCounterStatsData)ErrorsPerSecond).Dispose ();
			((PerformanceCounterStatsData)MaxThreads).Dispose ();
			((PerformanceCounterStatsData)ScheduledTasks).Dispose ();
			((PerformanceCounterStatsData)SpawnedThreads).Dispose ();
			((PerformanceCounterStatsData)Tasks).Dispose ();
			((PerformanceCounterStatsData)TasksRunning).Dispose ();
			((PerformanceCounterStatsData)TimeoutsPerSecond).Dispose ();
			((PerformanceCounterStatsData)TotalExceptions).Dispose ();
			((PerformanceCounterStatsData)TotalTimeouts).Dispose ();

			if (null != _performanceCounterCategory)
			{
				PerformanceCounterCategory.Delete(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY);
			}

        }

        /// <summary>
        /// Registers the performance counters with the operating system.
        /// </summary>
        private void CreatePerformanceCounters()
        {
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();

            ccdc.AddRange(TaskManagerInstaller.COUNTERS);

            if (!PerformanceCounterCategory.Exists(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY))
            {
                _performanceCounterCategory = PerformanceCounterCategory.Create(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.PERFORMANCE_COUNTER_DESCRIPTION, PerformanceCounterCategoryType.SingleInstance, ccdc);
            }
        }

        /// <summary>
        /// Initializes the performance counters.
        /// </summary>
        private void InitializePerformanceCounters()
        {
			AverageExecutionTime = CreateStatsData(TaskManagerInstaller.COUNTER_AVERAGE_TASK_TIME);
			AverageLagTime = CreateStatsData(TaskManagerInstaller.COUNTER_AVERAGE_TASK_LAG);
			BaseAverageExecutionTime = CreateStatsData(TaskManagerInstaller.BASE_COUNTER_AVERAGE_TASK_TIME);
			BaseAverageLagTime = CreateStatsData(TaskManagerInstaller.BASE_COUNTER_AVERAGE_TASK_LAG);
			ErrorsPerSecond = CreateStatsData(TaskManagerInstaller.COUNTER_EXCEPTIONS_PER_SECOND);
			MaxThreads = CreateStatsData(TaskManagerInstaller.COUNTER_MAX_THREADS);
			ScheduledTasks = CreateStatsData(TaskManagerInstaller.COUNTER_SCHEDULED_TASKS);
			SpawnedThreads = CreateStatsData(TaskManagerInstaller.COUNTER_SPAWNED_THREADS);
			Tasks = CreateStatsData(TaskManagerInstaller.COUNTER_TASKS);
			TasksRunning = CreateStatsData(TaskManagerInstaller.COUNTER_RUNNING_TASKS);
			TimeoutsPerSecond = CreateStatsData(TaskManagerInstaller.COUNTER_TIMEOUTS_PER_SECOND);
			TotalExceptions = CreateStatsData(TaskManagerInstaller.COUNTER_EXCEPTIONS);
			TotalTimeouts = CreateStatsData(TaskManagerInstaller.COUNTER_TIMEOUTS);
        }

        private PerformanceCounterStatsData CreateStatsData(string name)
        {
            return new PerformanceCounterStatsData(
                new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, name, false));
        }
		#endregion
    }
}
