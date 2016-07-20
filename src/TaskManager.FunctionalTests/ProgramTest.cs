using NUnit.Framework;
using System;
using TestSharp;
using System.IO;

namespace TaskManager.FunctionalTests
{
	[TestFixture]
	public class ProgramTest
	{

#if DEBUG
        private const string Config = "Debug";
        private const int NonStopWait = 15000;
#else
        private const string Config = "Release";
        private const int NonStopWait = 30000;
#endif

        [TestFixtureSetUp]
        public void SetUpAllTests()
        {
            ProcessHelper.KillAll("cmd");
        }


        [TestFixtureTearDown]
        public void TearDownAllTests()
        {
            Run("ServiceStop.cmd", waitForExit: false);
            Run("ServiceUninstall.cmd");
            ProcessHelper.KillAll("cmd");
        }

        [Test]
        public void RunForConsole_NoArgs_UseDefaultsAndRunTasks()
        {
            var output = RunTaskManagerExe();
            StringAssert.Contains("Initializing service...", output);
            StringAssert.Contains("Event log: WindowsEventLog", output);
            StringAssert.Contains("Stats strategy: PerformanceCounterStatsStrategy", output);
            StringAssert.Contains("TaskModule2 starting...", output);
            StringAssert.Contains("TaskModule2 ended.", output);
        }

        [Test]
		public void RunForConsole_ConsoleEventLogAndMemoryStatsStrategy_RunTasks()
		{
			var output = RunTaskManagerExe("Console", "Memory");
            StringAssert.Contains("Initializing service...", output);
            StringAssert.Contains("Event log: ConsoleEventLog", output);
            StringAssert.Contains("Stats strategy: MemoryStatsStrategy", output);
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
			var output = RunTaskManagerExe("Windows", "PerformanceCounter");

            StringAssert.Contains("Initializing service...", output);
            StringAssert.Contains("Event log: WindowsEventLog", output);
            StringAssert.Contains("Stats strategy: PerformanceCounterStatsStrategy", output);
            StringAssert.Contains("TaskModule2 starting...", output);
            StringAssert.Contains("TaskModule2 ended.", output);            
        }

        [Test]
        public void RunForConsole_Help_ShowsHelp()
        {
            var output = RunTaskManagerExe(null, null, "-help");

            StringAssert.Contains("Usage:", output);
            StringAssert.Contains("TaskManager.exe -e <event log> -s <stats>", output);
            StringAssert.Contains("-e, --event-log=VALUE", output);
            StringAssert.Contains("-s, --stats=VALUE", output);
            StringAssert.Contains("-h, --help ", output);
            StringAssert.DoesNotContain("--non-stop", output);
            StringAssert.Contains("TaskManager.exe -e Windows -s PerformanceCounter", output);
        }

        [Test]
        public void WindowsService_Cmds_InstallAndUninstall()
        {
            var output = Run("ServiceUninstall.cmd");
            output = Run("ServiceInstall.cmd");

            // Check if service was really installed.
            ServiceAssert.IsStopped("TaskManager");
            
            output = Run("ServiceStart.cmd", waitForExit:false);
            output = Run("ServiceStop.cmd", waitForExit:false);
            output = Run("ServiceUninstall.cmd");            
        }

        private static string RunTaskManagerExe(string eventLog = null, string statsStrategy = null, string extraArguments = null)
		{            
            var args = string.Empty;

            if (eventLog != null)          
            {
                args = String.Format("-e {0} -s {1} ", eventLog, statsStrategy);
            }

            args += "-non-stop -non-stop-wait " + NonStopWait;
            args += " " + extraArguments;
                

            return Run("TaskManager.exe", args);
		}

        public static string Run(string exeName, string args = null, bool waitForExit = true)
        {
            var taskManagerFolder = VSProjectHelper.GetProjectFolderPath("TaskManager");

            var exePath = Path.Combine(taskManagerFolder, string.Format(@"bin\{0}\{1}", Config, exeName));
         
            var output = ProcessHelper.Run(exePath, args, waitForExit);
            Assert.IsNotNull(output);

            StringAssert.DoesNotContain("SecurityException", output, "You do not have rights to install something, maybe the custom event log. Try 'Run as Administrator'.");

            return output;
        }
    }
}
