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
        /// The EventLog instance.
        /// </summary>
        private static EventLog _eventLog;

        /// <summary>
        /// The logger instance.
        /// </summary>
        private static ILogger _logger;

        /// <summary>
        /// Initializes static members of the <see cref="TaskManagerService"/> class.
        /// </summary>
        static TaskManagerService()
        {
            _eventLog = new EventLog(TaskManagerService.LogName, ".", TaskManagerService.LogSource);

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
        /// Logs an information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogInfo(string message)
        {
            if (null == _eventLog)
            {
                Console.WriteLine("    " + message);
            }
            else
            {
                lock (_eventLog)
                {
                    _eventLog.WriteEntry(message, EventLogEntryType.Information);
                }
            }
        }

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">An exception.</param>
        public static void LogError(string message, Exception ex)
        {
            if (null == _eventLog)
            {
                Console.WriteLine(string.Format("*** {0}: {2} ({1})", message, ex.GetType().Name, ex.Message));
            }
            else
            {
                lock (_eventLog)
                {
                    EventLogEntryType logEntryType = EventLogEntryType.Error;
                    string log = null;

                    if (ex is System.Threading.ThreadAbortException)
                    {
                        log = "Task scheduling and execution aborted.";
                        logEntryType = EventLogEntryType.Warning;
                    }
                    else
                    {
                        log = string.Format("An error ocurred during task execution:\r\n\r\n{0}\r\n\r\n{1}: {2}\r\n\r\nStacktrace:\r\n{3}", message, ex.GetType().Name, ex.Message, ex.StackTrace);
                    }

                    _eventLog.WriteEntry(log, logEntryType);
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
                TaskSupervisor.Initialize();
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
