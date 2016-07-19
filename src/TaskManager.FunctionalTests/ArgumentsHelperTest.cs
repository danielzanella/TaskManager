using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TaskManager.FunctionalTests
{
    [TestFixture]
    public class ArgumentsHelperTest
    {
        public void CreateStatsStrategy_InvalidName_Exception()
        {
            Assert.Catch<ArgumentException>(() => ArgumentsHelper.CreateStatsStrategy("TEST"));
        }

        public void CreateStatsStrategy_ValidName_Instance()
        {
            var actual = ArgumentsHelper.CreateStatsStrategy("Memory");
            Assert.IsInstanceOf<MemoryStatsStrategy>(actual);

            actual = ArgumentsHelper.CreateStatsStrategy("PerformanceCounter");
            Assert.IsInstanceOf<MemoryStatsStrategy>(actual);
        }

        public void CreateEventLog_InvalidName_Exception()
        {
            Assert.Catch<ArgumentException>(() => ArgumentsHelper.CreateEventLog("TEST"));
        }

        public void CreateEventLog_ValidName_Instance()
        {
            var actual = ArgumentsHelper.CreateEventLog("Console");
            Assert.IsInstanceOf<MemoryStatsStrategy>(actual);

            actual = ArgumentsHelper.CreateEventLog("Windows");
            Assert.IsInstanceOf<MemoryStatsStrategy>(actual);
        }
    }
}
