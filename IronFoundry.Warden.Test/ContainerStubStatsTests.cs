using System;
using System.Collections.Generic;
using System.Linq;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;
using IronFoundry.Warden.Shared.Data;

namespace IronFoundry.Warden.Test
{
    public class ContainerStubStatsTests
    {
        public class WhenManagingNoProcess : ContainerStubContext
        {
            ProcessStats stats;

            public WhenManagingNoProcess() 
            {
                jobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = TimeSpan.Zero,
                    TotalUserTime = TimeSpan.Zero,
                });
                jobObject.GetProcessIds().Returns(new int[0]);

                stats = containerStub.GetProcessStatistics();
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
            public void ReturnsDefaultPrivateMemory()
            {
                Assert.Equal(0, stats.PrivateMemory);
            }

            [Fact]
            public void ReturnsDefaultWorkingSet()
            {
                Assert.Equal(0, stats.WorkingSet);
            }
        }

        public class WhenManagingOneProcess : ContainerStubContext
        {
            ProcessStats stats;
            TimeSpan expectedTotalKernelTime = TimeSpan.FromSeconds(2);
            TimeSpan expectedTotalUserTime = TimeSpan.FromSeconds(8);
            TimeSpan expectedTotalProcessorTime = TimeSpan.FromSeconds(10);
            long expectedPrivateMemoryBytes = 2048;
            long expectedWorkingSet = 4096;

            public WhenManagingOneProcess()
            {
                jobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = expectedTotalKernelTime,
                    TotalUserTime = expectedTotalUserTime,
                });
                jobObject.GetProcessIds().Returns(new int[] { 1234 });

                var process = Substitute.For<IProcess>();
                process.TotalProcessorTime.Returns(expectedTotalProcessorTime);
                process.TotalUserProcessorTime.Returns(expectedTotalUserTime);
                process.PrivateMemoryBytes.Returns(expectedPrivateMemoryBytes);
                process.WorkingSet.Returns(expectedWorkingSet);
                
                processHelper.GetProcesses(null).ReturnsForAnyArgs(new[] { process });

                stats = containerStub.GetProcessStatistics();
            }

            [Fact]
            public void ReturnsTotalProcessorTime()
            {
                Assert.Equal(expectedTotalProcessorTime, stats.TotalProcessorTime);
            }

            [Fact]
            public void ReturnsTotalUserProcessorTime()
            {
                Assert.Equal(expectedTotalUserTime, stats.TotalUserProcessorTime);
            }

            [Fact]
            public void ReturnsExpectedPrivateMemoryBytes()
            {
                Assert.Equal(expectedPrivateMemoryBytes, stats.PrivateMemory);
            }

            [Fact]
            public void ReturnsExpectedWorkingSet()
            {
                Assert.Equal(expectedWorkingSet, stats.WorkingSet);
            }
        }

        public class WhenManagingMultipleProcesses : ContainerStubContext
        {
            ProcessStats stats;
            TimeSpan expectedTotalKernelTime = TimeSpan.FromSeconds(2);
            TimeSpan expectedTotalUserTime = TimeSpan.FromSeconds(8);
            TimeSpan expectedTotalProcessorTime = TimeSpan.FromSeconds(10);
            long expectedPrivateMemoryBytes = 2048;
            long expectedWorkingSet = 4096;

            List<IProcess> processes = new List<IProcess>();

            public WhenManagingMultipleProcesses()
            {
                var firstProcess = Substitute.For<IProcess>();
                firstProcess.Id.Returns(12);
                firstProcess.TotalProcessorTime.Returns(expectedTotalProcessorTime);
                firstProcess.TotalUserProcessorTime.Returns(expectedTotalUserTime);
                firstProcess.PrivateMemoryBytes.Returns(expectedPrivateMemoryBytes);
                firstProcess.WorkingSet.Returns(expectedWorkingSet);
                processes.Add(firstProcess);

                var secondProcess = Substitute.For<IProcess>();
                secondProcess.Id.Returns(34);
                secondProcess.TotalProcessorTime.Returns(expectedTotalProcessorTime);
                secondProcess.TotalUserProcessorTime.Returns(expectedTotalUserTime);
                secondProcess.PrivateMemoryBytes.Returns(expectedPrivateMemoryBytes);
                secondProcess.WorkingSet.Returns(expectedWorkingSet);
                processes.Add(secondProcess);

                jobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = expectedTotalKernelTime + expectedTotalKernelTime,
                    TotalUserTime = expectedTotalUserTime + expectedTotalUserTime,
                });
                jobObject.GetProcessIds().Returns(new int[] { 12, 34 });

                processHelper.GetProcesses(null).ReturnsForAnyArgs(new[] { firstProcess, secondProcess });

                stats = containerStub.GetProcessStatistics();
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

            [Fact]
            public void ReturnsAggregatePrivateMemoryBytes()
            {
                Assert.Equal(processes.Sum(p => p.PrivateMemoryBytes), stats.PrivateMemory);
            }

            [Fact]
            void ReturnsAggregateWorkingSet()
            {
                Assert.Equal(processes.Sum(p => p.WorkingSet), stats.WorkingSet);
            }
        }
    }
}
