namespace TaskManager
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using TaskManager.Configuration;

    /// <summary>
    /// Manages task schedules and execution.
    /// </summary>
    internal static class TaskSupervisor
    {
        /// <summary>
        /// Maximum number of threads allowed.
        /// </summary>
        public const int TaskMaxThreads = 200;

        /// <summary>
        /// Default execution timeout, in seconds.
        /// </summary>
        public const int TaskTimeout = 60; // 60s

        /// <summary>
        /// Default wait between bursts, in seconds.
        /// </summary>
        public const int IdleTimeout = 5; // 5s

        /// <summary>
        /// List of known tasks.
        /// </summary>
        private static List<TaskWrapper> _tasks;

        /// <summary>
        /// List of tasks scheduled for execution.
        /// </summary>
        private static Queue<TaskWrapper> _taskQueue;

        /// <summary>
        /// Lock for managing task lists.
        /// </summary>
        private static object _taskLock;

        /// <summary>
        /// List of executing threads.
        /// </summary>
        private static List<TaskThread> _threads;

        /// <summary>
        /// Lock for managing the executing thread list.
        /// </summary>
        private static object _threadsLock;

        /// <summary>
        /// The SLA Thread.
        /// </summary>
        private static Thread _threadSLA;

        /// <summary>
        /// Performance counter category.
        /// </summary>
        private static PerformanceCounterCategory _performanceCounterCategory;

        /// <summary>
        /// Counter for the number of active threads.
        /// </summary>
        private static PerformanceCounter _perfCounterSpawnedThreads;

        /// <summary>
        /// Counter for the maximum number of threads.
        /// </summary>
        private static PerformanceCounter _perfCounterMaxThreads;

        /// <summary>
        /// Counter for the number of known tasks.
        /// </summary>
        private static PerformanceCounter _perfCounterTasks;

        /// <summary>
        /// Counter for the number of tasks being executed.
        /// </summary>
        private static PerformanceCounter _perfCounterTasksRunning;

        /// <summary>
        /// Counter for the average execution time.
        /// </summary>
        private static PerformanceCounter _perfCounterAverageExecutionTime;

        /// <summary>
        /// Base for the average execution time counter.
        /// </summary>
        private static PerformanceCounter _perfCounterBaseAverageExecutionTime;

        /// <summary>
        /// Counter for the number of task exceptions caught since the service started.
        /// </summary>
        private static PerformanceCounter _perfCounterTotalExceptions;

        /// <summary>
        /// Counter for the number of task timeouts occurred since the service started.
        /// </summary>
        private static PerformanceCounter _perfCounterTotalTimeouts;

        /// <summary>
        /// Counter for the number of tasks scheduled for execution in the future.
        /// </summary>
        private static PerformanceCounter _perfCounterScheduledTasks;

        /// <summary>
        /// Counter for the average time a task ready to be executed has to wait before it really starts running.
        /// </summary>
        private static PerformanceCounter _perfCounterAverageLagTime;

        /// <summary>
        /// Base for the average lag counter.
        /// </summary>
        private static PerformanceCounter _perfCounterBaseAverageLagTime;

        /// <summary>
        /// Counter for the number of task exceptions caught per second.
        /// </summary>
        private static PerformanceCounter _perfCounterErrorsPerSecond;

        /// <summary>
        /// Counter for the number of timeouts occurred per second.
        /// </summary>
        private static PerformanceCounter _perfCounterTimeoutsPerSecond;

        /// <summary>
        /// Initializes static members of the <see cref="TaskSupervisor"/> class.
        /// </summary>
        static TaskSupervisor()
        {
            _taskLock = new object();
            _threadsLock = new object();
        }

        /// <summary>
        /// Initializes the supervisor.
        /// </summary>
        public static void Initialize()
        {
            _taskQueue = new Queue<TaskWrapper>();
            _tasks = new List<TaskWrapper>();
            _threads = new List<TaskThread>();

            CreatePerformanceCounters();

            InitializePerformanceCounters();

            _perfCounterMaxThreads.RawValue = TaskMaxThreads;

            ZeroPerformanceCounters();
        }

        /// <summary>
        /// Shuts down the supervisor.
        /// </summary>
        public static void Shutdown()
        {
            if (_taskQueue == null)
            {
                throw new Exception("TaskManager failed to initialize.");
            }

            lock (_taskLock)
            {
                lock (_threadsLock)
                {
                    _threadSLA.Abort();
                    _threadSLA.Join();
                    _threadSLA = null;

                    TaskThread[] toStop = new TaskThread[_threads.Count];

                    _threads.CopyTo(toStop);

                    foreach (TaskThread thread in toStop)
                    {
                        RemoveTaskThread(thread);
                    }
                }
            }

            _perfCounterMaxThreads.RawValue = 0;

            ZeroPerformanceCounters();

            ShudownPerformanceCounters();
        }

        #region Task management

        /// <summary>
        /// Prepares and schedules a task for execution.
        /// </summary>
        /// <param name="task">The task.</param>
        public static void ScheduleTask(TaskWrapper task)
        {
            if (_taskQueue == null)
            {
                throw new Exception("TaskManager failed to initialize.");
            }

            var nextAttempt = task.NextAttempt = DateTime.Now + (task.ConfigurationData.DelayStart ?? ConfigurationHelpers.DefaultDelayStart);

            ScheduleData current = task.ConfigurationData.GetScheduleFor(task.NextAttempt);

            lock (_taskLock)
            {
                _tasks.Add(task);

                for (int index = 0, total = current.Spawn.Value; index < total; index++)
                {
                    _taskQueue.Enqueue(task);

                    _perfCounterScheduledTasks.Increment();
                    _perfCounterTasks.Increment();
                }
            }

            EnsureExecution();
        }

        /// <summary>
        /// Adds a task to the execution list.
        /// </summary>
        /// <param name="task">The task.</param>
        public static void AddTask(TaskWrapper task)
        {
            if (_taskQueue == null)
            {
                throw new Exception("TaskManager failed to initialize.");
            }

            task.NextAttempt = DateTime.Now;

            lock (_taskLock)
            {
                _tasks.Add(task);
                _taskQueue.Enqueue(task);
            }

            _perfCounterScheduledTasks.Increment();
            _perfCounterTasks.Increment();

            EnsureExecution();
        }

        /// <summary>
        /// Terminates a task and removes it from the execution list.
        /// </summary>
        /// <param name="task">The task.</param>
        public static void RemoveTask(TaskWrapper task)
        {
            if (_taskQueue == null)
            {
                throw new Exception("TaskManager failed to initialize.");
            }

            try
            {
                lock (_taskLock)
                {
                    lock (_threadsLock)
                    {
                        AbortTaskThread(task);

                        if (_taskQueue.Count > 0 && _taskQueue.Contains(task))
                        {
                            RemoveTaskFromQueue(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskManagerService.Logger.Log("Exception caught while removing task.", ex);
            }
            finally
            {
                _tasks.Remove(task);
            }
        }

        /// <summary>
        /// Terminates the task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>True whether the task was being executed at the time, false otherwise.</returns>
        private static bool AbortTaskThread(TaskWrapper task)
        {
            bool found = false;

            lock (_threadsLock)
            {
                for (int i = _threads.Count - 1; i >= 0; i--)
                {
                    TaskThread tt = _threads[i];

                    if (tt.CurrentTask == task)
                    {
                        RemoveTaskThread(tt);
                        _perfCounterTasks.Decrement();
                        found = true;
                    }
                }

                SpawnTaskThread();
            }

            return found;
        }

        /// <summary>
        /// Removes a task from the execution list.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>True whether the task was queued for execution, false otherwise.</returns>
        private static bool RemoveTaskFromQueue(TaskWrapper task)
        {
            bool found = false;

            lock (_taskLock)
            {
                int before, spawnCount;

                before = _taskQueue.Count;

                lock (_threadsLock)
                {
                    spawnCount = _taskQueue.Count(t => t == task) + _threads.Count(t => t.CurrentTask == task);
                }

                for (int index = 0, total = _taskQueue.Count; index < total; index++)
                {
                    TaskWrapper front = _taskQueue.Dequeue();

                    if (front != task)
                    {
                        _taskQueue.Enqueue(front);
                    }
                    else
                    {
                        _perfCounterScheduledTasks.Decrement();
                        _perfCounterTasks.Decrement();
                        found = true;
                    }
                }

                Debug.Assert(_taskQueue.Count == before - spawnCount, "Lost track of tasks.");
            }

            return found;
        }

        /// <summary>
        /// Finds a task that is ready to be executed.
        /// </summary>
        /// <param name="thread">The thread that will execute the task.</param>
        /// <returns>A task.</returns>
        /// <remarks>This call might terminate the task execution thread (removing it from the pool) if there are more threads than there are tasks ready to be executed.</remarks>
        private static TaskWrapper GetScheduledTask(TaskThread thread)
        {
            lock (_taskLock)
            {
                if (0 == _taskQueue.Count)
                {
                    return null;
                }

                lock (_threadsLock)
                {
                    if (_threads.Count(t => !t.IsRunning) > _taskQueue.Count)
                    {
                        RemoveTaskThread(thread);
                    }
                }

                for (int i = 0; i < _taskQueue.Count; i++)
                {
                    TaskWrapper task = _taskQueue.Dequeue();
                    Thread.MemoryBarrier();

                    try
                    {
                        DateTime nextAttempt = task.NextAttempt;

                        if (nextAttempt <= DateTime.Now)
                        {
                            double lag = ((double)((TimeSpan)(DateTime.Now - task.NextAttempt)).TotalSeconds) * Stopwatch.Frequency;

                            _perfCounterAverageLagTime.IncrementBy((long)lag);
                            _perfCounterBaseAverageLagTime.Increment();

                            if (task.BurstCounter == 0)
                            {
                                task.BurstStart = DateTime.Now;
                            }

                            _perfCounterScheduledTasks.Decrement();
                            Thread.MemoryBarrier();

                            return task;
                        }

                        _taskQueue.Enqueue(task);
                    }
                    catch (AppDomainUnloadedException)
                    {
                        _perfCounterScheduledTasks.Decrement();
                        Thread.MemoryBarrier();
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Reschedules a task for its next execution time.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="hasMoreWork">Boolean indicating whether the previous execution identified more work available immediately. If true, and the task is configured to allow execution bursts, it might be scheduled for execution immediately.</param>
        /// <remarks>Burst is enabled by allowing more than 1 MaxRuns in a TimeUnit.</remarks>
        private static void RescheduleTask(TaskWrapper task, bool hasMoreWork)
        {
            ScheduleData currentSchedule = task.ConfigurationData.GetScheduleFor(DateTime.Now);

            DateTime nextAttempt = DateTime.MinValue;

            task.BurstCounter++;
            int currentBurstCount = task.BurstCounter;

            if (hasMoreWork)
            {
                if (currentBurstCount < currentSchedule.MaxRuns)
                {
                    nextAttempt = DateTime.Now;
                }
                else
                {
                    task.BurstCounter = 0;
                    nextAttempt = task.BurstStart.Add(currentSchedule.TimeUnit.Value);

                    if (nextAttempt < DateTime.Now)
                    {
                        nextAttempt = DateTime.Now;
                    }
                }
            }
            else
            {
                task.BurstCounter = 0;
                nextAttempt = DateTime.Now.Add(currentSchedule.Wait.Value);
            }

            task.NextAttempt = nextAttempt;
            ScheduleData nextSchedule = task.ConfigurationData.GetScheduleFor(nextAttempt);

            lock (_taskLock)
            {
                int currentSpawnCount;

                lock (_threadsLock)
                {
                    currentSpawnCount = _taskQueue.Count(t => t == task) + _threads.Count(t => t.CurrentTask == task);
                }

                bool extraSpawn = false;

                for (int index = currentSpawnCount, total = nextSchedule.Spawn.Value; index < total; index++)
                {
                    _taskQueue.Enqueue(task);

                    _perfCounterScheduledTasks.Increment();

                    if (extraSpawn)
                    {
                        _perfCounterTasks.Increment(); // TODO: missing .Decrement() when a task is not rescheduled.
                    }

                    extraSpawn = true;
                }

                Thread.MemoryBarrier();
            }
        }

        /// <summary>
        /// Logs an unexpected task exception.
        /// </summary>
        /// <param name="message">A debug message.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>True if the exception was logged.</returns>
        private static bool NotifyException(string message, Exception ex)
        {
            if (ex is ThreadAbortException)
            {
                return false;
            }

            _perfCounterTotalExceptions.Increment();
            _perfCounterErrorsPerSecond.Increment();

            TaskManagerService.Logger.Log(ex);

            return true;
        }

        /// <summary>
        /// The SLA Thread entry point.
        /// </summary>
        /// <remarks>
        /// The SLA Thread ensures that all tasks ready to be executed will start ASAP by spawning more execution threads, if possible, 
        /// and that tasks whose execution time exceeds the configured execution timeout will be aborted.
        /// </remarks>
        private static void SLALoopWorker()
        {
            while (true)
            {
                bool spawnNewThread = false;
                bool hasThreadAbort = false;

                lock (_taskLock)
                {
                    lock (_threadsLock)
                    {
                        if (_taskQueue.Count > 0)
                        {
                            for (int index = 0, total = _taskQueue.Count; index < total; index++)
                            {
                                TaskWrapper task = _taskQueue.Dequeue();

                                #region Figure out if we need to spawn a new thread, if this task is late.

                                try
                                {
                                    DateTime nextAttemptDate = task.NextAttempt;

                                    ScheduleData nextSchedule = task.ConfigurationData.GetScheduleFor(nextAttemptDate);

                                    if (nextAttemptDate.AddSeconds(nextSchedule.SLA.Value.TotalSeconds) < DateTime.Now)
                                    {
                                        spawnNewThread = true;
                                    }

                                    _taskQueue.Enqueue(task);
                                }
                                catch (AppDomainUnloadedException)
                                {
                                    _perfCounterScheduledTasks.Decrement();
                                    Thread.MemoryBarrier();
                                }

                                #endregion
                            }
                        }

                        if (_tasks.Count > 0)
                        {
                            for (int index = 0, total = _tasks.Count; index < total; index++)
                            {
                                TaskWrapper task = _tasks[index];

                                #region Figure out if we need to spawn task clones, if the current schedule requires more spawned clones than there are clones currently running

                                try
                                {
                                    ScheduleData currentSchedule = task.ConfigurationData.GetScheduleFor(DateTime.Now);

                                    int currentSpawnCount = _taskQueue.Count(t => t == task) + _threads.Count(t => t.CurrentTask == task);

                                    for (int spawnIndex = currentSpawnCount, spawnTotal = currentSchedule.Spawn.Value; spawnIndex < spawnTotal; spawnIndex++)
                                    {
                                        _taskQueue.Enqueue(task);
                                        _perfCounterScheduledTasks.Increment();
                                        _perfCounterTasks.Increment();
                                    }
                                }
                                catch (AppDomainUnloadedException)
                                {
                                    // The AppDomain is gone.
                                    // May happens during module shutdown (files were updated) or service shutdown; that's ok.
                                    // The spawn a new clone check will deal with it.
                                }

                                #endregion
                            }
                        }

                        for (int i = _threads.Count - 1; i >= 0; i--)
                        {
                            TaskThread thread = _threads[i];

                            try
                            {
                                if (thread.IsRunning &&
                                    thread.StartingTime != DateTime.MinValue &&
                                    null != thread.CurrentTask &&
                                    thread.CurrentTask.HasTimedOut() &&
                                    _threads.Count > 1)
                                {
                                    TaskWrapper currentTask = thread.CurrentTask;

                                    if (null != currentTask)
                                    {
                                        RemoveTaskThread(thread);
                                        SpawnTaskThread();
                                        _perfCounterTotalTimeouts.Increment();
                                        _perfCounterTimeoutsPerSecond.Increment();
                                        Thread.MemoryBarrier();
                                        RescheduleTask(currentTask, false);
                                        hasThreadAbort = true;
                                    }
                                }
                            }
                            catch (AppDomainUnloadedException)
                            {
                                // The AppDomain is gone.
                                // May happens during module shutdown (files were updated) or service shutdown; that's ok.
                                // The spawn a new clone check will deal with it.
                            }
                        }
                    }
                }

                if (spawnNewThread && !hasThreadAbort)
                {
                    SpawnTaskThread();
                }

                Thread.Sleep(1000);
            }
        }

        #endregion

        #region Thread management

        /// <summary>
        /// Ensures that the SLA Thread is running, and that there is at least one execution thread active.
        /// </summary>
        private static void EnsureExecution()
        {
            if (_threadSLA == null)
            {
                _threadSLA = new Thread(new ThreadStart(SLALoopWorker));
                _threadSLA.Name = "SLA Thread";
                _threadSLA.Start();
            }

            lock (_threadsLock)
            {
                foreach (TaskThread thread in _threads)
                {
                    if (!thread.IsRunning)
                    {
                        return;
                    }
                }

                SpawnTaskThread();
            }
        }

        /// <summary>
        /// Starts an execution thread, adding it to the pool.
        /// </summary>
        private static void SpawnTaskThread()
        {
            lock (_threadsLock)
            {
                if (_threads.Count < TaskMaxThreads)
                {
                    TaskThread newThread = new TaskThread();
                    _threads.Add(newThread);
                    newThread.Start();
                    _perfCounterSpawnedThreads.Increment();
                }
            }
        }

        /// <summary>
        /// Stops an execution thread, removing it from the pool.
        /// </summary>
        /// <param name="thread">The task thread.</param>
        private static void RemoveTaskThread(TaskThread thread)
        {
            lock (_threadsLock)
            {
                _threads.Remove(thread);
                _perfCounterSpawnedThreads.Decrement();
            }

            thread.Stop();
        }

        /// <summary>
        /// Called by the execution thread loop to notify the supervisor that it can't find any task ready to be executed.
        /// </summary>
        /// <param name="thread">The task thread.</param>
        private static void NotifyIdleThread(TaskThread thread)
        {
            lock (_threadsLock)
            {
                if (_threads.Count > 1)
                {
                    RemoveTaskThread(thread);
                }
            }
        }

        /// <summary>
        /// Notifies the supervisor that a task has started executing.
        /// </summary>
        /// <param name="task">The task.</param>
        private static void NotifyStart(TaskWrapper task)
        {
            _perfCounterTasksRunning.Increment();
        }

        /// <summary>
        /// Notifies the supervisor that a task has finished executing.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="ticks">How long the task took to run, in system ticks.</param>
        private static void NotifyEnd(TaskWrapper task, long ticks)
        {
            if (ticks != -1)
            {
                _perfCounterAverageExecutionTime.IncrementBy(ticks);
                _perfCounterBaseAverageExecutionTime.Increment();
            }

            _perfCounterTasksRunning.Decrement();
        }

        #region Counters

        /// <summary>
        /// Initializes the performance counters.
        /// </summary>
        private static void InitializePerformanceCounters()
        {
            _perfCounterScheduledTasks = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_SCHEDULED_TASKS, false);
            _perfCounterSpawnedThreads = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_SPAWNED_THREADS, false);
            _perfCounterMaxThreads = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_MAX_THREADS, false);
            _perfCounterTasks = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_TASKS, false);
            _perfCounterTasksRunning = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_RUNNING_TASKS, false);
            _perfCounterAverageExecutionTime = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_AVERAGE_TASK_TIME, false);
            _perfCounterBaseAverageExecutionTime = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.BASE_COUNTER_AVERAGE_TASK_TIME, false);
            _perfCounterTotalExceptions = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_EXCEPTIONS, false);
            _perfCounterTotalTimeouts = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_TIMEOUTS, false);
            _perfCounterAverageLagTime = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_AVERAGE_TASK_LAG, false);
            _perfCounterBaseAverageLagTime = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.BASE_COUNTER_AVERAGE_TASK_LAG, false);
            _perfCounterErrorsPerSecond = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_EXCEPTIONS_PER_SECOND, false);
            _perfCounterTimeoutsPerSecond = new PerformanceCounter(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.COUNTER_TIMEOUTS_PER_SECOND, false);
        }

        /// <summary>
        /// Registers the performance counters with the operating system.
        /// </summary>
        private static void CreatePerformanceCounters()
        {
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();

            ccdc.AddRange(TaskManagerInstaller.COUNTERS);

            if (!PerformanceCounterCategory.Exists(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY))
            {
                _performanceCounterCategory = PerformanceCounterCategory.Create(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY, TaskManagerInstaller.PERFORMANCE_COUNTER_DESCRIPTION, PerformanceCounterCategoryType.SingleInstance, ccdc);
            }
        }

        /// <summary>
        /// Terminates the performance counters.
        /// </summary>
        private static void ShudownPerformanceCounters()
        {
            _perfCounterSpawnedThreads.Close();
            _perfCounterMaxThreads.Close();
            _perfCounterTasks.Close();
            _perfCounterTasksRunning.Close();
            _perfCounterAverageExecutionTime.Close();
            _perfCounterBaseAverageExecutionTime.Close();
            _perfCounterTotalExceptions.Close();
            _perfCounterScheduledTasks.Close();
            _perfCounterTotalTimeouts.Close();
            _perfCounterAverageLagTime.Close();
            _perfCounterBaseAverageLagTime.Close();
            _perfCounterErrorsPerSecond.Close();
            _perfCounterTimeoutsPerSecond.Close();

            if (null != _performanceCounterCategory)
            {
                PerformanceCounterCategory.Delete(TaskManagerInstaller.PERFORMANCE_COUNTER_CATEGORY);
            }
        }
        
        /// <summary>
        /// Zeroes performance counter values.
        /// </summary>
        private static void ZeroPerformanceCounters()
        {
            _perfCounterSpawnedThreads.RawValue = 0;
            _perfCounterTasks.RawValue = 0;
            _perfCounterTasksRunning.RawValue = 0;
            _perfCounterAverageExecutionTime.RawValue = 0;
            _perfCounterBaseAverageExecutionTime.RawValue = 0;
            _perfCounterTotalExceptions.RawValue = 0;
            _perfCounterTotalTimeouts.RawValue = 0;
            _perfCounterScheduledTasks.RawValue = 0;
            _perfCounterAverageLagTime.RawValue = 0;
            _perfCounterBaseAverageLagTime.RawValue = 0;
            _perfCounterErrorsPerSecond.RawValue = 0;
            _perfCounterTimeoutsPerSecond.RawValue = 0;
        }

        #endregion

        /// <summary>
        /// Represents an worker thread that runs tasks.
        /// </summary>
        private class TaskThread
        {
            /// <summary>
            /// A counter for the number of created threads.
            /// </summary>
            private static int _id = 0;

            /// <summary>
            /// The <see cref="T:System.Thread"/> itself.
            /// </summary>
            private Thread _thread;

            /// <summary>
            /// The task currently being executed.
            /// </summary>
            private TaskWrapper _task;

            /// <summary>
            /// An unique identifier for this execution thread.
            /// </summary>
            private int _taskThreadId;

            /// <summary>
            /// Gets a value indicating whether this thread is executing a task.
            /// </summary>
            public bool IsRunning { get; private set; }

            /// <summary>
            /// Gets a value indicating when this thread started executing the current task.
            /// </summary>
            public DateTime StartingTime { get; private set; }

            /// <summary>
            /// Gets the task currently being executed.
            /// </summary>
            public TaskWrapper CurrentTask
            {
                get
                {
                    return this._task;
                }
            }

            /// <summary>
            /// Gets the current thread id.
            /// </summary>
            public int TaskThreadId
            {
                get
                {
                    return this._taskThreadId;
                }
            }

            /// <summary>
            /// Starts the execution thread.
            /// </summary>
            public void Start()
            {
                lock (typeof(TaskThread))
                {
                    _id++;
                    if (_id == int.MaxValue)
                    {
                        _id = 0;
                    }
                }

                if (null != this._thread)
                {
                    this.Stop();
                }

                this._thread = new Thread(new ThreadStart(this.Loop));
                this._thread.Name = "Task thread #" + (this._taskThreadId = _id).ToString();
                this._thread.Start();
            }

            /// <summary>
            /// Stops the execution thread, aborting the current executing task, if any.
            /// </summary>
            public void Stop()
            {
                if (null != this._thread)
                {
                    this._thread.Abort();
                    this._thread.Join();
                }

                this._thread = null;
            }

            /// <summary>
            /// The thread loop.
            /// </summary>
            private void Loop()
            {
                DateTime lastRun = DateTime.Now;

                while (true)
                {
                    lock (TaskSupervisor._taskLock)
                    {
                        this._task = TaskSupervisor.GetScheduledTask(this);
                    }

                    System.Threading.Thread.MemoryBarrier();

                    if (null != this._task)
                    {
                        bool result = false;

                        long runTime = -1;

                        TaskSupervisor.NotifyStart(this._task);

                        this.StartingTime = lastRun = DateTime.Now;
                        this.IsRunning = true;
                        System.Threading.Thread.MemoryBarrier();

                        Stopwatch timer = Stopwatch.StartNew();

                        try
                        {
                            result = this._task.Execute();

                            runTime = timer.ElapsedTicks;
                        }
                        catch (ThreadAbortException)
                        {
                            this._task = null;
                            throw;
                        }
                        catch (AppDomainUnloadedException)
                        {
                            this._task = null;
                            throw;
                        }
                        catch (Exception ex)
                        {
                            this.IsRunning = false;
                            if (!TaskSupervisor.NotifyException("Exception caught while executing task.", ex))
                            {
                                this._task = null;
                                System.Threading.Thread.MemoryBarrier();
                                throw;
                            }
                        }
                        finally
                        {
                            lastRun = DateTime.Now;
                            this.IsRunning = false;
                            this.StartingTime = DateTime.MinValue;
                            System.Threading.Thread.MemoryBarrier();
                            TaskSupervisor.NotifyEnd(this._task, runTime);
                        }

                        lock (TaskSupervisor._taskLock)
                        {
                            try
                            {
                                TaskWrapper toReturn = this._task;
                                this._task = null;
                                TaskSupervisor.RescheduleTask(toReturn, result);
                            }
                            catch (AppDomainUnloadedException)
                            {
                            }

                            this._task = null;
                        }

                        System.Threading.Thread.MemoryBarrier();

                        Thread.Sleep(100);
                    }
                    else
                    {
                        if (lastRun.AddSeconds(IdleTimeout) <= DateTime.Now)
                        {
                            TaskSupervisor.NotifyIdleThread(this);
                        }

                        Thread.Sleep(250);
                    }
                }
            }
        }

        #endregion
    }
}
