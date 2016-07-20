using System;
using NUnit.Framework;

namespace TaskManager.UnitTests
{
    [TestFixture]
    public class TaskManagerOptionsTest
    {
        [Test]
        public void Create_InvalidArgs_Exception()
        {
            Assert.Catch<ArgumentException>(() =>
            {
                TaskManagerOptions.Create("Test: ", new string[] { "-eventLog", "xpto" });
            });
        }

        [Test]
        public void Create_NoArgs_Instance()
        {
            var actual = TaskManagerOptions.Create("Test: ", new string[0]);
            Assert.IsInstanceOf<WindowsEventLog>(actual.EventLog);
            Assert.IsInstanceOf<PerformanceCounterStatsStrategy>(actual.StatsStrategy);
            Assert.IsFalse(actual.NonStop);
            Assert.AreEqual(0, actual.NonStopWait);
        }

        [Test]
        public void Create_Help_Instance()
        {
            var actual = TaskManagerOptions.Create("Test: ", new string[] { "-h" });
            Assert.IsTrue(actual.ShowHelp);
            StringAssert.Contains("Test: ", actual.HelpText);
            Assert.IsNull(actual.EventLog);
            Assert.IsNull(actual.StatsStrategy);
            Assert.IsFalse(actual.NonStop);
            Assert.AreEqual(0, actual.NonStopWait);
        }

        [Test]
        public void Create_OnlyEventLog_Instance()
        {
            var actual = TaskManagerOptions.Create("Test: ", new string[] { "-e", "Console" });
            Assert.IsInstanceOf<ConsoleEventLog>(actual.EventLog);
            Assert.IsInstanceOf<PerformanceCounterStatsStrategy>(actual.StatsStrategy);
            Assert.IsFalse(actual.NonStop);
            Assert.AreEqual(0, actual.NonStopWait);
        }

        [Test]
        public void Create_OnlyStatsStrategy_Instance()
        {
            var actual = TaskManagerOptions.Create("Test: ", new string[] { "-s", "Memory" });
            Assert.IsInstanceOf<WindowsEventLog>(actual.EventLog);
            Assert.IsInstanceOf<MemoryStatsStrategy>(actual.StatsStrategy);
            Assert.IsFalse(actual.NonStop);
            Assert.AreEqual(0, actual.NonStopWait);
        }

        [Test]
        public void Create_AllArgs_Instance()
        {
            var actual = TaskManagerOptions.Create("Test: ", new string[] { "-e", "Console", "-s", "Memory", "-non-stop", "-non-stop-wait", "5000" });
            Assert.IsInstanceOf<ConsoleEventLog>(actual.EventLog);
            Assert.IsInstanceOf<MemoryStatsStrategy>(actual.StatsStrategy);
            Assert.IsTrue(actual.NonStop);
            Assert.AreEqual(5000, actual.NonStopWait);

            actual = TaskManagerOptions.Create("Test: ", new string[] { "-e", "Windows", "-s", "PerformanceCounter", "-non-stop", "-non-stop-wait", "1000" });
            Assert.IsInstanceOf<WindowsEventLog>(actual.EventLog);
            Assert.IsInstanceOf<PerformanceCounterStatsStrategy>(actual.StatsStrategy);
            Assert.IsTrue(actual.NonStop);
            Assert.AreEqual(1000, actual.NonStopWait);
        }
    }
}
