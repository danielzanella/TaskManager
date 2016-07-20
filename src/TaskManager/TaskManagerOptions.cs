using System;
using System.Globalization;
using System.IO;
using Mono.Options;

namespace TaskManager
{
    /// <summary>
    /// TaskManager options.
    /// </summary>
    public class TaskManagerOptions
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskManager.TaskManagerOptions"/> class.
		/// </summary>
        private TaskManagerOptions()
        {
        }

		/// <summary>
		/// Gets the event log.
		/// </summary>
		/// <value>The event log.</value>
        public IEventLog EventLog { get; private set; }

		/// <summary>
		/// Gets the stats strategy.
		/// </summary>
		/// <value>The stats strategy.</value>
        public IStatsStrategy StatsStrategy { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="TaskManager.TaskManagerOptions"/> show help.
		/// </summary>
		/// <value><c>true</c> if show help; otherwise, <c>false</c>.</value>
        public bool ShowHelp { get; private set; }

		/// <summary>
		/// Gets the help text.
		/// </summary>
		/// <value>The help text.</value>
        public string HelpText { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="TaskManager.TaskManagerOptions"/> non stop.
		/// </summary>
		/// <value><c>true</c> if non stop; otherwise, <c>false</c>.</value>
        internal bool NonStop { get; private set; }

		/// <summary>
		/// Gets the non stop wait.
		/// </summary>
		/// <value>The non stop wait.</value>
        internal int NonStopWait { get; private set; }

		/// <summary>
		/// Create a TaskManageOptions from arguments.
		/// </summary>
		/// <param name="usagePrefix">Usage prefix.</param>
		/// <param name="args">The arguments.</param>
        public static TaskManagerOptions Create(string usagePrefix, string[] args)
        {
            var options = new TaskManagerOptions();
            var optionsSet = BuildOptions(usagePrefix, options);
            options.ParseArguments(optionsSet, args);

            if(!options.ShowHelp)
            {
                // Set the defaults.
                options.EventLog = options.EventLog ?? new WindowsEventLog();
                options.StatsStrategy = options.StatsStrategy ?? new PerformanceCounterStatsStrategy();
            }

            return options;
        }

        private static OptionSet BuildOptions(string usagePrefix, TaskManagerOptions options)
        {
            return new OptionSet()
            {
                "Usage: ",
                String.Format(CultureInfo.InvariantCulture, "   {0} -e <event log> -s <stats>", usagePrefix),
                string.Empty,
                "Options:",
                {
                    "e|event-log=",
                    "the event log. Available values are: Console and Windows. Default is: Windows.",
                    e => options.EventLog = ArgumentsHelper.CreateEventLog(e)
                },
                {
                    "s|stats=",
                    "the stats strategy. Available values are: Memory and PerformanceCounter. Default is: PerformanceCounter.",
                     s => options.StatsStrategy = ArgumentsHelper.CreateStatsStrategy(s)
                },
                {
                    "h|help", "show this message and exit", h => options.ShowHelp = h != null
                },
                // The arguments below are used to functional tests purpose only.
                {
                    "non-stop", "if should wait for user interaction", n => options.NonStop = n != null, true
                },
                {
                    "non-stop-wait=", "the time in milliseconds to wait to tasks run when in non-stop mode", n => options.NonStopWait = Convert.ToInt32(n), true
                },

                string.Empty,
                string.Empty,
                "Samples:",
                String.Format(CultureInfo.InvariantCulture, "{0} -e Windows -s PerformanceCounter", usagePrefix),                
                string.Empty,
                String.Format(CultureInfo.InvariantCulture, "{0} -e Console", usagePrefix),
                string.Empty,
                String.Format(CultureInfo.InvariantCulture, "{0} -s Memory", usagePrefix),
            };
        }
        
        private void ParseArguments(OptionSet optionsSet, string[] args)
        {
            optionsSet.Parse(args);		

            if (ShowHelp)
            {
                using (var writer = new StringWriter())
                {
                    optionsSet.WriteOptionDescriptions(writer);
                    HelpText = writer.ToString();
                }
            }          
        }
    }
}
