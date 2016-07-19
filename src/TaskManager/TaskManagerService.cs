namespace TaskManager
{
    using System;
    using System.Diagnostics;
    using System.ServiceProcess;

    /// <summary>
    /// Implements the Windows service interface for the Task Manager.
    /// </summary>
    public partial class TaskManagerService : ServiceBase
    {
        /// <summary>
        /// The name used in the Windows Event Log.
        /// </summary>
        internal static readonly string LogName = TaskManagerInstaller.SERVICE_DESCRIPTION;

        /// <summary>
        /// The source name used in the Windows Event Log.
        /// </summary>
        internal static readonly string LogSource = "TaskManager Service";     

        /// <summary>
        /// The logger instance.
        /// </summary>
        private static ILogger _logger;
        private static IEventLog _eventLog = new ConsoleEventLog();

        /// <summary>
        /// Initializes static members of the <see cref="TaskManagerService"/> class.
        /// </summary>
        static TaskManagerService()
        {
            _logger = new Logger();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManagerService"/> class.
        /// </summary>
        public TaskManagerService()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        public static ILogger Logger
        {
            get
            {
                return TaskManagerService._logger;
            }
        }

		/// <summary>
		/// Initialize the service.
		/// </summary>
		/// <param name="eventLog">Event log.</param>
        public static void Initialize(IEventLog eventLog)
        {
            _eventLog = eventLog;
        }

        /// <summary>
        /// Logs an information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogInfo(string message)
        {
            lock (_eventLog)
            {
                _eventLog.WriteInfo(message);
            }            
        }

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">An exception.</param>
        public static void LogError(string message, Exception ex)
        {            
            lock (_eventLog)
            {
                if (ex is System.Threading.ThreadAbortException)
                {
                    _eventLog.WriteWarning("Task scheduling and execution aborted.");
                }
                else
                {
                    var msg = string.Format("An error ocurred during task execution:\r\n\r\n{0}\r\n\r\n{1}: {2}\r\n\r\nStacktrace:\r\n{3}", message, ex.GetType().Name, ex.Message, ex.StackTrace);
                    _eventLog.WriteError(msg);
                }                    
            }            
        }

        /// <summary>
        /// Called when the service is being started by the SCM.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected override void OnStart(string[] args)
        {
////#if DEBUG
////            System.Threading.Thread.Sleep(10000);
////#endif

            LogInfo("Starting service...");
            try
            {
                var eventLog = args.Length > 0 ? ArgumentsHelper.CreateEventLog(args[0]) : new WindowsEventLog();
                var statsStrategy = args.Length > 1 ? ArgumentsHelper.CreateStatsStrategy(args[1]) : new PerformanceCounterStatsStrategy();

                Initialize(eventLog);
                TaskSupervisor.Initialize(statsStrategy);
                ModuleSupervisor.Initialize();

                LogInfo("Service successfully started...");

                ModuleSupervisor.Execute();
            }
            catch (Exception e)
            {
                LogError("Unable to start service.", e);
                throw;
            }
        }

        /// <summary>
        /// Called when the service is being stopped by the SCM.
        /// </summary>
        protected override void OnStop()
        {
            LogInfo("Stopping service...");
            try
            {
                ModuleSupervisor.Shutdown();
                TaskSupervisor.Shutdown();
                LogInfo("Service successfully stopped...");
            }
            catch (Exception e)
            {
                LogError("Unable to stop service.", e);
                throw;
            }

            lock (_eventLog)
            {
                _eventLog.Close();
                _eventLog = null;
            }
        }
    }
}
