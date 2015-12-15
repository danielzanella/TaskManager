namespace TaskManager.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using TaskManager.Common;

    /// <summary>
    /// Represents task configuration information.
    /// </summary>
    [Serializable, CLSCompliant(true)]
    public class TaskConfigurationData : ITaskConfigurationData
    {
        /// <summary>
        /// Gets or sets the task class type name.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the task culture.
        /// </summary>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Gets or sets the number of task instances spawned.
        /// </summary>
        /// <remarks>Default is to spawn a single instance.</remarks>
        public int? Spawn { get; set; }

        /// <summary>
        /// Gets or sets the amount of time the SLA scheduler waits before it considers the task execution is blocked/has hanged/in error and restarts it.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the amount of time the task is allowed to be late while waiting for an executing thread.
        /// </summary>
        /// <remarks>If the task doesn't start executing in this amount of time (after it is marked to start), and the number of executing threads is below the maximum number of threads allowed, the SLA scheduler spawns a new thread for the task.</remarks>
        public TimeSpan? SLA { get; set; }

        /// <summary>
        /// Gets or sets the minimum amount of time allowed per execution burst.
        /// </summary>
        public TimeSpan? TimeUnit { get; set; }

        /// <summary>
        /// Gets or sets the number of times a task runs per execution burst.
        /// </summary>
        public int? MaxRuns { get; set; }

        /// <summary>
        /// Gets or sets the amount of time to wait after an empty or failed execution burst.
        /// </summary>
        public TimeSpan? Wait { get; set; }

        /// <summary>
        /// Gets or sets the amount of time to wait before scheduling the first execution, after the module is loaded.
        /// </summary>
        public TimeSpan? DelayStart { get; set; }

        /// <summary>
        /// Gets or sets the list of schedules.
        /// </summary>
        public IEnumerable<ScheduleData> Schedule { get; set; }

        /// <summary>
        /// Gets or sets the raw task configuration data.
        /// </summary>
        public string ConfigurationData { get; set; }

        /// <summary>
        /// Loads a configuration file.
        /// </summary>
        /// <param name="fileName">The path to the configuration file.</param>
        /// <returns>The configuration information.</returns>
        public static IEnumerable<TaskConfigurationData> LoadFrom(string fileName)
        {
            XDocument doc = XDocument.Load(fileName, LoadOptions.SetLineInfo);

            XElement root = doc.Root;

            if (null == root || root.Name != ConfigurationHelpers.ConfigRootElementName)
            {
                throw new InvalidConfigurationException("Invalid configuration file content.");
            }

            var taskElements = root.Elements(ConfigurationHelpers.ConfigTaskElementName);

            List<TaskConfigurationData> taskConfigurations = new List<TaskConfigurationData>();

            foreach (XElement taskElement in taskElements)
            {
                TaskConfigurationData cfg = new TaskConfigurationData();

                #region Task type

                try
                {
                    cfg.TypeName = (string)taskElement.Attribute(ConfigurationHelpers.ConfigTypeAttributeName);
                }
                catch
                {
                    throw new InvalidConfigurationException(taskElement.MakeValueErrorMessage(ConfigurationHelpers.ConfigTypeAttributeName));
                }

                #endregion

                if (string.IsNullOrWhiteSpace(cfg.TypeName)) 
                {
                    throw new InvalidConfigurationException(ConfigurationHelpers.MakeValueErrorMessage(taskElement, ConfigurationHelpers.ConfigTypeAttributeName));
                }

                string cultureName = (string)taskElement.Attribute(ConfigurationHelpers.ConfigCultureAttributeName);

                if (!string.IsNullOrWhiteSpace(cultureName))
                {
                    CultureInfo info = CultureInfo.GetCultureInfo(cultureName);

                    if (null != info)
                    {
                        cfg.Culture = info;
                    }
                }

                cfg.ApplyCommonInfoFromElement(taskElement);

                try
                {
                    cfg.DelayStart = ((string)taskElement.Attribute(ConfigurationHelpers.ConfigDelayStartName)).ToTimeSpan();
                }
                catch
                {
                    throw new InvalidConfigurationException(taskElement.MakeValueErrorMessage(ConfigurationHelpers.ConfigDelayStartName));
                }

                XElement scheduleList;

                #region Schedule list

                try
                {
                    scheduleList = taskElement.Elements(ConfigurationHelpers.ConfigScheduleListElementName).SingleOrDefault();
                }
                catch
                {
                    throw new InvalidConfigurationException(taskElement.MakeElementErrorMessage("Task configuration has too many schedule lists."));
                }

                #endregion

                List<ScheduleData> scheds = new List<ScheduleData>();

                if (scheduleList != null)
                {
                    foreach (XElement scheduleElement in scheduleList.Elements(ConfigurationHelpers.ConfigScheduleItemElementName))
                    {
                        ScheduleData sched = new ScheduleData();

                        #region From

                        try
                        {
                            sched.From = ((string)scheduleElement.Attribute(ConfigurationHelpers.ConfigFromAttributeName)).ToTimeSpan().Value;
                        }
                        catch
                        {
                            throw new InvalidConfigurationException(scheduleElement.MakeValueErrorMessage(ConfigurationHelpers.ConfigFromAttributeName));
                        }

                        #endregion From

                        #region To

                        try
                        {
                            sched.To = ((string)scheduleElement.Attribute(ConfigurationHelpers.ConfigToAttributeName)).ToTimeSpan().Value;
                        }
                        catch
                        {
                            throw new InvalidConfigurationException(scheduleElement.MakeValueErrorMessage(ConfigurationHelpers.ConfigToAttributeName));
                        }

                        #endregion To

                        sched.ApplyCommonInfoFromElement(scheduleElement);

                        scheds.Add(sched);
                    }
                }

                cfg.Schedule = scheds.OrderBy(s => s.From).ToList().AsReadOnly();

                TimeSpan? tempFrom = null, tempTo = null;
                foreach (ScheduleData d in cfg.Schedule)
                {
                    if (null == tempFrom && null == tempTo)
                    {
                        tempFrom = d.From;
                        tempTo = d.To;
                        continue;
                    }

                    if ((tempFrom <= d.From && d.From < tempTo) || (tempFrom <= d.To && d.To < tempTo))
                    {
                        throw new InvalidConfigurationException(taskElement.MakeElementErrorMessage(string.Format("Schedule {0}-{1} conflicts with another schedule for this configuration", d.From, d.To)));
                    }
                }

                #region Configuration

                try
                {
                    XElement cfig = taskElement.Elements(ConfigurationHelpers.ConfigModuleConfigurationElementName).SingleOrDefault();

                    if (null != cfig)
                    {
                        cfg.ConfigurationData = cfig.ToString();
                    }
                }
                catch
                {
                    throw new InvalidConfigurationException(taskElement.MakeElementErrorMessage("Task configuration has too many module configuration elements."));
                }

                #endregion

                taskConfigurations.Add(cfg);
            }

            return taskConfigurations.AsReadOnly();
        }

        /// <summary>
        /// Determines the task configuration for a specific time of the day based on the defaults, global configuration and specific schedules.
        /// </summary>
        /// <param name="time">DateTime representing the current time. The date part is discarded.</param>
        /// <returns>The task configuration.</returns>
        public ScheduleData GetScheduleFor(DateTime time)
        {
            ScheduleData result = this.Schedule.FirstOrDefault(s => s.From <= time.TimeOfDay && time.TimeOfDay <= s.To);

            if (null == result)
            {
                result = new ScheduleData()
                {
                    From = new TimeSpan(0, 0, 0),
                    To = new TimeSpan(24, 0, 0)
                };
            }

            result.Spawn = result.Spawn ?? this.Spawn ?? ConfigurationHelpers.DefaultSpawn;
            result.Timeout = result.Timeout ?? this.Timeout ?? ConfigurationHelpers.DefaultTimeout;
            result.SLA = result.SLA ?? this.SLA ?? ConfigurationHelpers.DefaultSLA;
            result.TimeUnit = result.TimeUnit ?? this.TimeUnit ?? ConfigurationHelpers.DefaultTimeUnit;
            result.MaxRuns = result.MaxRuns ?? this.MaxRuns ?? ConfigurationHelpers.DefaultMaxRuns;
            result.Wait = result.Wait ?? this.Wait ?? ConfigurationHelpers.DefaultWait;

            return result;
        }
    }
}
