using System;
using System.Collections.Generic;
using System.Linq;
using IronFoundry.Container;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerStubStatsTests
    {
        public class WhenManagingNoProcess : ContainerStubContext
        {
            ContainerInfo info;

            public WhenManagingNoProcess() 
            {
                jobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = TimeSpan.Zero,
                    TotalUserTime = TimeSpan.Zero,
                });
                jobObject.GetProcessIds().Returns(new int[0]);

                containerStub.Initialize(containerDirectory, containerHandle, userInfo);

                info = containerStub.GetInfo();
            }

            [Fact]
            public void ReturnsDefaultTotalProcessorStats()
            {
                Assert.Equal(new TimeSpan(0), info.CpuStat.TotalProcessorTime);
            }

            [Fact]
            public void ReturnsDefaultPrivateMemory()
            {
                Assert.Equal(0UL, info.MemoryStat.PrivateBytes);
            }
        }

        public class WhenManagingOneProcess : ContainerStubContext
        {
            ContainerInfo info;
            TimeSpan expectedTotalKernelTime = TimeSpan.FromSeconds(2);
            TimeSpan expectedTotalUserTime = TimeSpan.FromSeconds(8);
            TimeSpan expectedTotalProcessorTime = TimeSpan.FromSeconds(10);
            long expectedPrivateMemoryBytes = 2048;

            public WhenManagingOneProcess()
            {
                jobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = expectedTotalKernelTime,
                    TotalUserTime = expectedTotalUserTime,
                });
                jobObject.GetProcessIds().Returns(new int[] { 1234 });

                var process = Substitute.For<IProcess>();
                process.PrivateMemoryBytes.Returns(expectedPrivateMemoryBytes);
                
                processHelper.GetProcesses(null).ReturnsForAnyArgs(new[] { process });

                containerStub.Initialize(containerDirectory, containerHandle, userInfo);

                info = containerStub.GetInfo();
            }

            [Fact]
            public void ReturnsTotalProcessorTime()
            {
                Assert.Equal(expectedTotalProcessorTime, info.CpuStat.TotalProcessorTime);
            }

            [Fact]
            public void ReturnsExpectedPrivateMemoryBytes()
            {
                Assert.Equal((ulong)expectedPrivateMemoryBytes, info.MemoryStat.PrivateBytes);
            }
        }

        public class WhenManagingMultipleProcesses : ContainerStubContext
        {
            ContainerInfo info;
            TimeSpan expectedTotalKernelTime = TimeSpan.FromSeconds(2);
            TimeSpan expectedTotalUserTime = TimeSpan.FromSeconds(8);
            TimeSpan expectedTotalProcessorTime = TimeSpan.FromSeconds(10);
            long expectedPrivateMemoryBytes = 2048;

            List<IProcess> processes = new List<IProcess>();
            CpuStatistics expectedCpuStats;

            public WhenManagingMultipleProcesses()
            {
                expectedCpuStats = new CpuStatistics
                {
                    TotalKernelTime = expectedTotalKernelTime + expectedTotalKernelTime,
                    TotalUserTime = expectedTotalUserTime + expectedTotalUserTime,
                };

                var firstProcess = Substitute.For<IProcess>();
                firstProcess.Id.Returns(12);
                firstProcess.PrivateMemoryBytes.Returns(expectedPrivateMemoryBytes);
                processes.Add(firstProcess);

                var secondProcess = Substitute.For<IProcess>();
                secondProcess.Id.Returns(34);
                secondProcess.PrivateMemoryBytes.Returns(expectedPrivateMemoryBytes);
                processes.Add(secondProcess);

                jobObject.GetCpuStatistics().Returns(expectedCpuStats);
                jobObject.GetProcessIds().Returns(new int[] { 12, 34 });

                processHelper.GetProcesses(null).ReturnsForAnyArgs(new[] { firstProcess, secondProcess });

                containerStub.Initialize(containerDirectory, containerHandle, userInfo);

                info = containerStub.GetInfo();
            }

            [Fact]
            public void ReturnsAggregateTotalProcessorTime()
            {
                Assert.Equal(expectedCpuStats.TotalKernelTime.Ticks + expectedCpuStats.TotalUserTime.Ticks, info.CpuStat.TotalProcessorTime.Ticks);
            }

            [Fact]
            public void ReturnsAggregatePrivateMemoryBytes()
            {
                Assert.Equal((ulong)processes.Sum(p => p.PrivateMemoryBytes), info.MemoryStat.PrivateBytes);
            }
        }
    }
}
