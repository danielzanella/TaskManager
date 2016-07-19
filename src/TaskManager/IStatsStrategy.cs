using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
	/// <summary>
	/// Defines an interface to a strategy to statistics used by TaskManager.
	/// </summary>
    public interface IStatsStrategy : IDisposable
    {
		/// <summary>
		/// Gets the average execution time.
		/// </summary>
		/// <value>The average execution time.</value>
        IStatsData AverageExecutionTime { get; }

		/// <summary>
		/// Gets the average lag time.
		/// </summary>
		/// <value>The average lag time.</value>
        IStatsData AverageLagTime { get; }

		/// <summary>
		/// Gets the base average execution time.
		/// </summary>
		/// <value>The base average execution time.</value>
        IStatsData BaseAverageExecutionTime { get; }

		/// <summary>
		/// Gets the base average lag time.
		/// </summary>
		/// <value>The base average lag time.</value>
        IStatsData BaseAverageLagTime { get; }

		/// <summary>
		/// Gets the errors per second.
		/// </summary>
		/// <value>The errors per second.</value>
        IStatsData ErrorsPerSecond { get; }

		/// <summary>
		/// Gets the max threads.
		/// </summary>
		/// <value>The max threads.</value>
        IStatsData MaxThreads { get; }

		/// <summary>
		/// Gets the scheduled tasks.
		/// </summary>
		/// <value>The scheduled tasks.</value>
        IStatsData ScheduledTasks { get; }

		/// <summary>
		/// Gets the spawned threads.
		/// </summary>
		/// <value>The spawned threads.</value>
        IStatsData SpawnedThreads { get; }

		/// <summary>
		/// Gets the tasks.
		/// </summary>
		/// <value>The tasks.</value>
        IStatsData Tasks { get; }

		/// <summary>
		/// Gets the tasks running.
		/// </summary>
		/// <value>The tasks running.</value>
        IStatsData TasksRunning { get; }

		/// <summary>
		/// Gets the timeouts per second.
		/// </summary>
		/// <value>The timeouts per second.</value>
        IStatsData TimeoutsPerSecond { get; }

		/// <summary>
		/// Gets the total exceptions.
		/// </summary>
		/// <value>The total exceptions.</value>
        IStatsData TotalExceptions { get; }

		/// <summary>
		/// Gets the total timeouts.
		/// </summary>
		/// <value>The total timeouts.</value>
        IStatsData TotalTimeouts { get; }        
    }
}