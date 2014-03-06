using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Utilities;
using Xunit;
using NSubstitute;

namespace IronFoundry.Warden.Test
{
    public class ProcessManagerTests
    {
        public class WhenManagingNoProcess
        {
            ProcessManager manager;
            ProcessStats stats;

            public WhenManagingNoProcess()
            {
                manager = new ProcessManager("TestUser");
                stats = manager.GetProcessStats();
            }

            [Fact]
            public void ReturnsDefaultTotalProcessorStats()
            {
                Assert.Equal(new TimeSpan(0), stats.TotalProcessorTime);
            }

            [Fact]
            public void ReturnsDefaultTotalUserProcessorStats()
            {
                Assert.Equal(new TimeSpan(0), stats.TotalUserProcessorTime);
            }

            [Fact]
            public void ReturnsDefaultWorkingSet()
            {
                Assert.Equal(0, stats.WorkingSet);
            }
        }


        public class WhenManagingOneProcess
        {
            ProcessStats stats;
            TimeSpan expectedTotalProcess = new TimeSpan(2048);
            TimeSpan expectedTotalUserProcess = new TimeSpan(1024);
            long expectedWorkingSet = 4096;

            public WhenManagingOneProcess()
            {
                var manager = new ProcessManager("TestUser");

                var mockProcess = Substitute.For<IProcess>();
                mockProcess.TotalProcessorTime.Returns(expectedTotalProcess);
                mockProcess.TotalUserProcessorTime.Returns(expectedTotalUserProcess);
                mockProcess.WorkingSet.Returns(expectedWorkingSet);

                manager.AddProcess(mockProcess);

                stats = manager.GetProcessStats();
            }

            [Fact]
            public void ReturnsTotalProcessorTime()
            {
                Assert.Equal(expectedTotalProcess, stats.TotalProcessorTime);
            }

            [Fact]
            public void ReturnsTotalUserProcessorTime()
            {
                Assert.Equal(expectedTotalUserProcess, stats.TotalUserProcessorTime);
            }

            [Fact]
            public void ReturnsExpectedWorkingSetInfo()
            {
                Assert.Equal(expectedWorkingSet, stats.WorkingSet);
            }
        }

        public class WhenManagingMultipleProcesses
        {
            ProcessStats stats;
            TimeSpan expectedTotalProcess = new TimeSpan(2048);
            TimeSpan expectedTotalUserProcess = new TimeSpan(1024);
            long expectedWorkingSet = 4096;

            List<IProcess> processes = new List<IProcess>();

            public WhenManagingMultipleProcesses()
            {
                var manager = new ProcessManager("TestUser");

                var firstProcess = Substitute.For<IProcess>();
                firstProcess.Id.Returns(0);
                firstProcess.TotalProcessorTime.Returns(expectedTotalProcess);
                firstProcess.TotalUserProcessorTime.Returns(expectedTotalUserProcess);
                firstProcess.WorkingSet.Returns(expectedWorkingSet);
                processes.Add(firstProcess);
                manager.AddProcess(firstProcess);

                var secondProcess = Substitute.For<IProcess>();
                secondProcess.Id.Returns(1);
                secondProcess.TotalProcessorTime.Returns(expectedTotalProcess);
                secondProcess.TotalUserProcessorTime.Returns(expectedTotalUserProcess);
                secondProcess.WorkingSet.Returns(expectedWorkingSet);
                processes.Add(secondProcess);
                manager.AddProcess(secondProcess);

                stats = manager.GetProcessStats();
            }

            [Fact]
            public void ReturnsAggregateTotalProcessorTime()
            {
                Assert.Equal(processes.Sum(p => p.TotalProcessorTime.Ticks), stats.TotalProcessorTime.Ticks);
            }

            [Fact]
            public void ReturnsAggregateTotalUserProcessorTime()
            {
                Assert.Equal(processes.Sum(p => p.TotalUserProcessorTime.Ticks), stats.TotalUserProcessorTime.Ticks);
            }

            [Fact] void ReturnsAggregateWorkingSet()
            {
                Assert.Equal(processes.Sum(p => p.WorkingSet), stats.WorkingSet);
            }
        }
    }
}
