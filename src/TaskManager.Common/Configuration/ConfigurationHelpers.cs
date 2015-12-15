namespace TaskManager.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;
    using TaskManager.Common;

    /// <summary>
    /// Common helpers for manipulating configuration XML.
    /// </summary>
    public static class ConfigurationHelpers
    {
        /// <summary>
        /// Default number of executions in a burst.
        /// </summary>
        public const int DefaultMaxRuns = 1;

        /// <summary>
        /// Default number of instances spawned per task.
        /// </summary>
        public const int DefaultSpawn = 1;

        /// <summary>
        /// The name of the root element in a configuration file.
        /// </summary>
        public const string ConfigRootElementName = "tasks";

        /// <summary>
        /// The name of the task element in a configuration file.
        /// </summary>
        public const string ConfigTaskElementName = "task";

        /// <summary>
        /// The name of the configuration element in a configuration file.
        /// </summary>
        public const string ConfigModuleConfigurationElementName = "configuration";

        /// <summary>
        /// The name of the schedule list element in a configuration file.
        /// </summary>
        public const string ConfigScheduleListElementName = "schedules";

        /// <summary>
        /// The name of the schedule element in a configuration file.
        /// </summary>
        public const string ConfigScheduleItemElementName = "schedule";

        /// <summary>
        /// The name of the class type attribute in a configuration file.
        /// </summary>
        public const string ConfigTypeAttributeName = "type";

        /// <summary>
        /// The name of the culture attribute in a configuration file.
        /// </summary>
        public const string ConfigCultureAttributeName = "culture";

        /// <summary>
        /// The name of the start time range attribute in a configuration file.
        /// </summary>
        public const string ConfigFromAttributeName = "from";

        /// <summary>
        /// The name of the end time range attribute in a configuration file.
        /// </summary>
        public const string ConfigToAttributeName = "to";

        /// <summary>
        /// The name of the spawn attribute in a configuration file.
        /// </summary>
        public const string ConfigSpawnAttributeName = "spawn";

        /// <summary>
        /// The name of the timeout attribute in a configuration file.
        /// </summary>
        public const string ConfigTimeoutAttributeName = "timeout";

        /// <summary>
        /// The name of the sla attribute in a configuration file.
        /// </summary>
        public const string ConfigSlaAttributeName = "sla";

        /// <summary>
        /// The name of the time unit attribute in a configuration file.
        /// </summary>
        public const string ConfigTimeUnitAttributeName = "timeUnit";

        /// <summary>
        /// The name of the max number of runs attribute in a configuration file.
        /// </summary>
        public const string ConfigMaxRunsAttributeName = "maxRuns";

        /// <summary>
        /// The name of the wait attribute in a configuration file.
        /// </summary>
        public const string ConfigWaitAttributeName = "wait";

        /// <summary>
        /// The name of the starting delay attribute in a configuration file.
        /// </summary>
        public const string ConfigDelayStartName = "delayStart";

        /// <summary>
        /// The default timeout.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 15, 0);

        /// <summary>
        /// The default SLA.
        /// </summary>
        public static readonly TimeSpan DefaultSLA = new TimeSpan(0, 0, 5);

        /// <summary>
        /// The default time unit.
        /// </summary>
        public static readonly TimeSpan DefaultTimeUnit = new TimeSpan(0, 0, 1);

        /// <summary>
        /// The default wait between bursts.
        /// </summary>
        public static readonly TimeSpan DefaultWait = new TimeSpan(0, 0, 0);

        /// <summary>
        /// The default starting delay.
        /// </summary>
        public static readonly TimeSpan DefaultDelayStart = new TimeSpan(0, 5, 0);

        /// <summary>
        /// Converts a string to a nullable TimeSpan.
        /// </summary>
        /// <param name="value">The string to be parsed.</param>
        /// <returns>If value contains a parseable TimeSpan, returns the TimeSpan; if value is null, returns null; otherwise, throws an exception.</returns>
        public static TimeSpan? ToTimeSpan(this string value)
        {
            if (null == value)
            {
                return null;
            }

            return TimeSpan.Parse(value);
        }

        /// <summary>
        /// Attempts to parse configuration data from element.
        /// </summary>
        /// <param name="target">The instance that will receive the configuration data.</param>
        /// <param name="element">The XML element containing configuration data.</param>
        public static void ApplyCommonInfoFromElement(this ITaskConfigurationData target, XElement element)
        {
            try
            {
                target.Spawn = (int?)element.Attribute(ConfigSpawnAttributeName);
            }
            catch
            {
                throw new InvalidConfigurationException(element.MakeValueErrorMessage(ConfigSpawnAttributeName));
            }

            try
            {
                target.Timeout = ((string)element.Attribute(ConfigTimeoutAttributeName)).ToTimeSpan();
            }
            catch
            {
                throw new InvalidConfigurationException(element.MakeValueErrorMessage(ConfigTimeoutAttributeName));
            }

            try
            {
                target.SLA = ((string)element.Attribute(ConfigSlaAttributeName)).ToTimeSpan();
            }
            catch
            {
                throw new InvalidConfigurationException(element.MakeValueErrorMessage(ConfigSlaAttributeName));
            }

            try
            {
                target.TimeUnit = ((string)element.Attribute(ConfigTimeUnitAttributeName)).ToTimeSpan();
            }
            catch
            {
                throw new InvalidConfigurationException(element.MakeValueErrorMessage(ConfigTimeUnitAttributeName));
            }

            try
            {
                target.MaxRuns = (int?)element.Attribute(ConfigMaxRunsAttributeName);
            }
            catch
            {
                throw new InvalidConfigurationException(element.MakeValueErrorMessage(ConfigMaxRunsAttributeName));
            }

            try
            {
                target.Wait = ((string)element.Attribute(ConfigWaitAttributeName)).ToTimeSpan();
            }
            catch
            {
                throw new InvalidConfigurationException(element.MakeValueErrorMessage(ConfigWaitAttributeName));
            }
        }

        #region MakeErrorMessage

        /// <summary>
        /// Composes an error message for an invalid configuration value.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="attributeName">The name of the attribute that contains the invalid value.</param>
        /// <returns>The error message.</returns>
        public static string MakeValueErrorMessage(this XElement element, string attributeName)
        {
            return MakeAttributeErrorMessage(element, "Invalid configuration value", attributeName);
        }

        /// <summary>
        /// Composes an error message for an invalid configuration attribute.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="message">The message.</param>
        /// <param name="attributeName">The name of the attribute that contains the invalid value.</param>
        /// <returns>The error message.</returns>
        public static string MakeAttributeErrorMessage(this XElement element, string message, string attributeName)
        {
            if (null == element)
            {
                return "Configuration element not found.";
            }

            IXmlLineInfo lineInfo = element;

            if (lineInfo.HasLineInfo())
            {
                return string.Format("{0}. Element: \"{3}\" (line {1}, column {2}), attribute: \"{4}\".", message, lineInfo.LineNumber, lineInfo.LinePosition, element.Name, attributeName);
            }
            else
            {
                return string.Format("{0}. Element: \"{1}\", attribute: \"{2}\".", message, element.Name, attributeName);
            }
        }

        /// <summary>
        /// Composes an error message for an invalid configuration element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="message">The message.</param>
        /// <returns>The error message.</returns>
        public static string MakeElementErrorMessage(this XElement element, string message)
        {
            if (null == element)
            {
                return "Configuration element not found.";
            }

            IXmlLineInfo lineInfo = element;

            if (lineInfo.HasLineInfo())
            {
                return string.Format("{0}. Element: \"{3}\" (line {1}, column {2}).", message, lineInfo.LineNumber, lineInfo.LinePosition, element.Name);
            }
            else
            {
                return string.Format("{0}. Element: \"{1}\".", message, element.Name);
            }
        }

        #endregion

        /// <summary>
        /// Determines whether the provided path corresponds to a valid XML configuration file.
        /// </summary>
        /// <param name="xmlFile">The path to the configuration file.</param>
        /// <returns>True if the file is valid, false otherwise.</returns>
        public static bool IsValidConfigurationFile(this string xmlFile)
        {
            try
            {
                XDocument doc = XDocument.Load(xmlFile);

                XElement root = doc.Root;

                if (root.Name != ConfigurationHelpers.ConfigRootElementName)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses configuration data from a file.
        /// </summary>
        /// <param name="fileName">The file path.</param>
        /// <returns>The configuration data.</returns>
        public static IEnumerable<TaskConfigurationData> ToTaskConfiguration(this string fileName)
        {
            return TaskConfigurationData.LoadFrom(fileName);
        }

        /// <summary>
        /// Parses a string as XML.
        /// </summary>
        /// <param name="data">The string.</param>
        /// <returns>The XML element.</returns>
        public static XElement ToXml(this string data)
        {
            return XElement.Parse(data);
        }
    }
}
