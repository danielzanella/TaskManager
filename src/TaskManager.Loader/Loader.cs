namespace TaskManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using TaskManager.Common;
    using TaskManager.Configuration;

    /// <summary>
    /// Implements a loader used to inspect assemblies to find task modules, in their own separate AppDomain.
    /// </summary>
    public class Loader : MarshalByRefObject
    {
        /// <summary>
        /// The remote logging component.
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// The base path.
        /// </summary>
        private string _baseDirectory;

        /// <summary>
        /// Inspects the assemblies in the current AppDomain and returns any valid tasks found.
        /// </summary>
        /// <param name="dllFile">The path to the task module assembly file.</param>
        /// <param name="xmlFile">The path to the task module configuration file.</param>
        /// <param name="logger">The remote logging component provided by the module supervisor.</param>
        /// <param name="baseDirectory">The base path.</param>
        /// <returns>Array containing any valid tasks found.</returns>
        public TaskWrapper[] LoadAndConfigure(string dllFile, string xmlFile, ILogger logger, string baseDirectory)
        {
            this._logger = logger;
            this._baseDirectory = baseDirectory;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(this.CurrentDomain_AssemblyResolve);

            List<TaskWrapper> result = new List<TaskWrapper>();

            Assembly dll = Assembly.LoadFrom(dllFile);

            IEnumerable<TaskConfigurationData> taskConfigurationData = xmlFile.ToTaskConfiguration();

            logger.Log(string.Format("Found {0} task definitions in configuration file.", taskConfigurationData.Count()));

            foreach (TaskConfigurationData task in taskConfigurationData)
            {
                if (string.IsNullOrWhiteSpace(task.TypeName))
                {
                    logger.Log("Invalid configuration: no type name specified.");
                    continue;
                }

                string targetTypeName = task.TypeName;

                if (null == task.ConfigurationData)
                {
                    logger.Log("Invalid configuration: no module configuration specified.");
                    continue;
                }

                int slaLimit = (int)(task.SLA ?? ConfigurationHelpers.DefaultSLA).TotalSeconds;

                int timeout = (int)(task.Timeout ?? ConfigurationHelpers.DefaultTimeout).TotalSeconds;

                int delayStart = (int)(task.DelayStart ?? ConfigurationHelpers.DefaultDelayStart).TotalSeconds;

                Type targetType = dll.GetType(targetTypeName, false);

                if (null != targetType)
                {
                    ITaskModule taskModuleInstance = null;

                    try
                    {
                        taskModuleInstance = (ITaskModule)Activator.CreateInstance(targetType);

                        XElement element = task.ConfigurationData.ToXml();

                        taskModuleInstance.Configure(element);
                    }
                    catch (Exception e)
                    {
                        logger.Log(string.Format("Unable to load task '{0}'", targetType.FullName), e);
                        throw;
                    }

                    logger.Log(string.Format("Task '{0}' successfully configured and initialized. ({1} schedules, {4}s start delay, {2}s SLA, {3}s timeout)", targetType.FullName, (task.Schedule ?? new ScheduleData[0]).Count(), slaLimit, timeout, delayStart));
                    result.Add(new TaskWrapper(dllFile, taskModuleInstance, task, logger));
                }
            }

            return result.ToArray();
        }
        
        /// <summary>
        /// Keeps the remote proxy (running in the supervisor AppDomain) alive until this AppDomain is unloaded.
        /// </summary>
        /// <returns>Null reference.</returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// Handles the AssemblyResolve event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event arguments.</param>
        /// <returns>The resolved assembly.</returns>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string temp = args.Name.Split(',')[0];

            // Ensures that only the current version of the task manager assemblies are loaded.
            if (string.Compare(temp, "TaskManager.Loader") == 0)
            {
                return Assembly.LoadFrom(Path.Combine(this._baseDirectory, "TaskManager.Loader.dll"));
            }

            if (string.Compare(temp, "TaskManager.Common") == 0)
            {
                return Assembly.LoadFrom(Path.Combine(this._baseDirectory, "TaskManager.Common.dll"));
            }

            return null;
        }

        /// <summary>
        /// Handles the UnhandledException event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception te = e.ExceptionObject as Exception;

            if (null == te && e.ExceptionObject != null)
            {
                te = new Exception("Fatal error: " + e.ExceptionObject.ToString());
            }

            if (null == te)
            {
                te = new Exception("Unknown error.");
            }

            this._logger.Log(string.Format("Fatal error caught in AppDomain '{0}'", AppDomain.CurrentDomain.FriendlyName), te);
        }
    }
}