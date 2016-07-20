using System;
using System.Globalization;
using System.Reflection;

namespace TaskManager
{
    /// <summary>
    /// Command-line and service arguments helper.
    /// </summary>
    public static class ArgumentsHelper
    {

        /// <summary>
        /// Creates the stats strategy.
        /// </summary>
        /// <param name="statsName">Name of the stats strategy.</param>
        /// <returns>The instance.</returns>
        public static IStatsStrategy CreateStatsStrategy(string statsName)
        {
            return CreateArgumentObject<IStatsStrategy>(statsName, "StatsStrategy", "Stats strategy");
        }

        /// <summary>
        /// Creates the event log.
        /// </summary>
        /// <param name="eventLogName">Name of the event log.</param>
        /// <returns>The instance.</returns>
        public static IEventLog CreateEventLog(string eventLogName)
        {
            return CreateArgumentObject<IEventLog>(eventLogName, "EventLog", "Event log");
        }

        private static T CreateArgumentObject<T>(string name, string suffix, string description)
        {
            var statsTypeName = String.Format(CultureInfo.InvariantCulture, "TaskManager.{0}{1}", name, suffix);
            var statsType = Type.GetType(statsTypeName);

            if (statsType == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "{0} with name '{1} ({2})' not found.", description, name, statsTypeName));
            }

            try
            {
                var instance = (T)Activator.CreateInstance(statsType);                

                return instance;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
