using NUnit.Framework;
using System;
using TestSharp;
using System.IO;

namespace TaskManager.FunctionalTests
{
	[TestFixture]
	public class ProgramTest
	{

        [Test]
        public void RunForConsole_NoArgs_UseDefaultsAndRunTasks()
        {
            var output = Run();
            StringAssert.Contains("Initializing service...", output);
            StringAssert.Contains("Event log: TaskManager.WindowsEventLog", output);
            StringAssert.Contains("Stats strategy: TaskManager.PerformanceCounterStatsStrategy", output);
            StringAssert.Contains("TaskModule2 starting...", output);
            StringAssert.Contains("TaskModule2 ended.", output);
        }

        [Test]
		public void RunForConsole_ConsoleEventLogAndMemoryStatsStrategy_RunTasks()
		{
			var output = Run ("Console", "Memory");
            StringAssert.Contains("Initializing service...", output);
            StringAssert.Contains("Event log: TaskManager.ConsoleEventLog", output);
            StringAssert.Contains("Stats strategy: TaskManager.MemoryStatsStrategy", output);
            StringAssert.Contains("Scanning path", output);
            StringAssert.Contains("Module found:", output);
            StringAssert.Contains("Found 2 task definitions in configuration file", output);
            StringAssert.Contains("Task 'Task.Sample.TestModule1' successfully configured and initialized. (5 schedules, 1s start delay, 60s SLA, 600s timeout)", output);
            StringAssert.Contains("Task 'Task.Sample.TestModule2' successfully configured and initialized. (1 schedules, 1s start delay, 60s SLA, 600s timeout)", output);
            StringAssert.Contains("1 modules found", output);
            StringAssert.Contains("Service successfully started...", output);
            StringAssert.Contains("Registering task 'Task.Sample.TestModule1'...", output);
            StringAssert.Contains("Registering task 'Task.Sample.TestModule2'...", output);
            StringAssert.Contains("TaskModule2 starting...", output);
            StringAssert.Contains("TaskModule2 ended.", output);
            StringAssert.Contains("Stopping task 'Task.Sample.TestModule2'...", output);
            StringAssert.Contains("Stopping task 'Task.Sample.TestModule1'...", output);
            StringAssert.Contains("Unloading AppDomain ", output);
            StringAssert.Contains("Service successfully stopped.", output);
        }

		[Test]
		public void RunForConsole_WindowsEventLogAndPerformanceCounterStatsStrategy_RunTasks()
		{
			var output = Run ("Windows", "PerformanceCounter");

            StringAssert.Contains("Initializing service...", output);
            StringAssert.Contains("Event log: TaskManager.WindowsEventLog", output);
            StringAssert.Contains("Stats strategy: TaskManager.PerformanceCounterStatsStrategy", output);
            StringAssert.Contains("TaskModule2 starting...", output);
            StringAssert.Contains("TaskModule2 ended.", output);            
        }

        [Test]
        public void RunForConsole_Help_ShowsHelp()
        {
            var output = Run(null, null, "-help");

            StringAssert.Contains("Usage:", output);
            StringAssert.Contains("TaskManager -e <event log> -s <stats>", output);
            StringAssert.Contains("-e, --event-log=VALUE", output);
            StringAssert.Contains("-s, --stats=VALUE", output);
            StringAssert.Contains("-h, --help ", output);
            StringAssert.DoesNotContain("--non-stop", output);
            StringAssert.Contains("TaskManager.exe -e Windows -s PerformanceCounter", output);
        }

        private static string Run(string eventLog = null, string statsStrategy = null, string extraArguments = null)
		{
			var taskManagerFolder = VSProjectHelper.GetProjectFolderPath ("TaskManager");

#if DEBUG
            var config = "Debug";
#else
            var config = "Release";
#endif
            var taskManagerExe = Path.Combine (taskManagerFolder, string.Format(@"bin\{0}\TaskManager.exe", config));
            var args = string.Empty;

            if (eventLog != null)          
            {
                args = String.Format("-e {0} -s {1} ", eventLog, statsStrategy);
            }

            args += "-non-stop -non-stop-wait 15000 " + extraArguments;

            var output = ProcessHelper.Run (taskManagerExe, args, true);
			Assert.IsNotNull (output);

            return output;       		
		}
	}
}
