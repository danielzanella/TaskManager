namespace TaskManager
{
    using System;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.ServiceProcess;

    /// <summary>
    /// Service installer.
    /// </summary>
    [RunInstaller(true)]
    public partial class TaskManagerInstaller : Installer
    {
        public static CounterCreationData[] COUNTERS;

        public static readonly string SERVICE_NAME = System.Configuration.ConfigurationManager.AppSettings["Instance.Name"] ?? "TaskManager";
        public static readonly string SERVICE_DESCRIPTION = System.Configuration.ConfigurationManager.AppSettings["Instance.Description"] ?? "Task Manager";

        public static readonly string PERFORMANCE_COUNTER_CATEGORY = System.Configuration.ConfigurationManager.AppSettings["Instance.Description"] ?? "Task Manager";
        public static readonly string PERFORMANCE_COUNTER_DESCRIPTION = (System.Configuration.ConfigurationManager.AppSettings["Instance.Description"] ?? "Task Manager") + " performance counters";

        public const string COUNTER_SPAWNED_THREADS = "# Threads";
        public const string COUNTER_MAX_THREADS = "# Max Threads";
        public const string COUNTER_TASKS = "# Tasks";
        public const string COUNTER_RUNNING_TASKS = "# Running Tasks";
        public const string COUNTER_AVERAGE_TASK_TIME = "Avg. Execution Time";
        public const string BASE_COUNTER_AVERAGE_TASK_TIME = "Avg. Execution Time Base";
        public const string COUNTER_EXCEPTIONS = "# Errors";
        public const string COUNTER_TIMEOUTS = "# Timeouts";
        public const string COUNTER_SCHEDULED_TASKS = "# Scheduled Tasks";
        public const string COUNTER_AVERAGE_TASK_LAG = "Avg. Lag Time";
        public const string BASE_COUNTER_AVERAGE_TASK_LAG = "Avg. Lag Time Base";
        public const string COUNTER_EXCEPTIONS_PER_SECOND = "# Errors/sec";
        public const string COUNTER_TIMEOUTS_PER_SECOND = "# Timeouts/sec";

        private ServiceInstaller _serviceInstaller;
        private ServiceProcessInstaller _serviceProcessInstaller;
        private EventLogInstaller _eventLogInstaller;
        private PerformanceCounterInstaller _perfCounterInstaller;

        /// <summary>
        /// Initializes static members of the <see cref="TaskManagerInstaller"/> class.
        /// </summary>
        static TaskManagerInstaller()
        {
            COUNTERS = new CounterCreationData[13];

            COUNTERS[0] = new CounterCreationData(COUNTER_SPAWNED_THREADS, "Number of active threads.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[1] = new CounterCreationData(COUNTER_MAX_THREADS, "Number of threads allowed.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[2] = new CounterCreationData(COUNTER_TASKS, "Number of spawned tasks.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[3] = new CounterCreationData(COUNTER_RUNNING_TASKS, "Number of running tasks.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[4] = new CounterCreationData(COUNTER_AVERAGE_TASK_TIME, "Average execution time, in seconds.", PerformanceCounterType.AverageTimer32);
            COUNTERS[5] = new CounterCreationData(BASE_COUNTER_AVERAGE_TASK_TIME, "Average execution time base counter.", PerformanceCounterType.AverageBase);
            COUNTERS[6] = new CounterCreationData(COUNTER_EXCEPTIONS, "Number of errors encountered.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[7] = new CounterCreationData(COUNTER_SCHEDULED_TASKS, "Number of scheduled tasks.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[8] = new CounterCreationData(COUNTER_TIMEOUTS, "Number of timeouts occurred.", PerformanceCounterType.NumberOfItems32);
            COUNTERS[9] = new CounterCreationData(COUNTER_AVERAGE_TASK_LAG, "Average task lag time, in seconds.", PerformanceCounterType.AverageTimer32);
            COUNTERS[10] = new CounterCreationData(BASE_COUNTER_AVERAGE_TASK_LAG, "Average lag time base counter.", PerformanceCounterType.AverageBase);
            COUNTERS[11] = new CounterCreationData(COUNTER_EXCEPTIONS_PER_SECOND, "Number of errors per second.", PerformanceCounterType.RateOfCountsPerSecond32);
            COUNTERS[12] = new CounterCreationData(COUNTER_TIMEOUTS_PER_SECOND, "Number of timeouts per second.", PerformanceCounterType.RateOfCountsPerSecond32);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManagerInstaller"/> class.
        /// </summary>
        public TaskManagerInstaller()
        {
            this.InitializeComponent();

            this._serviceInstaller = new ServiceInstaller();
            this._serviceProcessInstaller = new ServiceProcessInstaller();
            this._eventLogInstaller = new EventLogInstaller();

            this._serviceInstaller.ServiceName = SERVICE_NAME;
            this._serviceInstaller.StartType = ServiceStartMode.Automatic;
            this._serviceInstaller.Description = SERVICE_DESCRIPTION;

            this._serviceProcessInstaller.Account = ServiceAccount.NetworkService;

            this._eventLogInstaller.Log = TaskManagerService.LogName;
            this._eventLogInstaller.Source = TaskManagerService.LogSource;

            this.Installers.Add(this._serviceInstaller);
            this.Installers.Add(this._serviceProcessInstaller);
            this.Installers.Add(this._eventLogInstaller);

            this._perfCounterInstaller = new PerformanceCounterInstaller();
            this._perfCounterInstaller.CategoryName = PERFORMANCE_COUNTER_CATEGORY;
            this._perfCounterInstaller.CategoryHelp = PERFORMANCE_COUNTER_DESCRIPTION;
            this._perfCounterInstaller.CategoryType = PerformanceCounterCategoryType.SingleInstance;

            this._perfCounterInstaller.Counters.AddRange(COUNTERS);

            this.Installers.Add(this._perfCounterInstaller);
        }
    }
}