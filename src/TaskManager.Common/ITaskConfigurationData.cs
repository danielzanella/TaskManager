namespace TaskManager.Common
{
    using System;

    /// <summary>
    /// Defines common task configuration information, shared between the task configuration and specific schedules.
    /// </summary>
    public interface ITaskConfigurationData
    {
        /// <summary>
        /// Gets or sets the number of task instances spawned.
        /// </summary>
        /// <remarks>Default is to spawn a single instance.</remarks>
        int? Spawn { get; set; }

        /// <summary>
        /// Gets or sets the amount of time the SLA scheduler waits before it considers the task execution is blocked/has hanged/in error and restarts it.
        /// </summary>
        TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the amount of time the task is allowed to be late while waiting for an executing thread.
        /// </summary>
        /// <remarks>If the task doesn't start executing in this amount of time (after it is marked to start), and the number of executing threads is below the maximum number of threads allowed, the SLA scheduler spawns a new thread for the task.</remarks>
        TimeSpan? SLA { get; set; }

        /// <summary>
        /// Gets or sets the minimum amount of time allowed per execution burst.
        /// </summary>
        TimeSpan? TimeUnit { get; set; }

        /// <summary>
        /// Gets or sets the number of times a task runs per execution burst.
        /// </summary>
        int? MaxRuns { get; set; }

        /// <summary>
        /// Gets or sets the amount of time to wait after an empty or failed execution burst.
        /// </summary>
        TimeSpan? Wait { get; set; }
    }
}
