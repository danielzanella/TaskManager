namespace TaskManager
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Xml.Linq;
    using TaskManager.Common;
    using TaskManager.Configuration;

    /// <summary>
    /// Wraps a task instance locally and provides a proxy for the supervisor AppDomain.
    /// </summary>
    [Serializable]
    public class TaskWrapper : MarshalByRefObject, ITaskModule
    {
        /// <summary>
        /// The task instance.
        /// </summary>
        private ITaskModule _task;

        /// <summary>
        /// The remote logger.
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// The configuration data.
        /// </summary>
        private TaskConfigurationData _configurationData;

        /// <summary>
        /// The date and time when the task will be executed next.
        /// </summary>
        private DateTime _nextAttempt;

        /// <summary>
        /// Number of times the task has been executed in this execution burst.
        /// </summary>
        private int _burstCounter;

        /// <summary>
        /// The date and time this execution burst started.
        /// </summary>
        private DateTime _burstStart;

        /// <summary>
        /// The path to the assembly file.
        /// </summary>
        private string _dllFile;

        /// <summary>
        /// The task name.
        /// </summary>
        private string _taskName;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWrapper"/> class.
        /// </summary>
        /// <param name="dllFile">The path to the assembly file.</param>
        /// <param name="taskModule">The task instance.</param>
        /// <param name="configurationData">The configuration data.</param>
        /// <param name="logger">The proxy to the remote logger.</param>
        internal TaskWrapper(string dllFile, ITaskModule taskModule, TaskConfigurationData configurationData, ILogger logger)
        {
            this._dllFile = dllFile;
            this._task = taskModule;
            this._logger = logger;
            this._configurationData = configurationData;
            this._taskName = taskModule.GetType().FullName;
        }

        /// <summary>
        /// Gets the current task configuration.
        /// </summary>
        public TaskConfigurationData ConfigurationData
        {
            get
            {
                return this._configurationData;
            }
        }

        /// <summary>
        /// Gets or sets when the task should be executed.
        /// </summary>
        public DateTime NextAttempt
        {
            get
            {
                return this._nextAttempt;
            }

            set
            {
                this._nextAttempt = value;
            }
        }

        /// <summary>
        /// Gets or sets how many times the task has been executed in the current burst.
        /// </summary>
        public int BurstCounter
        {
            get
            {
                return this._burstCounter;
            }

            set
            {
                this._burstCounter = value;
            }
        }

        /// <summary>
        /// Gets or sets when the current burst started.
        /// </summary>
        public DateTime BurstStart
        {
            get
            {
                return this._burstStart;
            }

            set
            {
                this._burstStart = value;
            }
        }

        /// <summary>
        /// Gets the path to the assembly file.
        /// </summary>
        public string DLLFile
        {
            get
            {
                return this._dllFile;
            }
        }

        /// <summary>
        /// Gets the task name.
        /// </summary>
        public string TaskName
        {
            get
            {
                return this._taskName;
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>True if the task has determined that there is more work ready to be done, false otherwise.</returns>
        public bool Execute()
        {
            AppDomain domain = AppDomain.CurrentDomain;
            string name = domain.FriendlyName;

            CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;

            if (null != this._configurationData.Culture)
            {
                Thread.CurrentThread.CurrentCulture = this._configurationData.Culture;
            }

            try
            {
                return this._task.Execute();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        /// <summary>
        /// Configures the task using the custom configuration data present in the xml configuration file.
        /// </summary>
        /// <param name="node">The xml node.</param>
        public void Configure(XElement node)
        {
            this._task.Configure(node);
        }

        /// <summary>
        /// Determines whether the task has timed out.
        /// </summary>
        /// <returns>True if the task is being executed for longer than the timeout allows, false otherwise.</returns>
        public bool HasTimedOut()
        {
            return this.BurstStart.Add(this.ConfigurationData.GetScheduleFor(this.BurstStart).Timeout.Value) < DateTime.Now;
        }

        /// <summary>
        /// Keeps the remote proxy (running in the supervisor AppDomain) alive until this AppDomain is unloaded.
        /// </summary>
        /// <returns>Null reference.</returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
